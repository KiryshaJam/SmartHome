using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartHome.Migrations;

public partial class RemoveProductDraftStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Replace all status-dependent rules before removing the unused column.
        migrationBuilder.Sql("""
CREATE OR REPLACE FUNCTION guard_product_value() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF TG_OP='DELETE' THEN
   IF EXISTS(SELECT 1 FROM product p JOIN class_parameter cp ON cp."Id"=OLD."ClassParameterId" WHERE p."Id"=OLD."ProductId" AND cp."IsRequired") THEN
     RAISE EXCEPTION 'Нельзя очистить обязательное значение параметра.';
   END IF;
   RETURN OLD;
 END IF;
 IF TG_OP='UPDATE' AND (NEW."ProductId",NEW."ClassParameterId") IS DISTINCT FROM (OLD."ProductId",OLD."ClassParameterId") AND EXISTS(
 SELECT 1 FROM product p JOIN class_parameter cp ON cp."Id"=OLD."ClassParameterId" WHERE p."Id"=OLD."ProductId" AND cp."IsRequired" AND p."ClassNodeId"=cp."ClassNodeId") THEN
 RAISE EXCEPTION 'Нельзя перепривязать обязательное значение параметра.';
 END IF;
 PERFORM validate_catalog_value(NEW);
 RETURN NEW;
END $$;

CREATE OR REPLACE FUNCTION guard_product() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF NOT EXISTS(SELECT 1 FROM class_node WHERE "Id"=NEW."ClassNodeId" AND "IsTerminal") THEN RAISE EXCEPTION 'Изделие можно разместить только в конечном классе.'; END IF;
 IF TG_OP='UPDATE' AND NEW."ClassNodeId"<>OLD."ClassNodeId" THEN
   IF EXISTS(SELECT 1 FROM product_parameter_value v JOIN class_parameter src ON src."Id"=v."ClassParameterId"
     LEFT JOIN class_parameter dst ON dst."ClassNodeId"=NEW."ClassNodeId" AND dst."ParameterDefinitionId"=src."ParameterDefinitionId"
     WHERE v."ProductId"=NEW."Id" AND (dst."Id" IS NULL OR coalesce(v."IntegerValue"::numeric,v."NumberValue")<dst."MinNumberValue" OR coalesce(v."IntegerValue"::numeric,v."NumberValue")>dst."MaxNumberValue")) THEN
     RAISE EXCEPTION 'Перенос отклонён: целевой класс не поддерживает сохранённые параметры или их значения.';
   END IF;
 END IF;
 RETURN NEW;
END $$;

CREATE OR REPLACE FUNCTION guard_class_parameter() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE kind integer;
BEGIN
 SELECT "ValueType" INTO kind FROM parameter_definition WHERE "Id"=NEW."ParameterDefinitionId";
 IF NEW."MinNumberValue">NEW."MaxNumberValue" OR (kind NOT IN (1,2) AND num_nonnulls(NEW."MinNumberValue",NEW."MaxNumberValue")>0) THEN RAISE EXCEPTION 'Неверные ограничения параметра.'; END IF;
 IF TG_OP='UPDATE' AND (NEW."ClassNodeId",NEW."ParameterDefinitionId") IS DISTINCT FROM (OLD."ClassNodeId",OLD."ParameterDefinitionId") THEN RAISE EXCEPTION 'Нельзя менять класс или определение существующей привязки.'; END IF;
 IF EXISTS(SELECT 1 FROM product_parameter_value WHERE "ClassParameterId"=NEW."Id" AND (coalesce("IntegerValue"::numeric,"NumberValue")<NEW."MinNumberValue" OR coalesce("IntegerValue"::numeric,"NumberValue")>NEW."MaxNumberValue")) THEN RAISE EXCEPTION 'Новые ограничения нарушают сохранённые значения.'; END IF;
 RETURN NEW;
END $$;

DROP FUNCTION IF EXISTS complete_product(integer);
""");
        migrationBuilder.DropColumn(name: "IsDraft", table: "product");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Восстановите резервную копию в отдельную БД: удалённые статусы нельзя восстановить автоматически.");
    }
}
