\set ON_ERROR_STOP on
-- Disposable database only. Upgrade fixture must have been committed previously.
BEGIN;
DO $$
DECLARE r int; variant int; order_id int; new_revision int; original_count int;
BEGIN
 IF EXISTS(SELECT 1 FROM information_schema.columns WHERE table_name='bom_order_revision' AND column_name='Lines') THEN RAISE EXCEPTION 'JSON source still exists'; END IF;
 IF EXISTS(SELECT 1 FROM test_order_upgrade_expected e LEFT JOIN bom_order_revision historical_row ON historical_row."Id"=e.revision_id WHERE historical_row."Id" IS NULL OR e.metadata IS DISTINCT FROM to_jsonb(historical_row)-'CreationTransaction' OR e.lines IS DISTINCT FROM (SELECT jsonb_agg(jsonb_build_object('variantId',l."VariantId",'quantity',l."Quantity") ORDER BY l."SortOrder") FROM bom_order_line l WHERE l."OrderRevisionId"=historical_row."Id") OR e.totals IS DISTINCT FROM (SELECT jsonb_agg(t) FROM calc_order_bom(historical_row."Id") t)) THEN RAISE EXCEPTION 'Upgrade changed history, ordering, precision or totals'; END IF;
 SELECT max(revision_id) INTO r FROM test_order_upgrade_expected;
 SELECT "OrderId" INTO order_id FROM bom_order_revision WHERE "Id"=r;
 SELECT "VariantId" INTO variant FROM bom_order_line WHERE "OrderRevisionId"=r LIMIT 1;
 BEGIN
 INSERT INTO bom_order_line VALUES(r,1,variant,1);
 RAISE EXCEPTION 'Test failed: append to committed revision accepted';
 EXCEPTION WHEN raise_exception THEN IF SQLERRM LIKE 'Test failed:%' THEN RAISE; END IF; END;
 BEGIN
 UPDATE bom_order_line SET "Quantity"=2 WHERE "OrderRevisionId"=r;
 RAISE EXCEPTION 'Test failed: history update accepted';
 EXCEPTION WHEN raise_exception THEN IF SQLERRM LIKE 'Test failed:%' THEN RAISE; END IF; END;
 BEGIN
 DELETE FROM bom_order_line WHERE "OrderRevisionId"=r;
 RAISE EXCEPTION 'Test failed: history delete accepted';
 EXCEPTION WHEN raise_exception THEN IF SQLERRM LIKE 'Test failed:%' THEN RAISE; END IF; END;
 BEGIN
 TRUNCATE bom_order_line;
 RAISE EXCEPTION 'Test failed: history truncate accepted';
 EXCEPTION WHEN raise_exception THEN IF SQLERRM LIKE 'Test failed:%' THEN RAISE; END IF; END;
 SELECT count(*) INTO original_count FROM bom_order_revision;
 BEGIN
 INSERT INTO bom_order_revision("OrderId","PreviousId","Reason","CreatedAt") VALUES(order_id,r,'Empty version',now());
 SET CONSTRAINTS ALL IMMEDIATE;
 RAISE EXCEPTION 'Test failed: empty revision accepted';
 EXCEPTION WHEN raise_exception THEN IF SQLERRM LIKE 'Test failed:%' THEN RAISE; END IF; END;
 IF (SELECT count(*) FROM bom_order_revision)<>original_count THEN RAISE EXCEPTION 'Partial empty revision survived'; END IF;
 INSERT INTO bom_order_revision("OrderId","PreviousId","Reason","CreatedAt","CreationTransaction") VALUES(order_id,r,'New atomic revision',now(),'1'::xid8) RETURNING "Id" INTO new_revision;
 IF (SELECT "CreationTransaction" FROM bom_order_revision WHERE "Id"=new_revision)<>pg_current_xact_id() THEN RAISE EXCEPTION 'Client forged transaction marker'; END IF;
 BEGIN
 INSERT INTO bom_order_line VALUES(new_revision,0,-2147483648,1);
 RAISE EXCEPTION 'Test failed: unknown variant accepted';
 EXCEPTION WHEN foreign_key_violation THEN NULL; END;
 BEGIN
 INSERT INTO bom_order_line VALUES(new_revision,0,variant,0);
 RAISE EXCEPTION 'Test failed: zero quantity accepted';
 EXCEPTION WHEN check_violation THEN NULL; END;
 BEGIN
 INSERT INTO bom_order_line VALUES(new_revision,0,variant,1000000001);
 RAISE EXCEPTION 'Test failed: excessive quantity accepted';
 EXCEPTION WHEN check_violation THEN NULL; END;
 BEGIN
 INSERT INTO bom_order_line VALUES(new_revision,1,variant,1);
 SET CONSTRAINTS ALL IMMEDIATE;
 RAISE EXCEPTION 'Test failed: missing first position accepted';
 EXCEPTION WHEN raise_exception THEN IF SQLERRM LIKE 'Test failed:%' THEN RAISE; END IF; END;
 INSERT INTO bom_order_line VALUES(new_revision,0,variant,1.5),(new_revision,1,variant,2.5);
 SET CONSTRAINTS ALL IMMEDIATE;
 IF (SELECT sum(required_quantity) FROM calc_order_bom(new_revision))<>4 THEN RAISE EXCEPTION 'Duplicate variant lines aggregated incorrectly'; END IF;
 BEGIN
 INSERT INTO bom_order_revision("OrderId","PreviousId","Reason","CreatedAt") VALUES(order_id,r,'Stale predecessor',now());
 RAISE EXCEPTION 'Test failed: stale predecessor accepted';
 EXCEPTION WHEN serialization_failure THEN NULL; END;
 BEGIN
 INSERT INTO bom_order_revision("Id","OrderId","PreviousId","Reason","CreatedAt") VALUES(-2147483648,order_id,new_revision,'Lower ID',now());
 RAISE EXCEPTION 'Test failed: hidden order branch accepted';
 EXCEPTION WHEN serialization_failure THEN NULL; END;
 BEGIN
 IF EXISTS(SELECT 1 FROM information_schema.columns WHERE table_name='bom_revision' AND column_name='Document') THEN
  EXECUTE 'INSERT INTO bom_revision("Id","ProductId","UnitId","PreviousId","BaseRevisionId","Reason","CreatedAt","Document") SELECT -2147483648,"ProductId","UnitId","Id","BaseRevisionId",''Lower ID'',now(),"Document" FROM bom_revision ORDER BY "Id" DESC LIMIT 1';
 ELSE
  INSERT INTO bom_revision("Id","ProductId","UnitId","PreviousId","BaseRevisionId","Reason","CreatedAt","DocumentId") SELECT -2147483648,"ProductId","UnitId","Id","BaseRevisionId",'Lower ID',now(),"DocumentId" FROM bom_revision ORDER BY "Id" DESC LIMIT 1;
 END IF;
 RAISE EXCEPTION 'Test failed: hidden BOM branch accepted';
 EXCEPTION WHEN serialization_failure THEN NULL; END;
END $$;
-- A child subtransaction must still recognize the top-level creation marker.
SET CONSTRAINTS ALL DEFERRED;
INSERT INTO bom_order("Number","Date") VALUES ('savepoint-order',current_date);
INSERT INTO bom_order_revision("OrderId","Reason","CreatedAt") SELECT "Id",'Before savepoint',now() FROM bom_order WHERE "Number"='savepoint-order';
SAVEPOINT ef_save_changes;
INSERT INTO bom_order_line SELECT r."Id",0,l."VariantId",1 FROM bom_order_revision r CROSS JOIN LATERAL (SELECT "VariantId" FROM bom_order_line LIMIT 1) l WHERE r."OrderId"=(SELECT "Id" FROM bom_order WHERE "Number"='savepoint-order');
RELEASE SAVEPOINT ef_save_changes;
SET CONSTRAINTS ALL IMMEDIATE;
ROLLBACK;
