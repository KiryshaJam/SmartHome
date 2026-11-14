-- Only a NEW disposable database. This test rolls back its own fixture.
BEGIN;
DO $$
DECLARE unit_id int; cls int; part int; parent int; revision int; next_revision int; v1 int; v2 int; mod_product int; mod_revision int; oid int; amount numeric;
BEGIN
 IF (SELECT count(*) FROM information_schema.tables WHERE table_schema='public' AND table_name LIKE 'bom\_%' ESCAPE '\')<>8 THEN RAISE EXCEPTION 'Expected 8 BOM tables'; END IF;
 IF EXISTS(SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name LIKE 'bom\_%' ESCAPE '\' AND data_type IN ('json','jsonb','ARRAY')) THEN RAISE EXCEPTION 'Serialized storage found'; END IF;
 INSERT INTO measure_unit("Name","ShortName") VALUES('simple test unit','simple-test') RETURNING "Id" INTO unit_id;
 INSERT INTO class_node("Name","ShortName","IsTerminal","SortOrder") VALUES('simple test class','simple-test',true,0) RETURNING "Id" INTO cls;
 INSERT INTO product("Name","ShortName","ClassNodeId") VALUES('simple part','simple-part',cls) RETURNING "Id" INTO part;
 INSERT INTO product("Name","ShortName","ClassNodeId") VALUES('simple assembly','simple-assembly',cls) RETURNING "Id" INTO parent;
 revision:=register_change(part,unit_id,NULL,NULL,'{"positions":[],"parameters":[],"rules":[],"calculations":{}}','part');
 next_revision:=register_change(parent,unit_id,NULL,NULL,jsonb_build_object('positions',jsonb_build_array(jsonb_build_object('id','part','componentId',part,'revisionId',revision,'unitId',unit_id,'amount',3,'forQuantity',2))),'assembly');
 v1:=bom_create_variant(next_revision,'first',jsonb_build_object('productId',parent,'revisionId',next_revision,'unitId',unit_id,'quantity',1,'amount',1,'forQuantity',1,'values','{}'::jsonb,'children',jsonb_build_array(jsonb_build_object('productId',part,'revisionId',revision,'unitId',unit_id,'positionId','part','quantity',1.5,'amount',3,'forQuantity',2,'values','{}'::jsonb,'children','[]'::jsonb))));
 SELECT required_quantity INTO amount FROM calc_bom(v1,2,NULL) WHERE product_id=part;
 IF amount<>3 THEN RAISE EXCEPTION 'Fractional norm calculation failed'; END IF;
 next_revision:=register_change(parent,unit_id,next_revision,NULL,jsonb_build_object('positions',jsonb_build_array(jsonb_build_object('id','part','componentId',part,'revisionId',revision,'unitId',unit_id,'amount',5,'forQuantity',1))),'new template');
 SELECT required_quantity INTO amount FROM calc_bom(v1,2,NULL) WHERE product_id=part;
 IF amount<>3 THEN RAISE EXCEPTION 'Old configured norm changed'; END IF;
 mod_product:=create_modification(next_revision,'simple modification','simple-mod','modification');
 SELECT "Id" INTO mod_revision FROM bom_revision WHERE "ProductId"=mod_product AND NOT "IsConfigured";
 IF (SELECT count(*) FROM bom_position WHERE "RevisionId"=mod_revision)<>0 OR jsonb_array_length(bom_effective_positions(mod_revision))<>1 THEN RAISE EXCEPTION 'Base positions copied/lost'; END IF;
 mod_revision:=register_change(mod_product,unit_id,mod_revision,next_revision,'{"positions":[{"id":"part","excluded":true}]}','exclude inherited');
 IF jsonb_array_length(bom_effective_positions(mod_revision))<>0 THEN RAISE EXCEPTION 'Inherited exclusion failed'; END IF;
 INSERT INTO bom_order("Number","Date","UpdateToken") VALUES('simple-test',current_date,1) RETURNING "Id" INTO oid;
 INSERT INTO bom_order_line VALUES(oid,0,v1,1.25),(oid,1,v1,2.5);
 SELECT required_quantity INTO amount FROM calc_order_bom(oid,NULL) WHERE product_id=part;
 IF amount<>5.625 THEN RAISE EXCEPTION 'Duplicate order rows lost'; END IF;
 BEGIN
  UPDATE bom_position SET "Amount"=999 WHERE "RevisionId"=v1;
  RAISE EXCEPTION 'TEST FAILED: immutable position accepted';
 EXCEPTION WHEN raise_exception THEN IF SQLERRM<>'Сохранённая спецификация неизменна' THEN RAISE; END IF; END;
 BEGIN
  INSERT INTO bom_order_line VALUES(oid,2,next_revision,1);
  RAISE EXCEPTION 'TEST FAILED: order accepts template';
 EXCEPTION WHEN raise_exception THEN IF SQLERRM<>'Заказ ссылается только на готовое исполнение' THEN RAISE; END IF; END;
 -- Unknown actual keys must be rejected by the importer itself, not alias resolution.
 BEGIN
  PERFORM bom_import_parameters(revision,'{"parameters":[]}'::jsonb,'{"p2147483647":1}'::jsonb);
  RAISE EXCEPTION 'TEST FAILED: importer accepts an unknown parameter';
 EXCEPTION WHEN raise_exception THEN IF SQLERRM<>'Неизвестное фактическое значение параметра' THEN RAISE; END IF; END;
 IF bom_date_ticks(bom_date_json(638661234567890127)#>>'{}')<>638661234567890127 THEN RAISE EXCEPTION 'Date precision lost'; END IF;
END $$;
ROLLBACK;
