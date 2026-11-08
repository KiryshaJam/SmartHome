using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartHome.Data;

namespace SmartHome.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260926000100_InstallCatalogSql")]
public class InstallCatalogSql : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
CREATE OR REPLACE FUNCTION find_children(p_class_id INTEGER)
RETURNS TABLE (
    "Id" INTEGER,
    "Name" VARCHAR(256),
    "ShortName" VARCHAR(128),
    "ParentId" INTEGER,
    "MeasureUnitId" INTEGER,
    "IsTerminal" BOOLEAN,
    "SortOrder" INTEGER,
    "Level" INTEGER
)
AS $$
BEGIN
RETURN QUERY
    WITH RECURSIVE descendants AS (
        SELECT
            c."Id",
            c."Name",
            c."ShortName",
            c."ParentId",
            c."MeasureUnitId",
            c."IsTerminal",
            c."SortOrder",
            1 AS "Level"
        FROM class_node c
        WHERE c."Id" = p_class_id

        UNION ALL

        SELECT
            c."Id",
            c."Name",
            c."ShortName",
            c."ParentId",
            c."MeasureUnitId",
            c."IsTerminal",
            c."SortOrder",
            d."Level" + 1
        FROM class_node c
        INNER JOIN descendants d ON c."ParentId" = d."Id"
    )
SELECT
    d."Id",
    d."Name",
    d."ShortName",
    d."ParentId",
    d."MeasureUnitId",
    d."IsTerminal",
    d."SortOrder",
    d."Level"
FROM descendants d
ORDER BY d."Level", d."SortOrder", d."Name";
END;
$$ LANGUAGE plpgsql;


CREATE OR REPLACE FUNCTION find_parents(p_class_id INTEGER)
RETURNS TABLE (
    "Id" INTEGER,
    "Name" VARCHAR(256),
    "ShortName" VARCHAR(128),
    "ParentId" INTEGER,
    "MeasureUnitId" INTEGER,
    "IsTerminal" BOOLEAN,
    "SortOrder" INTEGER,
    "Level" INTEGER
)
AS $$
BEGIN
RETURN QUERY
    WITH RECURSIVE ancestors AS (
        SELECT
            c."Id",
            c."Name",
            c."ShortName",
            c."ParentId",
            c."MeasureUnitId",
            c."IsTerminal",
            c."SortOrder",
            1 AS "Level"
        FROM class_node c
        WHERE c."Id" = p_class_id

        UNION ALL

        SELECT
            p."Id",
            p."Name",
            p."ShortName",
            p."ParentId",
            p."MeasureUnitId",
            p."IsTerminal",
            p."SortOrder",
            a."Level" + 1
        FROM class_node p
        INNER JOIN ancestors a ON a."ParentId" = p."Id"
    )
SELECT
    a."Id",
    a."Name",
    a."ShortName",
    a."ParentId",
    a."MeasureUnitId",
    a."IsTerminal",
    a."SortOrder",
    a."Level"
FROM ancestors a
ORDER BY a."Level";
END;
$$ LANGUAGE plpgsql;


CREATE OR REPLACE FUNCTION find_leaves_by_class(p_class_id INTEGER)
RETURNS TABLE (
    "Id" INTEGER,
    "Name" VARCHAR(256),
    "ShortName" VARCHAR(128),
    "ParentId" INTEGER,
    "MeasureUnitId" INTEGER,
    "IsTerminal" BOOLEAN,
    "SortOrder" INTEGER
)
AS $$
BEGIN
RETURN QUERY
    WITH RECURSIVE subtree AS (
        SELECT
            c."Id",
            c."Name",
            c."ShortName",
            c."ParentId",
            c."MeasureUnitId",
            c."IsTerminal",
            c."SortOrder"
        FROM class_node c
        WHERE c."Id" = p_class_id

        UNION ALL

        SELECT
            c."Id",
            c."Name",
            c."ShortName",
            c."ParentId",
            c."MeasureUnitId",
            c."IsTerminal",
            c."SortOrder"
        FROM class_node c
        INNER JOIN subtree s ON c."ParentId" = s."Id"
    )
