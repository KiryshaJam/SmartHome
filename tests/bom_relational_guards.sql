\set ON_ERROR_STOP on
-- Run only after the committed relational-upgrade fixture, in a disposable DB.
-- Each rejected write is rolled back by a subtransaction; the outer test rolls back too.
BEGIN;
CREATE FUNCTION pg_temp.bom_expect_reject(statement text, expected_state text, expected_message text DEFAULT NULL)
RETURNS void LANGUAGE plpgsql AS $$
DECLARE rejected bool:=false;
BEGIN
 BEGIN
  EXECUTE statement;
  SET CONSTRAINTS ALL IMMEDIATE;
 EXCEPTION WHEN OTHERS THEN
  IF SQLSTATE<>expected_state OR (expected_message IS NOT NULL AND position(expected_message IN SQLERRM)=0)
  THEN RAISE EXCEPTION 'Unexpected rejection for %: [%] %',statement,SQLSTATE,SQLERRM; END IF;
  rejected:=true;
 END;
 IF NOT rejected THEN RAISE EXCEPTION 'Invalid write unexpectedly accepted: %',statement; END IF;
END $$;
CREATE FUNCTION pg_temp.bom_bad_position(patch jsonb) RETURNS void LANGUAGE plpgsql AS $$
DECLARE fresh int;
BEGIN
 INSERT INTO bom_document("PropertyMask") VALUES(1) RETURNING "Id" INTO fresh;
 INSERT INTO bom_position SELECT (jsonb_populate_record(NULL::bom_position,
  (SELECT to_jsonb(p) FROM bom_position p WHERE NOT p."Excluded" AND p."ChildVariantId" IS NULL LIMIT 1)
  ||patch||jsonb_build_object('DocumentId',fresh))).*;
END $$;
CREATE FUNCTION pg_temp.bom_bad_binding(patch jsonb) RETURNS void LANGUAGE plpgsql AS $$
DECLARE fresh int; other_doc int; binding bom_position_binding;
BEGIN
 SELECT * INTO STRICT binding FROM bom_position_binding LIMIT 1;
 IF patch->>'TargetDocumentId'='-1' THEN
  SELECT "DocumentId" INTO STRICT other_doc FROM bom_parameter WHERE "Key"=binding."ParameterKey" AND "DocumentId"<>binding."TargetDocumentId" LIMIT 1;
  patch:=jsonb_set(patch,'{TargetDocumentId}',to_jsonb(other_doc));
 END IF;
 INSERT INTO bom_document("PropertyMask") VALUES(1) RETURNING "Id" INTO fresh;
 INSERT INTO bom_position SELECT (jsonb_populate_record(NULL::bom_position,
  to_jsonb(p)||jsonb_build_object('DocumentId',fresh))).*
  FROM bom_position p WHERE p."DocumentId"=binding."DocumentId" AND p."PositionId"=binding."PositionId";
 INSERT INTO bom_position_binding SELECT (jsonb_populate_record(NULL::bom_position_binding,
  to_jsonb(binding)||patch||jsonb_build_object('DocumentId',fresh))).*;
END $$;
CREATE FUNCTION pg_temp.bom_cross_variant_parent() RETURNS void LANGUAGE plpgsql AS $$
DECLARE vid int; fresh int; node bom_variant_node;
BEGIN
 SELECT * INTO STRICT node FROM bom_variant_node WHERE "ParentNodeId" IS NOT NULL LIMIT 1;
 INSERT INTO bom_variant("RevisionId","Name","CreatedAt") VALUES(node."RevisionId",'Invalid cross-variant parent',now()) RETURNING "Id" INTO vid;
 fresh:=bom_import_document(bom_read_document(node."SourceDocumentId"));
 INSERT INTO bom_variant_node SELECT (jsonb_populate_record(NULL::bom_variant_node,
  to_jsonb(node)||jsonb_build_object('VariantId',vid,'SourceDocumentId',fresh))).*;
END $$;
CREATE FUNCTION pg_temp.bom_bad_parameter(patch jsonb) RETURNS void LANGUAGE plpgsql AS $$
DECLARE fresh int;
BEGIN
 INSERT INTO bom_document("PropertyMask") VALUES(2) RETURNING "Id" INTO fresh;
 INSERT INTO bom_parameter SELECT (jsonb_populate_record(NULL::bom_parameter,
  (SELECT to_jsonb(p) FROM bom_parameter p WHERE p."Type"=4 LIMIT 1)
  ||patch||jsonb_build_object('DocumentId',fresh))).*;
