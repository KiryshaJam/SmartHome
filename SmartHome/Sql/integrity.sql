-- Reject inconsistent existing data before enabling constraints. No values are removed.
CREATE OR REPLACE FUNCTION validate_catalog_value(v product_parameter_value) RETURNS void LANGUAGE plpgsql AS $$
DECLARE cp class_parameter; pd parameter_definition; product_class integer; n numeric;
BEGIN
 SELECT * INTO STRICT cp FROM class_parameter WHERE "Id"=v."ClassParameterId";
 SELECT * INTO STRICT pd FROM parameter_definition WHERE "Id"=cp."ParameterDefinitionId";
 SELECT "ClassNodeId" INTO product_class FROM product WHERE "Id"=v."ProductId";
 IF cp."ClassNodeId" IS DISTINCT FROM product_class THEN RAISE EXCEPTION 'Параметр не принадлежит классу изделия.'; END IF;
 IF num_nonnulls(v."IntegerValue",v."NumberValue",v."StringValue",v."DateTimeValue",v."EnumValueId") <> 1 THEN RAISE EXCEPTION 'Укажите ровно одно поле значения.'; END IF;
 IF NOT (CASE pd."ValueType"
 WHEN 1 THEN v."IntegerValue" IS NOT NULL
 WHEN 2 THEN v."NumberValue" IS NOT NULL
 WHEN 3 THEN v."StringValue" IS NOT NULL AND length(btrim(v."StringValue"))>0
 WHEN 4 THEN v."DateTimeValue" IS NOT NULL AND isfinite(v."DateTimeValue")
 WHEN 5 THEN EXISTS(SELECT 1 FROM enum_value WHERE "Id"=v."EnumValueId" AND "EnumClassId"=pd."EnumClassId")
 ELSE false END) THEN RAISE EXCEPTION 'Значение не соответствует типу или перечислению параметра.'; END IF;
 n := coalesce(v."IntegerValue"::numeric,v."NumberValue");
 IF n < cp."MinNumberValue" OR n > cp."MaxNumberValue" OR n::text IN ('NaN','Infinity','-Infinity') THEN RAISE EXCEPTION 'Значение выходит за ограничения параметра.'; END IF;
END $$;

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
DROP TRIGGER IF EXISTS guard_product_value ON product_parameter_value;
CREATE TRIGGER guard_product_value BEFORE INSERT OR UPDATE OR DELETE ON product_parameter_value FOR EACH ROW EXECUTE FUNCTION guard_product_value();

CREATE OR REPLACE FUNCTION guard_class_node() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF EXISTS(SELECT 1 FROM class_node WHERE "Id"=NEW."ParentId" AND "IsTerminal") THEN RAISE EXCEPTION 'Конечный класс не может иметь подклассы.'; END IF;
 IF NEW."IsTerminal" AND EXISTS(SELECT 1 FROM class_node WHERE "ParentId"=NEW."Id") THEN RAISE EXCEPTION 'У класса есть подклассы.'; END IF;
 IF NOT NEW."IsTerminal" AND EXISTS(SELECT 1 FROM product WHERE "ClassNodeId"=NEW."Id") THEN RAISE EXCEPTION 'Класс содержит изделия и должен оставаться конечным.'; END IF;
 RETURN NEW;
END $$;
DROP TRIGGER IF EXISTS guard_class_node ON class_node;
CREATE TRIGGER guard_class_node BEFORE INSERT OR UPDATE ON class_node FOR EACH ROW EXECUTE FUNCTION guard_class_node();

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
DROP TRIGGER IF EXISTS guard_product ON product;
CREATE TRIGGER guard_product BEFORE INSERT OR UPDATE ON product FOR EACH ROW EXECUTE FUNCTION guard_product();

CREATE OR REPLACE FUNCTION remap_product_values() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF NEW."ClassNodeId"<>OLD."ClassNodeId" THEN
 UPDATE product_parameter_value v SET "ClassParameterId"=dst."Id"
 FROM class_parameter src JOIN class_parameter dst ON dst."ParameterDefinitionId"=src."ParameterDefinitionId" AND dst."ClassNodeId"=NEW."ClassNodeId"
 WHERE v."ProductId"=NEW."Id" AND v."ClassParameterId"=src."Id";
 END IF;
 RETURN NEW;
END $$;
DROP TRIGGER IF EXISTS remap_product_values ON product;
CREATE TRIGGER remap_product_values AFTER UPDATE OF "ClassNodeId" ON product FOR EACH ROW EXECUTE FUNCTION remap_product_values();

CREATE OR REPLACE FUNCTION guard_class_parameter() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE kind integer;
BEGIN
 SELECT "ValueType" INTO kind FROM parameter_definition WHERE "Id"=NEW."ParameterDefinitionId";
 IF NEW."MinNumberValue">NEW."MaxNumberValue" OR (kind NOT IN (1,2) AND num_nonnulls(NEW."MinNumberValue",NEW."MaxNumberValue")>0) THEN RAISE EXCEPTION 'Неверные ограничения параметра.'; END IF;
 IF TG_OP='UPDATE' AND (NEW."ClassNodeId",NEW."ParameterDefinitionId") IS DISTINCT FROM (OLD."ClassNodeId",OLD."ParameterDefinitionId") THEN RAISE EXCEPTION 'Нельзя менять класс или определение существующей привязки.'; END IF;
 IF EXISTS(SELECT 1 FROM product_parameter_value WHERE "ClassParameterId"=NEW."Id" AND (coalesce("IntegerValue"::numeric,"NumberValue")<NEW."MinNumberValue" OR coalesce("IntegerValue"::numeric,"NumberValue")>NEW."MaxNumberValue")) THEN RAISE EXCEPTION 'Новые ограничения нарушают сохранённые значения.'; END IF;
 RETURN NEW;
