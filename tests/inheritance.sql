\set ON_ERROR_STOP on
BEGIN;
-- A failure at the lowest descendant must roll back the whole propagation.
CREATE FUNCTION pg_temp.reject_test_descendant() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF EXISTS(SELECT 1 FROM class_node WHERE "Id"=NEW."ClassNodeId" AND "ShortName"='test-leaf')
 AND EXISTS(SELECT 1 FROM parameter_definition WHERE "Id"=NEW."ParameterDefinitionId" AND "ShortName"='test-atomic') THEN
   RAISE EXCEPTION 'Simulated descendant failure';
 END IF;
 RETURN NEW;
END $$;
CREATE TRIGGER test_reject_descendant BEFORE INSERT ON class_parameter FOR EACH ROW EXECUTE FUNCTION pg_temp.reject_test_descendant();
DO $$
DECLARE root integer; child integer; leaf integer; definition integer; cp integer;
BEGIN
 INSERT INTO class_node ("Name","ShortName","IsTerminal","SortOrder") VALUES ('Test inheritance','test-inherit',false,0) RETURNING "Id" INTO root;
 INSERT INTO parameter_definition ("Name","ShortName","ValueType") VALUES ('Test inherited parameter','test-inherit-param',2) RETURNING "Id" INTO definition;
 INSERT INTO class_parameter ("ClassNodeId","ParameterDefinitionId","SortOrder","IsRequired","IsInherited","MinNumberValue","MaxNumberValue") VALUES (root,definition,7,true,false,1,10) RETURNING "Id" INTO cp;
 INSERT INTO class_node ("Name","ShortName","IsTerminal","SortOrder","ParentId") VALUES ('Test child','test-child',false,0,root) RETURNING "Id" INTO child;
 INSERT INTO class_node ("Name","ShortName","IsTerminal","SortOrder","ParentId") VALUES ('Test leaf','test-leaf',true,0,child) RETURNING "Id" INTO leaf;
 IF (SELECT count(*) FROM class_parameter WHERE "ParameterDefinitionId"=definition AND "SortOrder"=7 AND "IsRequired" AND "MinNumberValue"=1 AND "MaxNumberValue"=10) <> 3 THEN RAISE EXCEPTION 'Inheritance lost settings'; END IF;
 PERFORM inherit_class_parameters(child);
 IF (SELECT count(*) FROM class_parameter WHERE "ParameterDefinitionId"=definition) <> 3 THEN RAISE EXCEPTION 'Duplicate inheritance'; END IF;
 INSERT INTO parameter_definition ("Name","ShortName","ValueType") VALUES ('Late parameter','test-late-param',3) RETURNING "Id" INTO definition;
 INSERT INTO class_parameter ("ClassNodeId","ParameterDefinitionId","SortOrder","IsRequired","IsInherited") VALUES (root,definition,8,false,false);
 IF (SELECT count(*) FROM class_parameter WHERE "ParameterDefinitionId"=definition) <> 3 THEN RAISE EXCEPTION 'Late inheritance failed'; END IF;
 INSERT INTO parameter_definition ("Name","ShortName","ValueType") VALUES ('Atomic parameter','test-atomic',3) RETURNING "Id" INTO definition;
 BEGIN
   INSERT INTO class_parameter ("ClassNodeId","ParameterDefinitionId","SortOrder","IsRequired","IsInherited") VALUES (root,definition,9,false,false);
   RAISE EXCEPTION 'Descendant failure was ignored' USING ERRCODE='XX000';
 EXCEPTION WHEN raise_exception THEN
   NULL;
 END;
 IF EXISTS(SELECT 1 FROM class_parameter WHERE "ParameterDefinitionId"=definition) THEN RAISE EXCEPTION 'Inheritance partially persisted'; END IF;
END $$;
ROLLBACK;