END $$;
CREATE FUNCTION pg_temp.bom_label_only_enum() RETURNS void LANGUAGE plpgsql AS $$
DECLARE snapshot jsonb; param jsonb; chosen int; vid int;
BEGIN
 SELECT bom_read_variant(v."Id"),p.value,(bom_read_variant(v."Id")->'values'->>(p.value->>'key'))::int
 INTO STRICT snapshot,param,chosen FROM bom_variant v
 CROSS JOIN LATERAL jsonb_array_elements(bom_read_variant(v."Id")->'source'->'parameters') p
 WHERE (p.value->>'type')::int=5 AND (bom_read_variant(v."Id")->'values'->(p.value->>'key')) IS DISTINCT FROM p.value->'default'
 LIMIT 1;
 snapshot:=jsonb_set(snapshot,'{source,parameters}',(SELECT jsonb_agg(CASE WHEN p.value->>'key'=param->>'key'
 THEN jsonb_set(p.value,'{enumIds}',(SELECT coalesce(jsonb_agg(e.value),'[]'::jsonb) FROM jsonb_array_elements(p.value->'enumIds') e WHERE e.value::text::int<>chosen))
 ELSE p.value END ORDER BY p.ordinality) FROM jsonb_array_elements(snapshot->'source'->'parameters') WITH ORDINALITY AS p(value, ordinality)));
 snapshot:=jsonb_set(snapshot,'{children}','[]'::jsonb);
 INSERT INTO bom_variant("RevisionId","Name","CreatedAt") VALUES((snapshot->>'revisionId')::int,'Label-only enum is not allowed',now()) RETURNING "Id" INTO vid;
 PERFORM bom_import_node(vid,NULL,0,snapshot);
