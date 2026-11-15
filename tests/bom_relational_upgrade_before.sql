\set ON_ERROR_STOP on
-- ONLY an empty disposable database at NormalizeBomOrderLines, BEFORE BOM normalization.
-- This is synthetic history, deliberately including a pre-Source legacy leaf snapshot.
BEGIN;
CREATE TABLE test_bom_expected_ids (key text PRIMARY KEY, id integer NOT NULL);
CREATE TABLE test_bom_expected_revision (revision_id integer PRIMARY KEY, metadata jsonb NOT NULL, document jsonb NOT NULL, effective_positions jsonb NOT NULL);
CREATE TABLE test_bom_expected_variant (variant_id integer PRIMARY KEY, metadata jsonb NOT NULL, snapshot jsonb NOT NULL);
CREATE TABLE test_bom_expected_find (variant_id integer NOT NULL, quantity numeric NOT NULL, rows jsonb NOT NULL, PRIMARY KEY(variant_id,quantity));
CREATE TABLE test_bom_expected_calc (variant_id integer NOT NULL, quantity numeric NOT NULL, class_id integer, rows jsonb NOT NULL);
CREATE TABLE test_bom_expected_order_calc (order_revision_id integer NOT NULL, class_id integer, rows jsonb NOT NULL);
CREATE TABLE test_bom_expected_order (order_id integer PRIMARY KEY, metadata jsonb NOT NULL);
CREATE TABLE test_bom_expected_order_revision (revision_id integer PRIMARY KEY, metadata jsonb NOT NULL, lines jsonb NOT NULL, totals jsonb NOT NULL);
CREATE TABLE test_bom_expected_current_catalog (kind text NOT NULL, id integer NOT NULL, metadata jsonb NOT NULL, PRIMARY KEY(kind,id));
CREATE TABLE test_bom_expected_catalog (kind text NOT NULL, id integer NOT NULL, metadata jsonb NOT NULL, PRIMARY KEY(kind,id));
DO $$
DECLARE
 u integer; branch integer; cls integer; ec integer; red integer; blue integer;
 dint integer; dnum integer; dstr integer; ddate integer; denum integer; cp integer;
 leaf integer; sub integer; root integer; modification integer;
 lr integer; sr integer; r1 integer; r2 integer; mr integer;
 sv integer; rv1 integer; rv2 integer; mv integer; legacy integer;
 ord integer; or1 integer; or2 integer;
 params jsonb; vals jsonb; default_vals jsonb; base_doc jsonb; leaf_doc jsonb; sub_doc jsonb; root_doc jsonb; mod_doc jsonb;
 leaf_node jsonb; sub_node jsonb; root_node jsonb; bound_node jsonb; changed_node jsonb;
 part jsonb; subpart jsonb; pos jsonb; own_pos jsonb;
