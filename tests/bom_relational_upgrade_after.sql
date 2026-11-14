\set ON_ERROR_STOP on
-- ONLY the disposable database seeded by bom_relational_upgrade_before.sql,
-- after applying NormalizeBomSnapshots. Read-only assertions, safe to repeat.
BEGIN;
DO $$
DECLARE v_case record; v_actual jsonb; v_table text; v_count bigint;
BEGIN
 -- No seed history may disappear, be replaced by a different ID, or gain rows.
 IF (SELECT count(*) FROM test_bom_expected_revision)<>10
 OR (SELECT count(*) FROM test_bom_expected_variant)<>5
 OR (SELECT count(*) FROM test_bom_expected_find)<>10
 OR (SELECT count(*) FROM test_bom_expected_calc)<>40
 OR (SELECT count(*) FROM test_bom_expected_order)<>1
 OR (SELECT count(*) FROM test_bom_expected_order_revision)<>2
 OR (SELECT count(*) FROM test_bom_expected_order_calc)<>8
 OR (SELECT count(*) FROM test_bom_expected_current_catalog)<>14
 THEN RAISE EXCEPTION 'Missing or incomplete pre-upgrade fixture expectations.'; END IF;

 FOREACH v_table IN ARRAY ARRAY[
 'bom_document','bom_document_class','bom_position','bom_position_binding',
 'bom_parameter','bom_parameter_enum','bom_rule','bom_calculation',
 'bom_variant_node','bom_variant_node_class','bom_variant_node_value'] LOOP
  IF NOT EXISTS(SELECT 1 FROM pg_class pc JOIN pg_namespace pn ON pn.oid=pc.relnamespace
    WHERE pn.nspname=current_schema() AND pc.relname=v_table AND pc.relkind IN ('r','p'))
  THEN RAISE EXCEPTION 'Missing normalized BOM table: %',v_table; END IF;
 END LOOP;
 IF EXISTS(SELECT 1 FROM information_schema.columns cc
   JOIN information_schema.tables tt ON tt.table_schema=cc.table_schema AND tt.table_name=cc.table_name
   WHERE cc.table_schema=current_schema() AND left(cc.table_name,4)='bom_'
   AND tt.table_type='BASE TABLE' AND cc.udt_name IN ('json','jsonb','_json','_jsonb'))
 THEN RAISE EXCEPTION 'A persistent BOM table still contains JSON storage.'; END IF;
 IF EXISTS(SELECT 1 FROM information_schema.columns cc WHERE cc.table_schema=current_schema()
   AND ((cc.table_name='bom_revision' AND cc.column_name='Document')
     OR (cc.table_name='bom_variant' AND cc.column_name='Snapshot')))
 THEN RAISE EXCEPTION 'Legacy Document/Snapshot column remains.'; END IF;

 IF EXISTS(SELECT 1 FROM bom_revision br FULL JOIN test_bom_expected_revision er ON er.revision_id=br."Id"
   WHERE br."Id" IS NULL OR er.revision_id IS NULL)
 THEN RAISE EXCEPTION 'Revision IDs were added or lost.'; END IF;
 FOR v_case IN SELECT * FROM test_bom_expected_revision ORDER BY revision_id LOOP
  SELECT to_jsonb(br)-'DocumentId'-'CreationTransaction' INTO v_actual FROM bom_revision br WHERE br."Id"=v_case.revision_id;
  IF v_actual IS DISTINCT FROM v_case.metadata
  THEN RAISE EXCEPTION 'Revision % metadata changed: expected %, actual %',v_case.revision_id,v_case.metadata,v_actual; END IF;
  SELECT bom_read_document(br."DocumentId") INTO v_actual FROM bom_revision br WHERE br."Id"=v_case.revision_id;
  IF v_actual IS DISTINCT FROM v_case.document
  THEN RAISE EXCEPTION 'Revision % raw Document changed.',v_case.revision_id; END IF;
  IF bom_effective_positions(v_case.revision_id) IS DISTINCT FROM v_case.effective_positions
  THEN RAISE EXCEPTION 'Revision % effective positions changed.',v_case.revision_id; END IF;
 END LOOP;

 IF EXISTS(SELECT 1 FROM bom_variant bv FULL JOIN test_bom_expected_variant ev ON ev.variant_id=bv."Id"
   WHERE bv."Id" IS NULL OR ev.variant_id IS NULL)
 THEN RAISE EXCEPTION 'Variant IDs were added or lost.'; END IF;
 FOR v_case IN SELECT * FROM test_bom_expected_variant ORDER BY variant_id LOOP
  SELECT to_jsonb(bv)-'CreationTransaction' INTO v_actual FROM bom_variant bv WHERE bv."Id"=v_case.variant_id;
  IF v_actual IS DISTINCT FROM v_case.metadata
  THEN RAISE EXCEPTION 'Variant % metadata changed: expected %, actual %',v_case.variant_id,v_case.metadata,v_actual; END IF;
  IF bom_read_variant(v_case.variant_id) IS DISTINCT FROM v_case.snapshot
  THEN RAISE EXCEPTION 'Variant % raw Snapshot changed, including frozen Source/labels/defaults/presence.',v_case.variant_id; END IF;
 END LOOP;

 FOR v_case IN SELECT * FROM test_bom_expected_find ORDER BY variant_id,quantity LOOP
  SELECT coalesce(jsonb_agg(to_jsonb(b) ORDER BY b.path),'[]'::jsonb) INTO v_actual FROM find_bom(v_case.variant_id,v_case.quantity) b;
  IF v_actual IS DISTINCT FROM v_case.rows
  THEN RAISE EXCEPTION 'find_bom changed for variant %, quantity %: expected %, actual %',v_case.variant_id,v_case.quantity,v_case.rows,v_actual; END IF;
 END LOOP;
 FOR v_case IN SELECT * FROM test_bom_expected_calc ORDER BY variant_id,quantity,class_id NULLS FIRST LOOP
  SELECT coalesce(jsonb_agg(to_jsonb(b) ORDER BY b.product_id,b.name,b.unit_id,b.unit),'[]'::jsonb) INTO v_actual FROM calc_bom(v_case.variant_id,v_case.quantity,v_case.class_id) b;
  IF v_actual IS DISTINCT FROM v_case.rows
  THEN RAISE EXCEPTION 'calc_bom changed for variant %, quantity %, class %: expected %, actual %',v_case.variant_id,v_case.quantity,v_case.class_id,v_case.rows,v_actual; END IF;
 END LOOP;

 IF EXISTS(SELECT 1 FROM bom_order bo FULL JOIN test_bom_expected_order eo ON eo.order_id=bo."Id"
   WHERE bo."Id" IS NULL OR eo.order_id IS NULL OR to_jsonb(bo) IS DISTINCT FROM eo.metadata)
 THEN RAISE EXCEPTION 'Orders were added, lost or changed.'; END IF;
 IF EXISTS(SELECT 1 FROM bom_order_revision bor FULL JOIN test_bom_expected_order_revision eor ON eor.revision_id=bor."Id"
   WHERE bor."Id" IS NULL OR eor.revision_id IS NULL OR to_jsonb(bor) IS DISTINCT FROM eor.metadata)
 THEN RAISE EXCEPTION 'Order revisions were added, lost or changed (including original CreationTransaction).'; END IF;
 IF (SELECT count(*) FROM bom_order_line)<>(SELECT sum(jsonb_array_length(eor.lines)) FROM test_bom_expected_order_revision eor)
 OR EXISTS(SELECT 1 FROM bom_order_line bol WHERE NOT EXISTS(SELECT 1 FROM test_bom_expected_order_revision eor WHERE eor.revision_id=bol."OrderRevisionId"))
 THEN RAISE EXCEPTION 'Order lines were added or lost.'; END IF;
 FOR v_case IN SELECT * FROM test_bom_expected_order_revision ORDER BY revision_id LOOP
  SELECT coalesce(jsonb_agg(to_jsonb(bol) ORDER BY bol."SortOrder"),'[]'::jsonb) INTO v_actual FROM bom_order_line bol WHERE bol."OrderRevisionId"=v_case.revision_id;
  IF v_actual IS DISTINCT FROM v_case.lines
  THEN RAISE EXCEPTION 'Order revision % ordered lines changed.',v_case.revision_id; END IF;
  SELECT coalesce(jsonb_agg(to_jsonb(b) ORDER BY b.product_id,b.name,b.unit_id,b.unit),'[]'::jsonb) INTO v_actual FROM calc_order_bom(v_case.revision_id) b;
  IF v_actual IS DISTINCT FROM v_case.totals
  THEN RAISE EXCEPTION 'Order revision % totals changed.',v_case.revision_id; END IF;
 END LOOP;
 FOR v_case IN SELECT * FROM test_bom_expected_order_calc ORDER BY order_revision_id,class_id NULLS FIRST LOOP
  SELECT coalesce(jsonb_agg(to_jsonb(b) ORDER BY b.product_id,b.name,b.unit_id,b.unit),'[]'::jsonb) INTO v_actual FROM calc_order_bom(v_case.order_revision_id,v_case.class_id) b;
  IF v_actual IS DISTINCT FROM v_case.rows
  THEN RAISE EXCEPTION 'calc_order_bom changed for revision %, class %: expected %, actual %',v_case.order_revision_id,v_case.class_id,v_case.rows,v_actual; END IF;
 END LOOP;


 -- Count each normalized relation from original own documents and every frozen
 -- node Source. Even a legacy node with absent Source owns an empty document.
 FOR v_case IN
  WITH RECURSIVE original_nodes AS (
   SELECT ev.variant_id,ev.snapshot node FROM test_bom_expected_variant ev
   UNION ALL SELECT pn.variant_id,ch.value FROM original_nodes pn CROSS JOIN LATERAL jsonb_array_elements(coalesce(pn.node->'children','[]'::jsonb)) ch
  ), original_documents AS (
   SELECT er.document doc FROM test_bom_expected_revision er
   UNION ALL SELECT coalesce(pn.node->'source','{}'::jsonb) FROM original_nodes pn
  ), original_positions AS (
   SELECT ep.value pos FROM original_documents od CROSS JOIN LATERAL jsonb_array_elements(coalesce(od.doc->'positions','[]'::jsonb)) ep
  ), original_parameters AS (
   SELECT ep.value param FROM original_documents od CROSS JOIN LATERAL jsonb_array_elements(coalesce(od.doc->'parameters','[]'::jsonb)) ep
  )
  SELECT 'bom_document'::text table_name,count(*) expected_count FROM original_documents
  UNION ALL SELECT 'bom_document_class',coalesce(sum(jsonb_array_length(coalesce(od.doc->'classes','[]'::jsonb))),0) FROM original_documents od
  UNION ALL SELECT 'bom_position',count(*) FROM original_positions
  UNION ALL SELECT 'bom_position_binding',count(*) FROM original_positions op CROSS JOIN LATERAL jsonb_each(coalesce(op.pos->'bindings','{}'::jsonb)) eb
  UNION ALL SELECT 'bom_parameter',count(*) FROM original_parameters
  UNION ALL SELECT 'bom_parameter_enum',coalesce(sum((SELECT count(*) FROM (
   SELECT ei.value FROM jsonb_array_elements_text(coalesce(op.param->'enumIds','[]'::jsonb)) ei
   UNION SELECT el.key FROM jsonb_each(coalesce(op.param->'enumLabels','{}'::jsonb)) el) allowed)),0) FROM original_parameters op
  UNION ALL SELECT 'bom_rule',coalesce(sum(jsonb_array_length(coalesce(od.doc->'rules','[]'::jsonb))),0) FROM original_documents od
  UNION ALL SELECT 'bom_calculation',count(*) FROM original_documents od CROSS JOIN LATERAL jsonb_each(coalesce(od.doc->'calculations','{}'::jsonb)) ec
  UNION ALL SELECT 'bom_variant_node',count(*) FROM original_nodes
  UNION ALL SELECT 'bom_variant_node_class',coalesce(sum(jsonb_array_length(coalesce(pn.node->'classes','[]'::jsonb))),0) FROM original_nodes pn
  UNION ALL SELECT 'bom_variant_node_value',count(*) FROM original_nodes pn CROSS JOIN LATERAL jsonb_each(coalesce(pn.node->'values','{}'::jsonb)) nv
 LOOP
  EXECUTE format('SELECT count(*) FROM %I',v_case.table_name) INTO v_count;
  IF v_count IS DISTINCT FROM v_case.expected_count
  THEN RAISE EXCEPTION 'Normalized table % row count changed: expected %, actual %',v_case.table_name,v_case.expected_count,v_count; END IF;
 END LOOP;

 -- Compare catalog rows against their AFTER-rename state, rather than frozen names.
 IF EXISTS(
  WITH test_catalog_rows AS (
   SELECT 'product'::text kind,pp."Id" id,to_jsonb(pp) metadata FROM product pp WHERE pp."Id" IN (SELECT ec.id FROM test_bom_expected_current_catalog ec WHERE ec.kind='product')
   UNION ALL SELECT 'class',cc."Id",to_jsonb(cc) FROM class_node cc WHERE cc."Id" IN (SELECT ec.id FROM test_bom_expected_current_catalog ec WHERE ec.kind='class')
   UNION ALL SELECT 'unit',mu."Id",to_jsonb(mu) FROM measure_unit mu WHERE mu."Id" IN (SELECT ec.id FROM test_bom_expected_current_catalog ec WHERE ec.kind='unit')
   UNION ALL SELECT 'definition',pd."Id",to_jsonb(pd) FROM parameter_definition pd WHERE pd."Id" IN (SELECT ec.id FROM test_bom_expected_current_catalog ec WHERE ec.kind='definition')
   UNION ALL SELECT 'enum_value',ev."Id",to_jsonb(ev) FROM enum_value ev WHERE ev."Id" IN (SELECT ec.id FROM test_bom_expected_current_catalog ec WHERE ec.kind='enum_value')
  ) SELECT 1 FROM test_catalog_rows ac FULL JOIN test_bom_expected_current_catalog ec ON ec.kind=ac.kind AND ec.id=ac.id
  WHERE ac.id IS NULL OR ec.id IS NULL OR ac.metadata IS DISTINCT FROM ec.metadata)
 THEN RAISE EXCEPTION 'Current renamed catalog data changed during migration.'; END IF;
 IF EXISTS(SELECT 1 FROM test_bom_expected_current_catalog ec FULL JOIN test_bom_expected_catalog oldc ON ec.kind=oldc.kind AND ec.id=oldc.id
   WHERE ec.id IS NULL OR oldc.id IS NULL OR ec.metadata IS NOT DISTINCT FROM oldc.metadata)
 THEN RAISE EXCEPTION 'Fixture failed to distinguish historical labels from the current catalog.'; END IF;
END $$;
COMMIT;
