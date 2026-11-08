CREATE OR REPLACE FUNCTION get_enum_values(p_enum_class_id INTEGER)
RETURNS TABLE (
    "Id" INTEGER,
    "EnumClassId" INTEGER,
    "StringValue" VARCHAR(512),
    "NumberValue" NUMERIC(18, 4),
    "IconValue" VARCHAR(512),
    "DisplayName" VARCHAR(512),
    "SortOrder" INTEGER
)
AS $$
BEGIN
RETURN QUERY
SELECT
    ev."Id",
    ev."EnumClassId",
    ev."StringValue",
    ev."NumberValue",
    ev."IconValue",
    ev."DisplayName",
    ev."SortOrder"
FROM enum_value ev
WHERE ev."EnumClassId" = p_enum_class_id
ORDER BY ev."SortOrder", ev."DisplayName";
END;
$$ LANGUAGE plpgsql;


CREATE OR REPLACE FUNCTION add_enum_value(
    p_enum_class_id INTEGER,
    p_string_value VARCHAR(512),
    p_number_value NUMERIC(18, 4),
    p_icon_value VARCHAR(512),
    p_display_name VARCHAR(512),
    p_sort_order INTEGER
)
RETURNS INTEGER
AS $$
DECLARE
enum_exists BOOLEAN;
    new_id INTEGER;
BEGIN
SELECT EXISTS(
    SELECT 1 FROM enum_class WHERE "Id" = p_enum_class_id
) INTO enum_exists;

IF NOT enum_exists THEN
    RAISE EXCEPTION 'Enum class with id % not found', p_enum_class_id;
END IF;

INSERT INTO enum_value (
    "EnumClassId",
    "StringValue",
    "NumberValue",
    "IconValue",
    "DisplayName",
    "SortOrder"
)
VALUES (
           p_enum_class_id,
           p_string_value,
           p_number_value,
           p_icon_value,
           p_display_name,
           p_sort_order
       )
    RETURNING "Id" INTO new_id;

RETURN new_id;
END;
$$ LANGUAGE plpgsql;


CREATE OR REPLACE FUNCTION is_enum_value_allowed(p_enum_class_id integer,p_string_value varchar,p_number_value numeric,p_icon_value varchar) RETURNS boolean LANGUAGE sql AS $$
 SELECT num_nonnulls(p_string_value,p_number_value,p_icon_value)=1 AND EXISTS(
 SELECT 1 FROM enum_value ev JOIN enum_class ec ON ec."Id"=ev."EnumClassId" WHERE ec."Id"=p_enum_class_id AND
 CASE ec."ValueType" WHEN 1 THEN ev."StringValue"=p_string_value WHEN 2 THEN ev."NumberValue"=p_number_value WHEN 3 THEN ev."IconValue"=p_icon_value ELSE false END) $$;


CREATE OR REPLACE FUNCTION update_enum_value_sort_order(p_enum_value_id integer,p_sort_order integer) RETURNS boolean LANGUAGE plpgsql AS $$
DECLARE source enum_value; other integer; temporary integer := -2147483648;
BEGIN
 PERFORM pg_advisory_xact_lock(734021,1);
 SELECT * INTO source FROM enum_value WHERE "Id"=p_enum_value_id;
 IF NOT FOUND THEN RAISE EXCEPTION 'Значение перечисления не найдено.'; END IF;
 IF source."SortOrder"=p_sort_order THEN RETURN true; END IF;
 SELECT "Id" INTO other FROM enum_value WHERE "EnumClassId"=source."EnumClassId" AND "SortOrder"=p_sort_order;
 IF other IS NOT NULL THEN
   WHILE EXISTS(SELECT 1 FROM enum_value WHERE "EnumClassId"=source."EnumClassId" AND "SortOrder"=temporary) LOOP temporary:=temporary+1; END LOOP;
   UPDATE enum_value SET "SortOrder"=temporary WHERE "Id"=other;
 END IF;
 UPDATE enum_value SET "SortOrder"=p_sort_order WHERE "Id"=p_enum_value_id;
 IF other IS NOT NULL THEN UPDATE enum_value SET "SortOrder"=source."SortOrder" WHERE "Id"=other; END IF;
 RETURN true;
END $$;


CREATE OR REPLACE FUNCTION get_enum_classes()
RETURNS TABLE (
    "Id" INTEGER,
    "Name" VARCHAR(256),
    "ShortName" VARCHAR(128),
    "ValueType" INTEGER,
    "SortOrder" INTEGER,
    "MeasureUnitId" INTEGER
)
AS $$
BEGIN
RETURN QUERY
SELECT
    ec."Id",
    ec."Name",
    ec."ShortName",
    ec."ValueType",
    ec."SortOrder",
    ec."MeasureUnitId"
FROM enum_class ec
ORDER BY ec."SortOrder", ec."Name";
END;
$$ LANGUAGE plpgsql;