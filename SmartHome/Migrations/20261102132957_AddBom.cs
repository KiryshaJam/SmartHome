using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartHome.Migrations
{
    /// <inheritdoc />
    public partial class AddBom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bom_order",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Number = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_order", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "bom_revision",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    UnitId = table.Column<int>(type: "integer", nullable: false),
                    PreviousId = table.Column<int>(type: "integer", nullable: true),
                    BaseRevisionId = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Document = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_revision", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bom_revision_bom_revision_BaseRevisionId",
                        column: x => x.BaseRevisionId,
                        principalTable: "bom_revision",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bom_revision_bom_revision_PreviousId",
                        column: x => x.PreviousId,
                        principalTable: "bom_revision",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bom_revision_measure_unit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "measure_unit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bom_revision_product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bom_order_revision",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    PreviousId = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Lines = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_order_revision", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bom_order_revision_bom_order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "bom_order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bom_order_revision_bom_order_revision_PreviousId",
                        column: x => x.PreviousId,
                        principalTable: "bom_order_revision",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bom_variant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RevisionId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Snapshot = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_variant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bom_variant_bom_revision_RevisionId",
                        column: x => x.RevisionId,
                        principalTable: "bom_revision",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bom_order_Number",
                table: "bom_order",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bom_order_revision_OrderId",
                table: "bom_order_revision",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_bom_order_revision_PreviousId",
                table: "bom_order_revision",
                column: "PreviousId");

            migrationBuilder.CreateIndex(
                name: "IX_bom_revision_BaseRevisionId",
                table: "bom_revision",
                column: "BaseRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_bom_revision_PreviousId",
                table: "bom_revision",
                column: "PreviousId");

            migrationBuilder.CreateIndex(
                name: "IX_bom_revision_ProductId_Id",
                table: "bom_revision",
                columns: new[] { "ProductId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_bom_revision_UnitId",
                table: "bom_revision",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_bom_variant_RevisionId",
                table: "bom_variant",
                column: "RevisionId");
            migrationBuilder.Sql("""
-- Historical revisions are append-only. Each position pins the child revision.
CREATE FUNCTION bom_effective_positions(rid integer, nesting integer DEFAULT 0) RETURNS jsonb LANGUAGE plpgsql STABLE AS $$
DECLARE r bom_revision; inherited jsonb := '[]'; own jsonb;
BEGIN
 IF nesting>32 THEN RAISE EXCEPTION 'Слишком длинная цепочка модификаций.'; END IF;
 SELECT * INTO STRICT r FROM bom_revision WHERE "Id"=rid;
 own := r."Document"->'positions';
 IF r."BaseRevisionId" IS NOT NULL THEN inherited := bom_effective_positions(r."BaseRevisionId",nesting+1); END IF;
 RETURN coalesce((SELECT jsonb_agg(p ORDER BY (p->>'sortOrder')::int,p->>'id') FROM (
   SELECT i.value AS p FROM jsonb_array_elements(inherited) i WHERE NOT EXISTS(
     SELECT 1 FROM jsonb_array_elements(own) o WHERE o.value->>'id'=i.value->>'id')
   UNION ALL SELECT value FROM jsonb_array_elements(own)
 ) s WHERE NOT coalesce((p->>'excluded')::boolean,false)), '[]');
END $$;

CREATE FUNCTION bom_immutable() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN RAISE EXCEPTION 'История и зафиксированные варианты неизменяемы. Создайте новую версию.'; END $$;

CREATE FUNCTION bom_guard_revision() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE last_id integer; b bom_revision; pos jsonb; child bom_revision; cycle_found boolean;
BEGIN
 PERFORM pg_advisory_xact_lock(734021,1);
 SELECT max("Id") INTO last_id FROM bom_revision WHERE "ProductId"=NEW."ProductId";
 IF NEW."PreviousId" IS DISTINCT FROM last_id THEN RAISE EXCEPTION 'Спецификация уже изменена. Обновите страницу.' USING ERRCODE='40001'; END IF;
 IF length(trim(NEW."Reason"))=0 THEN RAISE EXCEPTION 'Укажите причину изменения.'; END IF;
 IF last_id IS NOT NULL AND EXISTS(SELECT 1 FROM bom_revision WHERE "ProductId"=NEW."ProductId" AND ("UnitId"<>NEW."UnitId" OR "BaseRevisionId" IS DISTINCT FROM NEW."BaseRevisionId")) THEN
 RAISE EXCEPTION 'Единица изделия и версия базы фиксированы первой спецификацией.'; END IF;
 IF NEW."BaseRevisionId" IS NOT NULL THEN
   SELECT * INTO STRICT b FROM bom_revision WHERE "Id"=NEW."BaseRevisionId";
   IF b."ProductId"=NEW."ProductId" OR b."UnitId"<>NEW."UnitId" THEN RAISE EXCEPTION 'Неверная база модификации или единица.'; END IF;
 END IF;
 IF jsonb_typeof(NEW."Document"->'parameters') IS DISTINCT FROM 'array' OR jsonb_typeof(NEW."Document"->'rules') IS DISTINCT FROM 'array' OR jsonb_typeof(NEW."Document"->'calculations') IS DISTINCT FROM 'object' THEN RAISE EXCEPTION 'Неверная структура параметров и правил.'; END IF;
 IF jsonb_typeof(NEW."Document"->'positions') IS DISTINCT FROM 'array' THEN RAISE EXCEPTION 'Ожидался список позиций.'; END IF;
 IF (SELECT count(*)<>count(DISTINCT x->>'id') FROM jsonb_array_elements(NEW."Document"->'positions') x) THEN RAISE EXCEPTION 'Повторяется идентификатор позиции.'; END IF;
 FOR pos IN SELECT value FROM jsonb_array_elements(NEW."Document"->'positions') LOOP
   IF coalesce(pos->>'id','') !~ '^[A-Za-z0-9_-]{1,64}$' THEN RAISE EXCEPTION 'Неверный идентификатор позиции.'; END IF;
   IF coalesce((pos->>'excluded')::boolean,false) THEN
     IF NEW."BaseRevisionId" IS NULL OR NOT EXISTS(SELECT 1 FROM jsonb_array_elements(bom_effective_positions(NEW."BaseRevisionId")) x WHERE x->>'id'=pos->>'id') THEN RAISE EXCEPTION 'Исключать можно только позицию базы.'; END IF;
     CONTINUE;
   END IF;
   SELECT * INTO child FROM bom_revision WHERE "Id"=(pos->>'revisionId')::integer;
   IF child."Id" IS NULL OR child."ProductId" IS DISTINCT FROM (pos->>'componentId')::int OR child."UnitId" IS DISTINCT FROM (pos->>'unitId')::int THEN RAISE EXCEPTION 'Компонент, версия и единица не согласованы.'; END IF;
   IF NOT coalesce((pos->>'amount')::numeric>0 AND (pos->>'amount')::numeric<=1000000000 AND (pos->>'forQuantity')::numeric>0 AND (pos->>'forQuantity')::numeric<=1000000000 AND (pos->>'amount')::numeric/nullif((pos->>'forQuantity')::numeric,0)<=1000000000,false) THEN RAISE EXCEPTION 'Нормы должны быть положительными и ограниченными.'; END IF;
   IF pos->>'childVariantId' IS NOT NULL AND NOT EXISTS(SELECT 1 FROM bom_variant WHERE "Id"=(pos->>'childVariantId')::int AND "RevisionId"=child."Id") THEN RAISE EXCEPTION 'Вложенный вариант другой версии.'; END IF;
 END LOOP;
 RETURN NEW;
END $$;

CREATE FUNCTION bom_guard_cycles() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE bad boolean;
BEGIN
 -- Check the current graph as well as pinned historical paths. The statement lock
 -- serializes opposing A->B / B->A writes, including ordinary direct SQL inserts.
 WITH RECURSIVE latest AS (SELECT DISTINCT ON ("ProductId") "ProductId", "Id" FROM bom_revision ORDER BY "ProductId","Id" DESC),
 edges AS (SELECT l."ProductId" p, (x->>'componentId')::int c FROM latest l CROSS JOIN LATERAL jsonb_array_elements(bom_effective_positions(l."Id")) x),
 walk AS (SELECT NEW."ProductId" AS node, ARRAY[NEW."ProductId"] AS path, false AS cycle
 UNION ALL SELECT e.c, w.path||e.c, e.c=ANY(w.path) FROM walk w JOIN edges e ON e.p=w.node WHERE NOT w.cycle AND cardinality(w.path)<=33)
 SELECT coalesce(bool_or(cycle OR cardinality(path)>33),false) INTO bad FROM walk;
 IF bad THEN RAISE EXCEPTION 'Цикл или превышение глубины состава.'; END IF;
 WITH RECURSIVE walk AS (
 SELECT NEW."Id" AS rid, ARRAY[NEW."ProductId"] AS path,false AS cycle
 UNION ALL SELECT c."Id",w.path||c."ProductId",c."ProductId"=ANY(w.path)
 FROM walk w CROSS JOIN LATERAL jsonb_array_elements(bom_effective_positions(w.rid)) x
 JOIN bom_revision c ON c."Id"=(x->>'revisionId')::int WHERE NOT w.cycle AND cardinality(w.path)<=33)
 SELECT coalesce(bool_or(cycle OR cardinality(path)>33),false) INTO bad FROM walk;
 IF bad THEN RAISE EXCEPTION 'Цикл в зафиксированной версии состава.'; END IF;
 RETURN NEW;
END $$;

CREATE TRIGGER catalog_write_lock BEFORE INSERT OR UPDATE OR DELETE ON bom_revision FOR EACH STATEMENT EXECUTE FUNCTION lock_catalog_write();
CREATE TRIGGER bom_validate BEFORE INSERT ON bom_revision FOR EACH ROW EXECUTE FUNCTION bom_guard_revision();
CREATE TRIGGER bom_cycles AFTER INSERT ON bom_revision FOR EACH ROW EXECUTE FUNCTION bom_guard_cycles();
CREATE TRIGGER bom_revision_immutable BEFORE UPDATE OR DELETE ON bom_revision FOR EACH ROW EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_order_immutable BEFORE UPDATE OR DELETE ON bom_order FOR EACH ROW EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_variant_immutable BEFORE UPDATE OR DELETE ON bom_variant FOR EACH ROW EXECUTE FUNCTION bom_immutable();
CREATE TRIGGER bom_order_revision_immutable BEFORE UPDATE OR DELETE ON bom_order_revision FOR EACH ROW EXECUTE FUNCTION bom_immutable();

CREATE FUNCTION bom_guard_order() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE last_id int; line jsonb;
BEGIN
 PERFORM pg_advisory_xact_lock(734021,1);
 SELECT max("Id") INTO last_id FROM bom_order_revision WHERE "OrderId"=NEW."OrderId";
 IF NEW."PreviousId" IS DISTINCT FROM last_id THEN RAISE EXCEPTION 'Заказ уже изменён.' USING ERRCODE='40001'; END IF;
 IF length(trim(NEW."Reason"))=0 OR jsonb_typeof(NEW."Lines")<>'array' OR jsonb_array_length(NEW."Lines")=0 THEN RAISE EXCEPTION 'Нужны причина изменения и позиции заказа.'; END IF;
 FOR line IN SELECT value FROM jsonb_array_elements(NEW."Lines") LOOP
 IF NOT coalesce((line->>'quantity')::numeric>0 AND (line->>'quantity')::numeric<=1000000000,false) OR NOT EXISTS(SELECT 1 FROM bom_variant WHERE "Id"=(line->>'variantId')::int) THEN RAISE EXCEPTION 'Неверный вариант или количество заказа.'; END IF;
 END LOOP;
 RETURN NEW;
END $$;
CREATE TRIGGER catalog_write_lock BEFORE INSERT OR UPDATE OR DELETE ON bom_order_revision FOR EACH STATEMENT EXECUTE FUNCTION lock_catalog_write();
CREATE TRIGGER bom_validate_order BEFORE INSERT ON bom_order_revision FOR EACH ROW EXECUTE FUNCTION bom_guard_order();

-- The API and SQL clients read and aggregate the same frozen configuration.
CREATE FUNCTION find_bom(variant_id int, quantity numeric DEFAULT 1)
RETURNS TABLE(path text, depth int, product_id int, name text, unit_id int, unit text, required_quantity numeric, classes jsonb, leaf boolean, revision_id int, norm numeric, for_quantity numeric, position_id text)
LANGUAGE plpgsql STABLE AS $$
BEGIN
 IF NOT (quantity>0 AND quantity<=1000000000) OR quantity IS NULL THEN RAISE EXCEPTION 'Количество должно быть положительным и ограниченным.'; END IF;
 IF NOT EXISTS(SELECT 1 FROM bom_variant WHERE "Id"=variant_id) THEN RAISE EXCEPTION 'Вариант не найден.'; END IF;
 RETURN QUERY WITH RECURSIVE tree AS (
 SELECT v."Snapshot" n, ''::text p, 0 d, quantity q FROM bom_variant v WHERE v."Id"=variant_id
 UNION ALL SELECT x, t.p||'/'||(x->>'positionId'), t.d+1, t.q*(x->>'quantity')::numeric FROM tree t CROSS JOIN LATERAL jsonb_array_elements(t.n->'children') x
 ) SELECT t.p,t.d,(t.n->>'productId')::int,t.n->>'name',(t.n->>'unitId')::int,t.n->>'unit',t.q,t.n->'classes',jsonb_array_length(t.n->'children')=0,(t.n->>'revisionId')::int,coalesce((t.n->>'amount')::numeric,(t.n->>'quantity')::numeric),coalesce((t.n->>'forQuantity')::numeric,1),t.n->>'positionId' FROM tree t ORDER BY t.p;
END $$;
CREATE FUNCTION calc_bom(variant_id int, quantity numeric DEFAULT 1, class_id int DEFAULT NULL)
RETURNS TABLE(product_id int, name text, unit_id int, unit text, required_quantity numeric)
LANGUAGE sql STABLE AS $$
 SELECT b.product_id,b.name,b.unit_id,b.unit,sum(b.required_quantity) FROM find_bom(variant_id,quantity) b
 WHERE b.leaf AND (class_id IS NULL OR b.classes @> to_jsonb(ARRAY[class_id]))
 GROUP BY b.product_id,b.name,b.unit_id,b.unit ORDER BY b.name,b.unit_id
$$;
CREATE FUNCTION calc_order_bom(order_revision_id int, class_id int DEFAULT NULL)
RETURNS TABLE(product_id int, name text, unit_id int, unit text, required_quantity numeric)
LANGUAGE sql STABLE AS $$
 SELECT b.product_id,b.name,b.unit_id,b.unit,sum(b.required_quantity)
 FROM bom_order_revision r CROSS JOIN LATERAL jsonb_array_elements(r."Lines") l
 CROSS JOIN LATERAL calc_bom((l->>'variantId')::int,(l->>'quantity')::numeric,class_id) b
 WHERE r."Id"=order_revision_id GROUP BY b.product_id,b.name,b.unit_id,b.unit ORDER BY b.name,b.unit_id
$$;

CREATE FUNCTION register_change(product_id int, unit_id int, expected_revision_id int, base_revision_id int, document jsonb, reason text) RETURNS int LANGUAGE plpgsql AS $$
DECLARE rid int;
BEGIN
 INSERT INTO bom_revision("ProductId","UnitId","PreviousId","BaseRevisionId","Document","Reason","CreatedAt")
 VALUES(product_id,unit_id,expected_revision_id,base_revision_id,document,reason,now()) RETURNING "Id" INTO rid;
 RETURN rid;
END $$;
CREATE FUNCTION create_modification(base_revision_id int, name text, short_name text, reason text) RETURNS int LANGUAGE plpgsql AS $$
DECLARE b bom_revision; pid int; doc jsonb;
BEGIN
 PERFORM pg_advisory_xact_lock(734021,1);
 SELECT * INTO STRICT b FROM bom_revision WHERE "Id"=base_revision_id;
 INSERT INTO product("Name","ShortName","ClassNodeId") SELECT name,short_name,"ClassNodeId" FROM product WHERE "Id"=b."ProductId" RETURNING "Id" INTO pid;
 doc:=jsonb_set(jsonb_set(b."Document",'{positions}','[]'),'{productName}',to_jsonb(name));
 PERFORM register_change(pid,b."UnitId",NULL,b."Id",doc,reason);
 RETURN pid;
END $$;

-- Position-level SQL operations retain the revision, reason and optimistic check.
CREATE FUNCTION bom_put_position(product_id int, expected_revision_id int, item jsonb, reason text) RETURNS int LANGUAGE plpgsql AS $$
DECLARE r bom_revision; positions jsonb;
BEGIN
 PERFORM pg_advisory_xact_lock(734021,1);
 SELECT * INTO STRICT r FROM bom_revision WHERE "Id"=expected_revision_id AND "ProductId"=product_id;
 SELECT coalesce(jsonb_agg(x),'[]') INTO positions FROM jsonb_array_elements(r."Document"->'positions') x WHERE x->>'id' IS DISTINCT FROM item->>'id';
 RETURN register_change(product_id,r."UnitId",r."Id",r."BaseRevisionId",jsonb_set(r."Document",'{positions}',positions||jsonb_build_array(item)),reason);
END $$;
CREATE FUNCTION bom_remove_position(product_id int, expected_revision_id int, position_id text, reason text) RETURNS int LANGUAGE plpgsql AS $$
DECLARE r bom_revision; positions jsonb;
BEGIN
 PERFORM pg_advisory_xact_lock(734021,1);
 SELECT * INTO STRICT r FROM bom_revision WHERE "Id"=expected_revision_id AND "ProductId"=product_id;
 IF r."BaseRevisionId" IS NOT NULL AND EXISTS(SELECT 1 FROM jsonb_array_elements(bom_effective_positions(r."BaseRevisionId")) x WHERE x->>'id'=position_id) THEN
 RETURN bom_put_position(product_id,expected_revision_id,jsonb_build_object('id',position_id,'excluded',true),reason);
 END IF;
 IF NOT EXISTS(SELECT 1 FROM jsonb_array_elements(r."Document"->'positions') x WHERE x->>'id'=position_id) THEN RAISE EXCEPTION 'Позиция не найдена.'; END IF;
 SELECT coalesce(jsonb_agg(x),'[]') INTO positions FROM jsonb_array_elements(r."Document"->'positions') x WHERE x->>'id'<>position_id;
 RETURN register_change(product_id,r."UnitId",r."Id",r."BaseRevisionId",jsonb_set(r."Document",'{positions}',positions),reason);
END $$;

""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Восстановите резервную копию в отдельную БД: история спецификаций и заказов не удаляется автоматически.");
        }
    }
}