BEGIN
 IF EXISTS(SELECT 1 FROM bom_revision) OR EXISTS(SELECT 1 FROM bom_variant) OR EXISTS(SELECT 1 FROM bom_order)
 THEN RAISE EXCEPTION 'Fixture requires a disposable database with no BOM history.'; END IF;
 INSERT INTO measure_unit("Name","ShortName") VALUES('Upgrade fixture pieces','uf-pc') RETURNING "Id" INTO u;
 branch:=add_class_node('Upgrade fixture branch','uf-branch',NULL,false,9000,NULL);
 cls:=add_class_node('Upgrade fixture products','uf-products',branch,true,0,u);
 ec:=add_enum_class('Upgrade fixture colors','uf-colors',1,9000,NULL);
 INSERT INTO enum_value("EnumClassId","StringValue","DisplayName","SortOrder") VALUES(ec,'red','Historical ruby',0) RETURNING "Id" INTO red;
 INSERT INTO enum_value("EnumClassId","StringValue","DisplayName","SortOrder") VALUES(ec,'blue','Historical azure',1) RETURNING "Id" INTO blue;
 dint:=describe_parameter('Historical integer','uf-integer',1,u,NULL);
 dnum:=describe_parameter('Historical decimal','uf-decimal',2,u,NULL);
 dstr:=describe_parameter('Historical string','uf-string',3,NULL,NULL);
 ddate:=describe_parameter('Historical timestamp','uf-date',4,NULL,NULL);
 denum:=describe_parameter('Historical color','uf-color',5,NULL,ec);
 PERFORM bind_class_parameter(cls,dint,NULL,0,false,1,100);
 PERFORM bind_class_parameter(cls,dnum,NULL,1,false,0.01,100);
 PERFORM bind_class_parameter(cls,dstr,NULL,2,false,NULL,NULL);
 PERFORM bind_class_parameter(cls,ddate,NULL,3,false,NULL,NULL);
 PERFORM bind_class_parameter(cls,denum,NULL,4,false,NULL,NULL);
 leaf:=add_product('Historical leaf','uf-leaf',cls);
 sub:=add_product('Historical subassembly','uf-sub',cls);
 root:=add_product('Historical root','uf-root',cls);
 SELECT "Id" INTO cp FROM class_parameter WHERE "ClassNodeId"=cls AND "ParameterDefinitionId"=dint;
 PERFORM write_product_parameter(root,cp,2);
 SELECT "Id" INTO cp FROM class_parameter WHERE "ClassNodeId"=cls AND "ParameterDefinitionId"=dnum;
 PERFORM write_product_parameter(root,cp,NULL,1.234567890123456789);
 SELECT "Id" INTO cp FROM class_parameter WHERE "ClassNodeId"=cls AND "ParameterDefinitionId"=dstr;
 PERFORM write_product_parameter(root,cp,NULL,NULL,'Исходная строка "цитата" \ путь');
 SELECT "Id" INTO cp FROM class_parameter WHERE "ClassNodeId"=cls AND "ParameterDefinitionId"=ddate;
 PERFORM write_product_parameter(root,cp,NULL,NULL,NULL,'2026-10-01T05:33:04.1234567+03:30'::timestamptz);
 SELECT "Id" INTO cp FROM class_parameter WHERE "ClassNodeId"=cls AND "ParameterDefinitionId"=denum;
 PERFORM write_product_parameter(root,cp,NULL,NULL,NULL,NULL,red);
 params:=jsonb_build_array(
   jsonb_build_object('key','p'||dint,'definitionId',dint,'name','Historical integer','type',1,'required',true,'min',1,'max',100,'enumIds','[]'::jsonb,'enumLabels','{}'::jsonb,'default',2),
   jsonb_build_object('key','p'||dnum,'definitionId',dnum,'name','Historical decimal','type',2,'required',false,'min',0.01,'max',100,'enumIds','[]'::jsonb,'enumLabels','{}'::jsonb,'default',1.234567890123456789),
   jsonb_build_object('key','p'||dstr,'definitionId',dstr,'name','Historical string','type',3,'required',true,'min',NULL,'max',NULL,'enumIds','[]'::jsonb,'enumLabels','{}'::jsonb,'default','Исходная строка "цитата" \ путь'),
   jsonb_build_object('key','p'||ddate,'definitionId',ddate,'name','Historical timestamp','type',4,'required',false,'min',NULL,'max',NULL,'enumIds','[]'::jsonb,'enumLabels','{}'::jsonb,'default','2026-10-01T05:33:04.1234567+03:30'),
   jsonb_build_object('key','p'||denum,'definitionId',denum,'name','Historical color','type',5,'required',true,'min',NULL,'max',NULL,'enumIds',jsonb_build_array(red,blue),'enumLabels',jsonb_build_object(red::text,'Historical ruby',blue::text,'Historical azure'),'default',red));
 vals:=jsonb_build_object('p'||dint,2,'p'||dnum,1.234567890123456789,'p'||dstr,'Значение "настроено" — 雪','p'||ddate,'2026-10-02T08:49:05.6543217+05:45','p'||denum,blue);
 SELECT jsonb_object_agg(x->>'key',x->'default') INTO default_vals FROM jsonb_array_elements(params) x;
 base_doc:=jsonb_build_object('positions','[]'::jsonb,'parameters',params,'rules','[]'::jsonb,'calculations','{}'::jsonb,'unitName','uf-pc','classes',jsonb_build_array(cls,branch));
 leaf_doc:=base_doc||jsonb_build_object('productName','Historical leaf');
 lr:=register_change(leaf,u,NULL,NULL,leaf_doc,'Initial typed leaf');
 leaf_node:=jsonb_build_object('productId',leaf,'revisionId',lr,'name','Historical leaf','unitId',u,'unit','uf-pc','classes',jsonb_build_array(cls,branch),'positionId','','quantity',1,'amount',1,'forQuantity',1,'values',default_vals,'source',leaf_doc,'children','[]'::jsonb);
 part:=jsonb_build_object('id','leaf_inside','componentId',leaf,'revisionId',lr,'amount',0.375,'forQuantity',2,'unitId',u,'sortOrder',0,'condition',NULL,'formula',NULL,'childVariantId',NULL,'bindings','{}'::jsonb,'excluded',false,'inherited',false);
 sub_doc:=base_doc||jsonb_build_object('productName','Historical subassembly','positions',jsonb_build_array(part));
 sr:=register_change(sub,u,NULL,NULL,sub_doc,'Subassembly with fractional leaf');
 sub_node:=jsonb_build_object('productId',sub,'revisionId',sr,'name','Historical subassembly','unitId',u,'unit','uf-pc','classes',jsonb_build_array(cls,branch),'positionId','','quantity',1,'amount',1,'forQuantity',1,'values',vals,'source',sub_doc,'children',jsonb_build_array(leaf_node||jsonb_build_object('positionId','leaf_inside','amount',0.375,'forQuantity',2,'quantity',0.1875)));
 INSERT INTO bom_variant("RevisionId","Name","CreatedAt","Snapshot") VALUES(sr,'Frozen nested subassembly','2026-10-03T01:02:03Z',sub_node) RETURNING "Id" INTO sv;
 subpart:=jsonb_build_object('id','nested_fixed','componentId',sub,'revisionId',sr,'amount',1.25,'forQuantity',2,'unitId',u,'sortOrder',0,'condition',NULL,'formula',NULL,'childVariantId',sv,'bindings','{}'::jsonb,'excluded',false,'inherited',false);
 pos:=part||jsonb_build_object('id','repeat_a','amount',0.125,'forQuantity',1,'sortOrder',2);
 root_doc:=base_doc||jsonb_build_object('productName','Historical root','positions',jsonb_build_array(subpart,pos,pos||jsonb_build_object('id','repeat_b','amount',2.5,'sortOrder',3)),'rules',jsonb_build_array(jsonb_build_object('expression','p'||dint||' >= 1','message','Historical integer must be positive')),'calculations',jsonb_build_object('p'||dnum,'p'||dint||' * 1.5'));
 r1:=register_change(root,u,NULL,NULL,root_doc,'First base specification');
 root_node:=jsonb_build_object('productId',root,'revisionId',r1,'name','Historical root','unitId',u,'unit','uf-pc','classes',jsonb_build_array(cls,branch),'positionId','','quantity',1,'amount',1,'forQuantity',1,'values',vals||jsonb_build_object('p'||dnum,3),'source',root_doc,'children',jsonb_build_array(sub_node||jsonb_build_object('positionId','nested_fixed','amount',1.25,'forQuantity',2,'quantity',0.625),leaf_node||jsonb_build_object('positionId','repeat_a','amount',0.125,'quantity',0.125),leaf_node||jsonb_build_object('positionId','repeat_b','amount',2.5,'quantity',2.5)));
 INSERT INTO bom_variant("RevisionId","Name","CreatedAt","Snapshot") VALUES(r1,'Frozen first base','2026-10-04T01:02:03Z',root_node) RETURNING "Id" INTO rv1;
 -- The second base adds a parameter binding alongside the fixed nested variant.
 own_pos:=subpart||jsonb_build_object('id','nested_bound','sortOrder',1,'childVariantId',NULL,'amount',3,'forQuantity',4,'condition','p'||dint||' > 1','formula','p'||dint||' * 1.5','bindings',jsonb_build_object('p'||dint,'p'||dint||' + 1'));
 root_doc:=root_doc||jsonb_build_object('positions',(root_doc->'positions')||jsonb_build_array(own_pos));
 r2:=register_change(root,u,r1,NULL,root_doc,'Second base adds bound child');
 bound_node:=sub_node||jsonb_build_object('positionId','nested_bound','amount',3,'forQuantity',4,'quantity',0.75,'values',default_vals||jsonb_build_object('p'||dint,3));
 root_node:=root_node||jsonb_build_object('revisionId',r2,'source',root_doc,'children',(root_node->'children')||jsonb_build_array(bound_node));
 INSERT INTO bom_variant("RevisionId","Name","CreatedAt","Snapshot") VALUES(r2,'Frozen second base','2026-10-05T01:02:03Z',root_node) RETURNING "Id" INTO rv2;
 modification:=create_modification(r2,'Historical modification','uf-mod','Derive second base');
 SELECT max("Id") INTO mr FROM bom_revision WHERE "ProductId"=modification;
 mr:=bom_put_position(modification,mr,pos||jsonb_build_object('amount',0.875),'Replace inherited repeated leaf');
 mr:=bom_remove_position(modification,mr,'repeat_b','Exclude inherited repeated leaf');
 mr:=bom_put_position(modification,mr,pos||jsonb_build_object('id','own_leaf','amount',1.234567890123456789,'forQuantity',3,'sortOrder',5),'Add own fractional leaf');
 -- Exercise removing an own row without manufacturing an inherited tombstone.
 mr:=bom_put_position(modification,mr,pos||jsonb_build_object('id','temporary_leaf','sortOrder',6),'Add temporary own row');
 mr:=bom_remove_position(modification,mr,'temporary_leaf','Remove temporary own row');
 SELECT "Document" INTO mod_doc FROM bom_revision WHERE "Id"=mr;
 -- Effective Source freezes inherited flags as BomService.Effective did, not the delta.
 SELECT jsonb_set(mod_doc,'{positions}',jsonb_agg(CASE WHEN x->>'id' IN ('nested_fixed','nested_bound') THEN x||'{"inherited":true}'::jsonb ELSE x END ORDER BY (x->>'sortOrder')::int,x->>'id'))
 INTO mod_doc FROM jsonb_array_elements(bom_effective_positions(mr)) x;
 changed_node:=root_node||jsonb_build_object('productId',modification,'revisionId',mr,'name','Historical modification','source',mod_doc,'children',jsonb_build_array(sub_node||jsonb_build_object('positionId','nested_fixed','amount',1.25,'forQuantity',2,'quantity',0.625),bound_node,leaf_node||jsonb_build_object('positionId','repeat_a','amount',0.875,'quantity',0.875),leaf_node||jsonb_build_object('positionId','own_leaf','amount',1.234567890123456789,'forQuantity',3,'quantity',1.234567890123456789/3)));
 INSERT INTO bom_variant("RevisionId","Name","CreatedAt","Snapshot") VALUES(mr,'Frozen effective modification','2026-10-06T01:02:03Z',changed_node) RETURNING "Id" INTO mv;
 -- Legacy leaf: no Source, Values, PositionId, Amount or ForQuantity. Root quantity
 -- is NOT the requested find_bom quantity; it still supplies norm fallback 2.75.
 INSERT INTO bom_variant("RevisionId","Name","CreatedAt","Snapshot") VALUES(lr,'Legacy minimal fractional leaf','2026-10-07T01:02:03Z',jsonb_build_object('productId',leaf,'revisionId',lr,'name','Legacy frozen leaf','unitId',u,'unit','Legacy frozen unit','classes',jsonb_build_array(cls,branch),'quantity',2.75,'children','[]'::jsonb)) RETURNING "Id" INTO legacy;
 INSERT INTO bom_order("Number","Date") VALUES('uf-history','2026-10-08') RETURNING "Id" INTO ord;
 INSERT INTO bom_order_revision("OrderId","PreviousId","Reason","CreatedAt") VALUES(ord,NULL,'Original frozen order','2026-10-08T01:02:03Z') RETURNING "Id" INTO or1;
 INSERT INTO bom_order_line("OrderRevisionId","SortOrder","VariantId","Quantity") VALUES(or1,0,rv1,1.234567890123456789),(or1,1,mv,2.5),(or1,2,rv1,0.125),(or1,3,legacy,3.25);
 INSERT INTO bom_order_revision("OrderId","PreviousId","Reason","CreatedAt") VALUES(ord,or1,'Changed order retains history','2026-10-09T01:02:03Z') RETURNING "Id" INTO or2;
 INSERT INTO bom_order_line("OrderRevisionId","SortOrder","VariantId","Quantity") VALUES(or2,0,rv2,7.125),(or2,1,sv,0.333333333333333333),(or2,2,legacy,1.5);
 INSERT INTO test_bom_expected_ids VALUES('unit',u),('branch',branch),('class',cls),('enum_class',ec),('red',red),('blue',blue),('integer_definition',dint),('number_definition',dnum),('string_definition',dstr),('date_definition',ddate),('enum_definition',denum),('leaf',leaf),('sub',sub),('root',root),('modification',modification),('base_v1',r1),('base_v2',r2),('modification_revision',mr),('nested_variant',sv),('base_variant_v1',rv1),('base_variant_v2',rv2),('modification_variant',mv),('legacy_variant',legacy),('order',ord),('order_revision_v1',or1),('order_revision_v2',or2);