END $$;
DO $$
DECLARE tbl text; col text; row_count bigint; did int; vid int;
BEGIN
 IF to_regclass('test_bom_expected_variant') IS NULL THEN RAISE EXCEPTION 'Committed upgrade fixture required'; END IF;
 -- Every normalized table has fixture rows. Verify actual row triggers, rather
 -- than allowing a vacuous UPDATE/DELETE to pass because no rows exist.
 FOREACH tbl IN ARRAY ARRAY['bom_document','bom_document_class','bom_position',
  'bom_position_binding','bom_parameter','bom_parameter_enum','bom_rule',
  'bom_calculation','bom_variant_node','bom_variant_node_class','bom_variant_node_value',
  'bom_revision','bom_variant'] LOOP
  EXECUTE format('SELECT count(*) FROM %I',tbl) INTO row_count;
  IF row_count=0 THEN RAISE EXCEPTION 'Missing guard fixture rows in %',tbl; END IF;
  SELECT column_name INTO STRICT col FROM information_schema.columns
   WHERE table_schema=current_schema() AND table_name=tbl ORDER BY ordinal_position LIMIT 1;
  PERFORM pg_temp.bom_expect_reject(format('UPDATE %I SET %I=%I',tbl,col,col),'P0001');
  PERFORM pg_temp.bom_expect_reject(format('DELETE FROM %I',tbl),'P0001');
  -- CASCADE prevents the FK check from masking a missing TRUNCATE trigger.
  PERFORM pg_temp.bom_expect_reject(format('TRUNCATE %I CASCADE',tbl),'P0001');
  IF tbl NOT IN ('bom_document','bom_revision','bom_variant') THEN
   PERFORM pg_temp.bom_expect_reject(format('INSERT INTO %I SELECT * FROM %I LIMIT 1',tbl,tbl),'P0001','immutable');
  END IF;
 END LOOP;
 PERFORM pg_temp.bom_expect_reject('INSERT INTO bom_document("PropertyMask") VALUES(0)','P0001','exactly one');
 PERFORM pg_temp.bom_expect_reject('INSERT INTO bom_variant("RevisionId","Name","CreatedAt") SELECT min("Id"),''Missing root'',now() FROM bom_revision','P0001','missing root');
 PERFORM pg_temp.bom_expect_reject('SELECT bom_import_document(''{"unknownField":1}''::jsonb)','P0001','roundtrip');
 PERFORM pg_temp.bom_expect_reject('SELECT bom_import_document(''{"positions":[{"id":"bad","excluded":true,"componentId":2147483647,"revisionId":2147483647,"unitId":2147483647}]}''::jsonb)','P0001','dangling historical reference');
 PERFORM pg_temp.bom_expect_reject('SELECT pg_temp.bom_bad_parameter(''{"Type":6}''::jsonb)','23514','CK_bom_parameter');
 PERFORM pg_temp.bom_expect_reject('SELECT pg_temp.bom_bad_parameter(''{"Ordinal":-1}''::jsonb)','23514','CK_bom_parameter');
 PERFORM pg_temp.bom_expect_reject('SELECT pg_temp.bom_bad_parameter(''{"DefaultInteger":1}''::jsonb)','23514','CK_bom_parameter_default');
 PERFORM pg_temp.bom_expect_reject('SELECT pg_temp.bom_bad_parameter(''{"DefaultDateText":"1999-01-01T00:00:00Z"}''::jsonb)','23514','CK_bom_parameter_default');
 PERFORM pg_temp.bom_expect_reject('SELECT pg_temp.bom_bad_position(''{"RevisionId":-1}''::jsonb)','23503','FK_bom_position_child');
 PERFORM pg_temp.bom_expect_reject('SELECT pg_temp.bom_bad_position(''{"ComponentId":-1}''::jsonb)','23503','FK_bom_position_child');
 PERFORM pg_temp.bom_expect_reject('SELECT pg_temp.bom_bad_position(''{"UnitId":-1}''::jsonb)','23503','FK_bom_position_child');
 PERFORM pg_temp.bom_expect_reject('SELECT pg_temp.bom_bad_binding(''{"ChildRevisionId":-1}''::jsonb)','P0001','child revision mismatch');
 PERFORM pg_temp.bom_expect_reject('SELECT pg_temp.bom_bad_binding(''{"TargetDocumentId":-1}''::jsonb)','23503','FK_bom_binding_revision');
 PERFORM pg_temp.bom_expect_reject('SELECT pg_temp.bom_bad_binding(''{"ParameterKey":"missing-parameter-key"}''::jsonb)','23503','FK_bom_binding_parameter');
 PERFORM pg_temp.bom_expect_reject('SELECT pg_temp.bom_cross_variant_parent()','23503','FK_bom_node_parent');
 PERFORM pg_temp.bom_expect_reject('SELECT pg_temp.bom_label_only_enum()','P0001','enum outside frozen allowed set');

 -- Re-run order-line integrity against this fixture without assuming a leaf's
 -- unit norm: compare the whole calculated result with calc_bom(variant,4).
 SELECT min("Id") INTO vid FROM bom_variant;
 FOREACH tbl IN ARRAY ARRAY['bom_order_line'] LOOP
  PERFORM pg_temp.bom_expect_reject(format('UPDATE %I SET "Quantity"="Quantity"',tbl),'P0001');
  PERFORM pg_temp.bom_expect_reject(format('DELETE FROM %I',tbl),'P0001');
  PERFORM pg_temp.bom_expect_reject(format('TRUNCATE %I',tbl),'P0001');
 END LOOP;
 BEGIN
  DECLARE oid int; rid int; actual jsonb; expected jsonb; BEGIN
   INSERT INTO bom_order("Number","Date") VALUES('relational-guard-order',current_date) RETURNING "Id" INTO oid;
   INSERT INTO bom_order_revision("OrderId","Reason","CreatedAt") VALUES(oid,'Duplicate frozen variant',now()) RETURNING "Id" INTO rid;
   PERFORM pg_temp.bom_expect_reject(format('INSERT INTO bom_order_line VALUES(%s,0,-2147483648,1)',rid),'23503');
   PERFORM pg_temp.bom_expect_reject(format('INSERT INTO bom_order_line VALUES(%s,0,%s,0)',rid,vid),'23514');
   INSERT INTO bom_order_line VALUES(rid,0,vid,1.5),(rid,1,vid,2.5);
   SET CONSTRAINTS ALL IMMEDIATE;
   IF (SELECT count(*) FROM bom_order_line WHERE "OrderRevisionId"=rid)<>2 THEN RAISE EXCEPTION 'Duplicate variants collapsed'; END IF;
   SELECT coalesce(jsonb_agg(to_jsonb(b) ORDER BY b.product_id,b.unit_id,b.name),'[]'::jsonb) INTO actual FROM calc_order_bom(rid) b;
   SELECT coalesce(jsonb_agg(to_jsonb(b) ORDER BY b.product_id,b.unit_id,b.name),'[]'::jsonb) INTO expected FROM calc_bom(vid,4) b;
   IF actual IS DISTINCT FROM expected THEN RAISE EXCEPTION 'Duplicate fractional order-line totals differ from frozen variant calculation'; END IF;
   SET CONSTRAINTS ALL DEFERRED;
  END;
 END;

 -- Exercise publication validation on new draft documents without altering old
 -- rows. Explicit validation isolates each invariant from ownership validation.
 SELECT min("DocumentId") INTO did FROM bom_revision;
 BEGIN
  DECLARE fresh int; BEGIN
   INSERT INTO bom_document("PropertyMask") VALUES(0) RETURNING "Id" INTO fresh;
   INSERT INTO bom_document_class("DocumentId","Ordinal","ClassId") SELECT fresh,0,"ClassId" FROM bom_document_class WHERE "DocumentId"=did LIMIT 1;
   -- No owner exists yet: the deferred publication check must reject the draft.
   PERFORM pg_temp.bom_expect_reject(format('SELECT bom_validate_document_id(%s)',fresh),'P0001','exactly one');
  END;
 END;
END $$;
ROLLBACK;