END $$;
DROP TRIGGER IF EXISTS guard_class_parameter ON class_parameter;
CREATE TRIGGER guard_class_parameter BEFORE INSERT OR UPDATE ON class_parameter FOR EACH ROW EXECUTE FUNCTION guard_class_parameter();

CREATE OR REPLACE FUNCTION guard_enum_value() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE kind integer;
BEGIN
 SELECT "ValueType" INTO kind FROM enum_class WHERE "Id"=NEW."EnumClassId";
 IF num_nonnulls(NEW."StringValue",NEW."NumberValue",NEW."IconValue")<>1 OR NOT (CASE kind WHEN 1 THEN length(btrim(NEW."StringValue"))>0 WHEN 2 THEN NEW."NumberValue" IS NOT NULL AND NEW."NumberValue"::text NOT IN ('NaN','Infinity','-Infinity') WHEN 3 THEN length(btrim(NEW."IconValue"))>0 ELSE false END) THEN RAISE EXCEPTION 'Укажите одно непустое значение подходящего типа перечисления.'; END IF;
 IF (kind=1 AND NEW."StringValue" IS NULL) OR (kind=3 AND NEW."IconValue" IS NULL) THEN RAISE EXCEPTION 'Неверный тип значения перечисления.'; END IF;
 IF EXISTS(SELECT 1 FROM enum_value WHERE "EnumClassId"=NEW."EnumClassId" AND "Id"<>NEW."Id" AND ("StringValue"=NEW."StringValue" OR "NumberValue"=NEW."NumberValue" OR "IconValue"=NEW."IconValue")) THEN RAISE EXCEPTION 'Такое значение уже есть в перечислении.'; END IF;
 IF TG_OP='UPDATE' AND (NEW."EnumClassId",NEW."StringValue",NEW."NumberValue",NEW."IconValue") IS DISTINCT FROM (OLD."EnumClassId",OLD."StringValue",OLD."NumberValue",OLD."IconValue") AND EXISTS(SELECT 1 FROM product_parameter_value WHERE "EnumValueId"=OLD."Id") THEN RAISE EXCEPTION 'Используемое значение нельзя менять; добавьте новое.'; END IF;
 RETURN NEW;
END $$;
DROP TRIGGER IF EXISTS guard_enum_value ON enum_value;
CREATE TRIGGER guard_enum_value BEFORE INSERT OR UPDATE ON enum_value FOR EACH ROW EXECUTE FUNCTION guard_enum_value();

CREATE OR REPLACE FUNCTION guard_definition_type() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF TG_TABLE_NAME='parameter_definition' THEN
   IF (NEW."ValueType",NEW."EnumClassId") IS DISTINCT FROM (OLD."ValueType",OLD."EnumClassId") AND EXISTS(SELECT 1 FROM class_parameter WHERE "ParameterDefinitionId"=OLD."Id") THEN RAISE EXCEPTION 'Нельзя менять тип используемого параметра.'; END IF;
 ELSE
   IF NEW."ValueType"<>OLD."ValueType" AND EXISTS(SELECT 1 FROM enum_value WHERE "EnumClassId"=OLD."Id") THEN RAISE EXCEPTION 'Нельзя менять тип непустого перечисления.'; END IF;
 END IF;
 RETURN NEW;
END $$;
DROP TRIGGER IF EXISTS guard_definition_type ON parameter_definition;
CREATE TRIGGER guard_definition_type BEFORE UPDATE ON parameter_definition FOR EACH ROW EXECUTE FUNCTION guard_definition_type();
DROP TRIGGER IF EXISTS guard_enum_type ON enum_class;
CREATE TRIGGER guard_enum_type BEFORE UPDATE ON enum_class FOR EACH ROW EXECUTE FUNCTION guard_definition_type();

ALTER TABLE parameter_definition DROP CONSTRAINT IF EXISTS valid_parameter_type;
ALTER TABLE parameter_definition ADD CONSTRAINT valid_parameter_type CHECK (("MeasureUnitId" IS NULL OR "ValueType" IN (1,2)) AND "ValueType" BETWEEN 1 AND 5 AND (("ValueType"=5)=("EnumClassId" IS NOT NULL)));
ALTER TABLE enum_class DROP CONSTRAINT IF EXISTS valid_enum_type;
ALTER TABLE enum_class ADD CONSTRAINT valid_enum_type CHECK ("ValueType" BETWEEN 1 AND 3 AND (("ValueType"=2)=("MeasureUnitId" IS NOT NULL)));
DO $$ DECLARE v product_parameter_value; BEGIN
 IF EXISTS(SELECT 1 FROM class_node n WHERE n."IsTerminal" AND EXISTS(SELECT 1 FROM class_node c WHERE c."ParentId"=n."Id")) OR EXISTS(SELECT 1 FROM product p JOIN class_node c ON c."Id"=p."ClassNodeId" WHERE NOT c."IsTerminal") THEN RAISE EXCEPTION 'Существующие данные нарушают терминальность классов. Исправьте их перед миграцией.'; END IF;
 FOR v IN SELECT * FROM product_parameter_value LOOP PERFORM validate_catalog_value(v); END LOOP;
 -- Revalidate existing enum values and class restrictions without deleting records.
 UPDATE enum_value SET "SortOrder"="SortOrder";
 UPDATE class_parameter SET "SortOrder"="SortOrder";
END $$;
