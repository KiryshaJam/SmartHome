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


CREATE OR REPLACE FUNCTION is_enum_value_allowed(
    p_enum_class_id INTEGER,
    p_string_value VARCHAR(512),
    p_number_value NUMERIC(18, 4),
    p_icon_value VARCHAR(512)
)
RETURNS BOOLEAN
AS $$
DECLARE
result BOOLEAN;
BEGIN
SELECT EXISTS(
    SELECT 1
    FROM enum_value ev
    WHERE ev."EnumClassId" = p_enum_class_id
      AND (
        (p_string_value IS NOT NULL AND ev."StringValue" = p_string_value)
            OR (p_number_value IS NOT NULL AND ev."NumberValue" = p_number_value)
            OR (p_icon_value IS NOT NULL AND ev."IconValue" = p_icon_value)
        )
) INTO result;

RETURN result;
END;
$$ LANGUAGE plpgsql;


CREATE OR REPLACE FUNCTION update_enum_value_sort_order(
    p_enum_value_id INTEGER,
    p_sort_order INTEGER
)
RETURNS BOOLEAN
AS $$
DECLARE
updated_count INTEGER;
BEGIN
UPDATE enum_value
SET "SortOrder" = p_sort_order
WHERE "Id" = p_enum_value_id;

GET DIAGNOSTICS updated_count = ROW_COUNT;

IF updated_count = 0 THEN
    RAISE EXCEPTION 'Enum value with id % not found', p_enum_value_id;
END IF;

RETURN TRUE;
END;
$$ LANGUAGE plpgsql;


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