END $$;

-- Capture every original history row and ordered observable result before upgrade.
INSERT INTO test_bom_expected_revision SELECT r."Id",to_jsonb(r)-'Document',r."Document",bom_effective_positions(r."Id") FROM bom_revision r;
INSERT INTO test_bom_expected_variant SELECT v."Id",to_jsonb(v)-'Snapshot',v."Snapshot" FROM bom_variant v;
INSERT INTO test_bom_expected_find
 SELECT v."Id",q.quantity,coalesce((SELECT jsonb_agg(to_jsonb(b) ORDER BY b.path) FROM find_bom(v."Id",q.quantity) b),'[]'::jsonb)
 FROM bom_variant v CROSS JOIN (VALUES(1::numeric),(2.375::numeric)) q(quantity);
INSERT INTO test_bom_expected_calc
 SELECT v."Id",q.quantity,c.class_id,coalesce((SELECT jsonb_agg(to_jsonb(b) ORDER BY b.product_id,b.name,b.unit_id,b.unit) FROM calc_bom(v."Id",q.quantity,c.class_id) b),'[]'::jsonb)
 FROM bom_variant v CROSS JOIN (VALUES(1::numeric),(2.375::numeric)) q(quantity)
 CROSS JOIN (SELECT NULL::integer class_id UNION ALL SELECT id FROM test_bom_expected_ids WHERE key IN ('branch','class') UNION ALL SELECT -1) c;
