using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartHome.Data;

#nullable disable
namespace SmartHome.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261111000100_NormalizeBomOrderLines")]
public class NormalizeBomOrderLines : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
-- Historical upgrade reference; installed ONLY by NormalizeBomOrderLines migration.
-- Upgrade in one EF migration transaction. Any invalid legacy reference/quantity
-- aborts the migration instead of silently omitting historical data.
CREATE TABLE bom_order_line (
 "OrderRevisionId" integer NOT NULL,
 "SortOrder" integer NOT NULL,
 "VariantId" integer NOT NULL,
 "Quantity" numeric NOT NULL,
 CONSTRAINT "FK_bom_order_line_bom_order_revision_OrderRevisionId" FOREIGN KEY ("OrderRevisionId") REFERENCES bom_order_revision("Id") ON DELETE RESTRICT,
 CONSTRAINT "FK_bom_order_line_bom_variant_VariantId" FOREIGN KEY ("VariantId") REFERENCES bom_variant("Id") ON DELETE RESTRICT,
 CONSTRAINT "PK_bom_order_line" PRIMARY KEY ("OrderRevisionId", "SortOrder"),
 CONSTRAINT "CK_bom_order_line_sort" CHECK ("SortOrder" >= 0),
 CONSTRAINT "CK_bom_order_line_quantity" CHECK ("Quantity" > 0 AND "Quantity" <= 1000000000)
);
CREATE INDEX "IX_bom_order_line_VariantId" ON bom_order_line("VariantId");
INSERT INTO bom_order_line ("OrderRevisionId", "SortOrder", "VariantId", "Quantity")
 SELECT r."Id", (l.ordinality-1)::integer, (l.value->>'variantId')::integer, (l.value->>'quantity')::numeric
 FROM bom_order_revision r CROSS JOIN LATERAL jsonb_array_elements(r."Lines") WITH ORDINALITY l;
DO $$ BEGIN
 IF EXISTS (SELECT 1 FROM bom_order_revision r WHERE jsonb_typeof(r."Lines") IS DISTINCT FROM 'array'
   OR jsonb_array_length(r."Lines")=0
   OR r."Lines" <> (SELECT jsonb_agg(jsonb_build_object('variantId',l."VariantId",'quantity',l."Quantity") ORDER BY l."SortOrder") FROM bom_order_line l WHERE l."OrderRevisionId"=r."Id"))
 THEN RAISE EXCEPTION 'Перенос позиций заказа изменил данные; миграция отменена.'; END IF;
END $$;
-- xid8 belongs to the top-level transaction, including EF savepoints. A trigger
-- overwrites supplied values, so direct SQL cannot forge an old revision's marker.
ALTER TABLE bom_order_revision ADD COLUMN "CreationTransaction" xid8 NOT NULL DEFAULT pg_current_xact_id();
CREATE OR REPLACE FUNCTION bom_guard_order() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE last_id int;
BEGIN
 PERFORM pg_advisory_xact_lock(734021,1);
 SELECT max("Id") INTO last_id FROM bom_order_revision WHERE "OrderId"=NEW."OrderId";
 IF NEW."PreviousId" IS DISTINCT FROM last_id OR (last_id IS NOT NULL AND NEW."Id"<=last_id)
 THEN RAISE EXCEPTION 'Заказ уже изменён.' USING ERRCODE='40001'; END IF;
 IF length(trim(NEW."Reason"))=0 THEN RAISE EXCEPTION 'Укажите причину изменения.'; END IF;
 NEW."CreationTransaction" := pg_current_xact_id();
 RETURN NEW;
END $$;
CREATE FUNCTION bom_guard_order_line() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF NOT EXISTS (SELECT 1 FROM bom_order_revision WHERE "Id"=NEW."OrderRevisionId" AND "CreationTransaction"=pg_current_xact_id())
 THEN RAISE EXCEPTION 'Позиции сохранённой версии заказа неизменяемы. Создайте новую версию.'; END IF;
 RETURN NEW;
