-- Disposable database only. Run with -v before=1 on AddCatalogOperations,
-- apply the latest migration, then run with -v before=0 on the same database.
\set ON_ERROR_STOP on
\if :before
BEGIN;
DO $$
DECLARE leaf integer; definition integer; binding integer; item integer;
BEGIN
 leaf:=add_class_node('Migration preservation','migration-preservation',NULL,true);
 definition:=describe_parameter('Migration value','migration-value',2);
 binding:=bind_class_parameter(leaf,definition,NULL,1,true,1,10);
 item:=add_product('Filled draft','migration-filled-draft',leaf);
 PERFORM write_product_parameter(item,binding,NULL,3);
 item:=add_product('Completed product','migration-completed',leaf);
 PERFORM write_product_parameter(item,binding,NULL,4);
 PERFORM complete_product(item);
 PERFORM add_product('Unfilled product','migration-unfilled',leaf);
END $$;
CREATE TABLE test_draft_removal_snapshot(table_name text PRIMARY KEY, rows jsonb NOT NULL);
DO $$
DECLARE t text; data jsonb;
BEGIN
 FOREACH t IN ARRAY ARRAY['class_node','product','measure_unit','enum_class','enum_value','parameter_group','parameter_definition','class_parameter','product_parameter_value'] LOOP
   EXECUTE format('SELECT coalesce(jsonb_agg(to_jsonb(t)-''IsDraft'' ORDER BY "Id"),''[]''::jsonb) FROM %I t',t) INTO data;
   INSERT INTO test_draft_removal_snapshot VALUES(t,data);
 END LOOP;
END $$;
COMMIT;
\else
BEGIN;
DO $$
DECLARE snapshot record; data jsonb;
BEGIN
 IF EXISTS(SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='product' AND column_name='IsDraft') OR to_regprocedure('complete_product(integer)') IS NOT NULL THEN
   RAISE EXCEPTION 'Product lifecycle was not removed';
 END IF;
 FOR snapshot IN SELECT * FROM test_draft_removal_snapshot LOOP
   EXECUTE format('SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY "Id"),''[]''::jsonb) FROM %I t',snapshot.table_name) INTO data;
   IF data IS DISTINCT FROM snapshot.rows THEN RAISE EXCEPTION 'Migration changed business data in %',snapshot.table_name; END IF;
 END LOOP;
END $$;
DROP TABLE test_draft_removal_snapshot;
COMMIT;
\endif