INSERT INTO test_bom_expected_order SELECT o."Id",to_jsonb(o) FROM bom_order o;
INSERT INTO test_bom_expected_order_revision
 SELECT r."Id",to_jsonb(r),
 (SELECT jsonb_agg(to_jsonb(l) ORDER BY l."SortOrder") FROM bom_order_line l WHERE l."OrderRevisionId"=r."Id"),
 coalesce((SELECT jsonb_agg(to_jsonb(b) ORDER BY b.product_id,b.name,b.unit_id,b.unit) FROM calc_order_bom(r."Id") b),'[]'::jsonb)
 FROM bom_order_revision r;
INSERT INTO test_bom_expected_order_calc
 SELECT h."Id",c.class_id,coalesce((SELECT jsonb_agg(to_jsonb(b) ORDER BY b.product_id,b.name,b.unit_id,b.unit) FROM calc_order_bom(h."Id",c.class_id) b),'[]'::jsonb)
 FROM bom_order_revision h CROSS JOIN (SELECT NULL::integer class_id UNION ALL SELECT id FROM test_bom_expected_ids WHERE key IN ('branch','class') UNION ALL SELECT -1) c;
INSERT INTO test_bom_expected_catalog
 SELECT 'product',p."Id",to_jsonb(p) FROM product p WHERE p."Id" IN (SELECT id FROM test_bom_expected_ids WHERE key IN ('leaf','sub','root','modification'))
 UNION ALL SELECT 'class',c."Id",to_jsonb(c) FROM class_node c WHERE c."Id" IN (SELECT id FROM test_bom_expected_ids WHERE key IN ('branch','class'))
 UNION ALL SELECT 'unit',u."Id",to_jsonb(u) FROM measure_unit u WHERE u."Id"=(SELECT id FROM test_bom_expected_ids WHERE key='unit')
 UNION ALL SELECT 'definition',d."Id",to_jsonb(d) FROM parameter_definition d WHERE d."Id" IN (SELECT id FROM test_bom_expected_ids WHERE key LIKE '%_definition')
 UNION ALL SELECT 'enum_value',e."Id",to_jsonb(e) FROM enum_value e WHERE e."Id" IN (SELECT id FROM test_bom_expected_ids WHERE key IN ('red','blue'));
