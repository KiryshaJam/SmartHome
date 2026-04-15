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