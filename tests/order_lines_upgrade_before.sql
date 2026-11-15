\set ON_ERROR_STOP on
-- Run ONLY on an empty disposable database migrated to AddBom.
CREATE TABLE test_order_upgrade_expected (revision_id int PRIMARY KEY, metadata jsonb, lines jsonb, totals jsonb);
DO $$
DECLARE p int; r int; v int; o int; first_revision int; second_revision int; cls int; unit int;
BEGIN
 SELECT "Id" INTO cls FROM class_node WHERE "IsTerminal" ORDER BY "Id" LIMIT 1;
 SELECT "Id" INTO unit FROM measure_unit ORDER BY "Id" LIMIT 1;
 INSERT INTO product("Name","ShortName","ClassNodeId") VALUES ('upgrade-order-leaf','upgrade-order-leaf',cls) RETURNING "Id" INTO p;
 r:=register_change(p,unit,NULL,NULL,jsonb_build_object('positions','[]'::jsonb,'parameters','[]'::jsonb,'rules','[]'::jsonb,'calculations','{}'::jsonb),'Upgrade leaf');
 INSERT INTO bom_variant("RevisionId","Name","CreatedAt","Snapshot") VALUES (r,'Upgrade frozen leaf','2026-11-01T12:34:56Z',jsonb_build_object('productId',p,'revisionId',r,'name','Frozen name','unitId',unit,'unit','unit','classes',jsonb_build_array(cls),'quantity',1,'children','[]'::jsonb)) RETURNING "Id" INTO v;
 INSERT INTO bom_order("Number","Date") VALUES ('upgrade-history','2026-10-29') RETURNING "Id" INTO o;
 INSERT INTO bom_order_revision("OrderId","PreviousId","Reason","CreatedAt","Lines") VALUES (o,NULL,'Original','2026-11-01T12:34:56Z',jsonb_build_array(jsonb_build_object('variantId',v,'quantity',1.234567890123456789),jsonb_build_object('variantId',v,'quantity',2.5))) RETURNING "Id" INTO first_revision;
 INSERT INTO bom_order_revision("OrderId","PreviousId","Reason","CreatedAt","Lines") VALUES (o,first_revision,'Changed quantity','2026-11-02T01:02:03Z',jsonb_build_array(jsonb_build_object('variantId',v,'quantity',7))) RETURNING "Id" INTO second_revision;
 INSERT INTO test_order_upgrade_expected SELECT x."Id",to_jsonb(x)-'Lines',x."Lines",(SELECT jsonb_agg(t) FROM calc_order_bom(x."Id") t) FROM bom_order_revision x;
END $$;