-- Legal catalog edits AFTER freezing history; IDs, types and enum raw values stay valid.
UPDATE product SET "Name"='Current renamed '||"ShortName" WHERE "Id" IN (SELECT id FROM test_bom_expected_ids WHERE key IN ('leaf','sub','root','modification'));
UPDATE class_node SET "Name"='Current renamed class '||"Id" WHERE "Id" IN (SELECT id FROM test_bom_expected_ids WHERE key IN ('branch','class'));
UPDATE measure_unit SET "Name"='Current renamed unit',"ShortName"='current-pc' WHERE "Id"=(SELECT id FROM test_bom_expected_ids WHERE key='unit');
UPDATE parameter_definition SET "Name"='Current renamed parameter '||"Id" WHERE "Id" IN (SELECT id FROM test_bom_expected_ids WHERE key LIKE '%_definition');
UPDATE enum_value SET "DisplayName"='Current renamed color '||"Id" WHERE "Id" IN (SELECT id FROM test_bom_expected_ids WHERE key IN ('red','blue'));
INSERT INTO test_bom_expected_current_catalog
 SELECT 'product',p."Id",to_jsonb(p) FROM product p WHERE p."Id" IN (SELECT id FROM test_bom_expected_ids WHERE key IN ('leaf','sub','root','modification'))
 UNION ALL SELECT 'class',c."Id",to_jsonb(c) FROM class_node c WHERE c."Id" IN (SELECT id FROM test_bom_expected_ids WHERE key IN ('branch','class'))
 UNION ALL SELECT 'unit',u."Id",to_jsonb(u) FROM measure_unit u WHERE u."Id"=(SELECT id FROM test_bom_expected_ids WHERE key='unit')
 UNION ALL SELECT 'definition',d."Id",to_jsonb(d) FROM parameter_definition d WHERE d."Id" IN (SELECT id FROM test_bom_expected_ids WHERE key LIKE '%_definition')
 UNION ALL SELECT 'enum_value',e."Id",to_jsonb(e) FROM enum_value e WHERE e."Id" IN (SELECT id FROM test_bom_expected_ids WHERE key IN ('red','blue'));
