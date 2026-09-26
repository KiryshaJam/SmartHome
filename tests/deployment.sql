\set ON_ERROR_STOP on
BEGIN;
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'check_class_cycle_trigger') THEN
        RAISE EXCEPTION 'Cycle trigger is missing';
    END IF;
    PERFORM * FROM get_enum_classes();
    PERFORM * FROM get_enum_values(1);
    PERFORM * FROM find_children(1);
    BEGIN
        UPDATE class_node SET "ParentId" = "Id" WHERE "Id" = 1;
        RAISE EXCEPTION 'Cycle was accepted' USING ERRCODE = 'XX000';
    EXCEPTION WHEN raise_exception THEN
        NULL;
    END;
END $$;
ROLLBACK;