SELECT
    s."Id",
    s."Name",
    s."ShortName",
    s."ParentId",
    s."MeasureUnitId",
    s."IsTerminal",
    s."SortOrder"
FROM subtree s
WHERE s."IsTerminal" = TRUE
ORDER BY s."SortOrder", s."Name";
END;
$$ LANGUAGE plpgsql;


CREATE OR REPLACE FUNCTION move_class_node(
    p_class_id INTEGER,
    p_new_parent_id INTEGER
)
RETURNS BOOLEAN
AS $$
DECLARE
class_exists BOOLEAN;
    parent_exists BOOLEAN;
BEGIN
SELECT EXISTS(
    SELECT 1 FROM class_node WHERE "Id" = p_class_id
) INTO class_exists;

IF NOT class_exists THEN
        RAISE EXCEPTION 'Class node with id % not found', p_class_id;
END IF;

    IF p_new_parent_id IS NOT NULL THEN
SELECT EXISTS(
    SELECT 1 FROM class_node WHERE "Id" = p_new_parent_id
) INTO parent_exists;

IF NOT parent_exists THEN
            RAISE EXCEPTION 'Parent node with id % not found', p_new_parent_id;
END IF;
END IF;

UPDATE class_node
SET "ParentId" = p_new_parent_id
WHERE "Id" = p_class_id;

RETURN TRUE;
END;
$$ LANGUAGE plpgsql;


CREATE OR REPLACE FUNCTION update_class_sort_order(
    p_class_id INTEGER,
    p_sort_order INTEGER
)
RETURNS BOOLEAN
AS $$
DECLARE
updated_count INTEGER;
BEGIN
UPDATE class_node
SET "SortOrder" = p_sort_order
WHERE "Id" = p_class_id;

GET DIAGNOSTICS updated_count = ROW_COUNT;

IF updated_count = 0 THEN
        RAISE EXCEPTION 'Class node with id % not found', p_class_id;
END IF;

RETURN TRUE;
END;
$$ LANGUAGE plpgsql;


CREATE OR REPLACE FUNCTION prevent_class_cycle()
RETURNS TRIGGER
AS $$
DECLARE
current_parent_id INTEGER;
BEGIN
    IF NEW."ParentId" IS NULL THEN
        RETURN NEW;
END IF;

    IF NEW."ParentId" = NEW."Id" THEN
        RAISE EXCEPTION 'Cycle detected: node cannot reference itself';
END IF;

    current_parent_id := NEW."ParentId";

    WHILE current_parent_id IS NOT NULL LOOP
        IF current_parent_id = NEW."Id" THEN
            RAISE EXCEPTION 'Cycle detected in class hierarchy';
END IF;

SELECT "ParentId"
INTO current_parent_id
FROM class_node
WHERE "Id" = current_parent_id;
END LOOP;

RETURN NEW;
END;
$$ LANGUAGE plpgsql;


DROP TRIGGER IF EXISTS check_class_cycle_trigger ON class_node;

CREATE TRIGGER check_class_cycle_trigger
    BEFORE INSERT OR UPDATE OF "ParentId"
                     ON class_node
                         FOR EACH ROW
                         EXECUTE FUNCTION prevent_class_cycle();

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
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DROP TRIGGER IF EXISTS check_class_cycle_trigger ON class_node;
DROP FUNCTION IF EXISTS prevent_class_cycle();
DROP FUNCTION IF EXISTS find_children(integer), find_parents(integer), find_leaves_by_class(integer), move_class_node(integer, integer), update_class_sort_order(integer, integer);
DROP FUNCTION IF EXISTS get_enum_values(integer), add_enum_value(integer, varchar, numeric, varchar, varchar, integer), is_enum_value_allowed(integer, varchar, numeric, varchar), update_enum_value_sort_order(integer, integer), get_enum_classes();
""");
    }
}