DO $$
BEGIN
 IF (SELECT count(*) FROM test_bom_expected_revision)<>10
 OR (SELECT count(*) FROM test_bom_expected_variant)<>5
 OR (SELECT count(*) FROM test_bom_expected_find)<>10
 OR (SELECT count(*) FROM test_bom_expected_calc)<>40
 OR (SELECT count(*) FROM test_bom_expected_order_revision)<>2
 OR (SELECT count(*) FROM test_bom_expected_order_calc)<>8
 OR (SELECT count(*) FROM bom_order_line)<>7
 OR (SELECT count(*) FROM test_bom_expected_current_catalog)<>14
 THEN RAISE EXCEPTION 'Incomplete upgrade fixture coverage.'; END IF;
 IF jsonb_array_length(bom_effective_positions((SELECT id FROM test_bom_expected_ids WHERE key='base_v1')))<>3
 OR jsonb_array_length(bom_effective_positions((SELECT id FROM test_bom_expected_ids WHERE key='base_v2')))<>4
 OR jsonb_array_length(bom_effective_positions((SELECT id FROM test_bom_expected_ids WHERE key='modification_revision')))<>4
 THEN RAISE EXCEPTION 'Incorrect base/modification fixture composition.'; END IF;
 IF (SELECT norm FROM find_bom((SELECT id FROM test_bom_expected_ids WHERE key='legacy_variant'),1))<>2.75
 THEN RAISE EXCEPTION 'Legacy quantity fallback was not captured.'; END IF;
END $$;
SET CONSTRAINTS ALL IMMEDIATE;
COMMIT;
