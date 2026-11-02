\set ON_ERROR_STOP on
BEGIN;
DO $$
DECLARE pid int; cid int; uid int; a int; b int; doc jsonb; old_count int; v int;
BEGIN
 SELECT "Id" INTO cid FROM class_node WHERE "IsTerminal" ORDER BY "Id" LIMIT 1;
 SELECT "Id" INTO uid FROM measure_unit ORDER BY "Id" LIMIT 1;
 INSERT INTO product("Name","ShortName","ClassNodeId") VALUES('sql-bom-check-a','sql-bom-check-a',cid) RETURNING "Id" INTO pid;
 doc := jsonb_build_object('positions','[]'::jsonb,'parameters','[]'::jsonb,'rules','[]'::jsonb,'calculations','{}'::jsonb,'productName','SQL item','unitName','unit','classes',jsonb_build_array(cid));
 a:=register_change(pid,uid,NULL,NULL,doc,'Initial');
 SELECT count(*) INTO old_count FROM bom_revision;
 BEGIN
 PERFORM register_change(pid,uid,a,NULL,jsonb_set(doc,'{positions}',jsonb_build_array(jsonb_build_object('id','self','componentId',pid,'revisionId',a,'unitId',uid,'amount',1,'forQuantity',1,'sortOrder',0))),'Self cycle');
 RAISE EXCEPTION 'Test failed: cycle accepted';
 EXCEPTION WHEN raise_exception THEN IF SQLERRM='Test failed: cycle accepted' THEN RAISE; END IF; END;
 IF (SELECT count(*) FROM bom_revision)<>old_count THEN RAISE EXCEPTION 'Partial revision left behind'; END IF;
 BEGIN
 UPDATE bom_revision SET "Reason"='Rewrite history' WHERE "Id"=a;
 RAISE EXCEPTION 'Test failed: mutable history';
 EXCEPTION WHEN raise_exception THEN IF SQLERRM='Test failed: mutable history' THEN RAISE; END IF; END;
 BEGIN
 PERFORM register_change(pid,uid,NULL,NULL,doc,'Stale');
 RAISE EXCEPTION 'Test failed: stale write';
 EXCEPTION WHEN serialization_failure THEN NULL; END;
 b:=create_modification(a,'sql-bom-check-mod','sql-bom-check-mod','Derived');
 IF jsonb_array_length(bom_effective_positions((SELECT max("Id") FROM bom_revision WHERE "ProductId"=b)))<>0 THEN RAISE EXCEPTION 'Empty modification not empty'; END IF;
 -- A failing trigger after the insert must roll back product + revision as one SQL operation.
 BEGIN
 PERFORM create_modification(a,'sql-bom-check-bad','sql-bom-check-bad','');
 RAISE EXCEPTION 'Test failed: empty reason accepted';
 EXCEPTION WHEN raise_exception THEN IF SQLERRM='Test failed: empty reason accepted' THEN RAISE; END IF; END;
 IF EXISTS(SELECT 1 FROM product WHERE "Name"='sql-bom-check-bad') THEN RAISE EXCEPTION 'Partial modification left behind'; END IF;
END $$;
DO $$
DECLARE cls int; unit int; other_unit int; root_id int; comp_id int; leaf_rev int; base_rev int; mod_id int; mod_rev int; d jsonb; p jsonb;
BEGIN
 SELECT "Id" INTO cls FROM class_node WHERE "IsTerminal" ORDER BY "Id" LIMIT 1;
 SELECT "Id" INTO unit FROM measure_unit ORDER BY "Id" LIMIT 1;
 SELECT "Id" INTO other_unit FROM measure_unit WHERE "Id"<>unit ORDER BY "Id" LIMIT 1;
 INSERT INTO product("Name","ShortName","ClassNodeId") VALUES('sql-bom-root','sql-bom-root',cls) RETURNING "Id" INTO root_id;
 INSERT INTO product("Name","ShortName","ClassNodeId") VALUES('sql-bom-leaf','sql-bom-leaf',cls) RETURNING "Id" INTO comp_id;
 d:=jsonb_build_object('positions','[]'::jsonb,'parameters','[]'::jsonb,'rules','[]'::jsonb,'calculations','{}'::jsonb);
 leaf_rev:=register_change(comp_id,unit,NULL,NULL,d,'Leaf');
 p:=jsonb_build_object('id','one','componentId',comp_id,'revisionId',leaf_rev,'unitId',unit,'amount',4,'forQuantity',2,'sortOrder',0);
 base_rev:=register_change(root_id,unit,NULL,NULL,jsonb_set(d,'{positions}',jsonb_build_array(p,jsonb_set(p,'{id}','"two"'))),'Two occurrences');
 mod_id:=create_modification(base_rev,'sql-bom-mod','sql-bom-mod','Base');
 SELECT max("Id") INTO mod_rev FROM bom_revision WHERE "ProductId"=mod_id;
 mod_rev:=register_change(mod_id,unit,mod_rev,base_rev,jsonb_set(d,'{positions}',jsonb_build_array(jsonb_set(p,'{amount}','6'),jsonb_build_object('id','two','excluded',true),jsonb_set(p,'{id}','"three"'))),'Replace exclude add');
 IF jsonb_array_length(bom_effective_positions(mod_rev))<>2 OR NOT EXISTS(SELECT 1 FROM jsonb_array_elements(bom_effective_positions(mod_rev)) x WHERE x->>'id'='one' AND x->>'amount'='6') THEN RAISE EXCEPTION 'Incorrect effective differences'; END IF;
 mod_rev:=bom_remove_position(mod_id,mod_rev,'one','Exclude inherited one');
 IF jsonb_array_length(bom_effective_positions(mod_rev))<>1 THEN RAISE EXCEPTION 'SQL exclude failed'; END IF;
 mod_rev:=bom_put_position(mod_id,mod_rev,jsonb_set(p,'{amount}','6'),'Restore replacement');
 mod_rev:=bom_remove_position(mod_id,mod_rev,'three','Remove added position');
 IF jsonb_array_length(bom_effective_positions(mod_rev))<>1 THEN RAISE EXCEPTION 'SQL remove failed'; END IF;
 IF jsonb_array_length(bom_effective_positions(base_rev))<>2 OR EXISTS(SELECT 1 FROM jsonb_array_elements(bom_effective_positions(base_rev)) x WHERE x->>'amount'<>'4') THEN RAISE EXCEPTION 'Base changed'; END IF;
 BEGIN
 PERFORM register_change(root_id,unit,base_rev,NULL,jsonb_set(d,'{positions}',jsonb_build_array(jsonb_set(p,'{forQuantity}','0'))),'Zero denominator');
 RAISE EXCEPTION 'Test failed: zero denominator';
 EXCEPTION WHEN raise_exception THEN IF SQLERRM='Test failed: zero denominator' THEN RAISE; END IF; END;
 BEGIN
 PERFORM register_change(root_id,unit,base_rev,NULL,jsonb_set(d,'{positions}',jsonb_build_array(jsonb_set(p,'{unitId}',to_jsonb(other_unit)))),'Wrong unit');
 RAISE EXCEPTION 'Test failed: wrong unit';
 EXCEPTION WHEN raise_exception THEN IF SQLERRM='Test failed: wrong unit' THEN RAISE; END IF; END;
 BEGIN
 DELETE FROM product WHERE "Id"=comp_id;
 RAISE EXCEPTION 'Test failed: used component deleted';
 EXCEPTION WHEN foreign_key_violation THEN NULL; END;
END $$;
ROLLBACK;