END $$;
CREATE FUNCTION bom_check_order_lines() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE rid int; n bigint; last_position int;
BEGIN
 IF TG_TABLE_NAME='bom_order_revision' THEN rid:=NEW."Id"; ELSE rid:=NEW."OrderRevisionId"; END IF;
 SELECT count(*),max("SortOrder") INTO n,last_position FROM bom_order_line WHERE "OrderRevisionId"=rid;
 IF n=0 OR last_position<>n-1 THEN RAISE EXCEPTION 'Нужны позиции заказа с последовательным порядком от нуля.'; END IF;
 RETURN NULL;
END $$;
CREATE TRIGGER bom_order_line_insert BEFORE INSERT ON bom_order_line FOR EACH ROW EXECUTE FUNCTION bom_guard_order_line();
CREATE TRIGGER bom_order_line_immutable BEFORE UPDATE OR DELETE ON bom_order_line FOR EACH ROW EXECUTE FUNCTION bom_immutable();
-- Check at commit: EF inserts the revision followed by its rows in the same tx.
-- Also check row inserts when callers use SET CONSTRAINTS ... IMMEDIATE.
CREATE CONSTRAINT TRIGGER bom_order_revision_lines AFTER INSERT ON bom_order_revision DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION bom_check_order_lines();
CREATE CONSTRAINT TRIGGER bom_order_line_complete AFTER INSERT ON bom_order_line DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION bom_check_order_lines();
CREATE TRIGGER bom_order_line_no_truncate BEFORE TRUNCATE ON bom_order_line FOR EACH STATEMENT EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_order_revision_no_truncate BEFORE TRUNCATE ON bom_order_revision FOR EACH STATEMENT EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_order_no_truncate BEFORE TRUNCATE ON bom_order FOR EACH STATEMENT EXECUTE FUNCTION bom_immutable();
CREATE OR REPLACE FUNCTION calc_order_bom(order_revision_id int, class_id int DEFAULT NULL)
RETURNS TABLE(product_id int, name text, unit_id int, unit text, required_quantity numeric)
LANGUAGE sql STABLE AS $$
 SELECT b.product_id,b.name,b.unit_id,b.unit,sum(b.required_quantity)
 FROM bom_order_line l CROSS JOIN LATERAL calc_bom(l."VariantId",l."Quantity",class_id) b
 WHERE l."OrderRevisionId"=order_revision_id GROUP BY b.product_id,b.name,b.unit_id,b.unit ORDER BY b.name,b.unit_id
$$;
ALTER TABLE bom_order_revision DROP COLUMN "Lines";
-- max(Id) identifies latest throughout the application: forbid explicit lower IDs.
CREATE FUNCTION bom_guard_revision_sequence() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF EXISTS (SELECT 1 FROM bom_revision WHERE "ProductId"=NEW."ProductId" AND "Id">=NEW."Id")
 THEN RAISE EXCEPTION 'Идентификатор новой версии должен следовать истории.' USING ERRCODE='40001'; END IF;
 RETURN NEW;
END $$;
CREATE TRIGGER bom_validate_sequence BEFORE INSERT ON bom_revision FOR EACH ROW EXECUTE FUNCTION bom_guard_revision_sequence();

-- Unique predecessors also stop forks from sessions with an older RR snapshot.
DROP INDEX "IX_bom_order_revision_OrderId";
CREATE UNIQUE INDEX "IX_bom_order_revision_OrderId_PreviousId" ON bom_order_revision("OrderId","PreviousId") NULLS NOT DISTINCT;
CREATE UNIQUE INDEX "IX_bom_revision_ProductId_PreviousId" ON bom_revision("ProductId","PreviousId") NULLS NOT DISTINCT;
""");
    }
    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("История заказов не удаляется автоматически. Восстановите резервную копию в отдельную БД.");
}
