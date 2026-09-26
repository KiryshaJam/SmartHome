-- All writes share one transaction lock: concurrent tree/schema/value changes
-- observe a consistent catalog. Reads remain concurrent.
CREATE OR REPLACE FUNCTION lock_catalog_write() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    PERFORM pg_advisory_xact_lock(734021, 1);
    RETURN NULL;
END $$;
DO $$ DECLARE t text; BEGIN
    FOREACH t IN ARRAY ARRAY['class_node','class_parameter','product','product_parameter_value','parameter_definition','enum_class','enum_value'] LOOP
        EXECUTE format('CREATE TRIGGER catalog_write_lock BEFORE INSERT OR UPDATE OR DELETE ON %I FOR EACH STATEMENT EXECUTE FUNCTION lock_catalog_write()', t);
    END LOOP;
END $$;

CREATE OR REPLACE FUNCTION inherit_class_parameters(p_class_id integer) RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
    INSERT INTO class_parameter ("ClassNodeId", "ParameterDefinitionId", "ParameterGroupId", "SortOrder", "IsRequired", "IsInherited", "MinNumberValue", "MaxNumberValue")
    SELECT c."Id", p."ParameterDefinitionId", p."ParameterGroupId", p."SortOrder", p."IsRequired", true, p."MinNumberValue", p."MaxNumberValue"
    FROM class_node c JOIN class_parameter p ON p."ClassNodeId" = c."ParentId"
    WHERE c."Id" = p_class_id
    ON CONFLICT ("ClassNodeId", "ParameterDefinitionId") DO NOTHING;
    GET DIAGNOSTICS affected = ROW_COUNT;
    RETURN affected;
END $$;

CREATE OR REPLACE FUNCTION inherit_new_class() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    PERFORM inherit_class_parameters(NEW."Id");
    RETURN NEW;
END $$;
CREATE TRIGGER inherit_new_class AFTER INSERT OR UPDATE OF "ParentId" ON class_node
FOR EACH ROW EXECUTE FUNCTION inherit_new_class();

CREATE OR REPLACE FUNCTION spread_new_parameter() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO class_parameter ("ClassNodeId", "ParameterDefinitionId", "ParameterGroupId", "SortOrder", "IsRequired", "IsInherited", "MinNumberValue", "MaxNumberValue")
    SELECT c."Id", NEW."ParameterDefinitionId", NEW."ParameterGroupId", NEW."SortOrder", NEW."IsRequired", true, NEW."MinNumberValue", NEW."MaxNumberValue"
    FROM class_node c WHERE c."ParentId" = NEW."ClassNodeId"
    ON CONFLICT ("ClassNodeId", "ParameterDefinitionId") DO NOTHING;
    RETURN NEW;
END $$;
CREATE TRIGGER spread_new_parameter AFTER INSERT ON class_parameter
FOR EACH ROW EXECUTE FUNCTION spread_new_parameter();
