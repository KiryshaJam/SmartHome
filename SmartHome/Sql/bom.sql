-- Eight related BOM tables. JSON here is only a function/API transport format.
-- DateTime ticks keep the full precision of .NET values in an ordinary bigint.
CREATE FUNCTION bom_date_ticks(value text) RETURNS bigint LANGUAGE sql IMMUTABLE STRICT SET TimeZone='UTC' AS $$
 SELECT (extract(epoch FROM (regexp_replace(value,'(\.[0-9]{6})[0-9]','\1')::timestamptz - timestamptz '0001-01-01 00:00:00+00'))*10000000)::bigint
   + coalesce(substring(value FROM '\.[0-9]{6}([0-9])')::bigint,0)
$$;
CREATE FUNCTION bom_date_json(ticks bigint) RETURNS jsonb LANGUAGE sql IMMUTABLE STRICT SET TimeZone='UTC' AS $$
 SELECT to_jsonb(to_char(timestamptz '0001-01-01 00:00:00+00' + (ticks/864000000000)*interval '1 day'
   + ((ticks%864000000000)/10000000)*interval '1 second' + ((ticks%10000000)/10)*interval '1 microsecond',
   'YYYY-MM-DD"T"HH24:MI:SS.US') || (ticks%10)::text || 'Z')
$$;
CREATE FUNCTION bom_classes(pid int) RETURNS jsonb LANGUAGE sql STABLE AS $$
 WITH RECURSIVE c AS (
  SELECT n."Id",n."ParentId",0 depth FROM class_node n JOIN product p ON p."ClassNodeId"=n."Id" WHERE p."Id"=pid
  UNION ALL SELECT n."Id",n."ParentId",c.depth+1 FROM class_node n JOIN c ON c."ParentId"=n."Id"
 ) SELECT coalesce(jsonb_agg("Id" ORDER BY depth),'[]') FROM c
$$;
CREATE FUNCTION bom_read_document(rid int) RETURNS jsonb LANGUAGE sql STABLE AS $$
 SELECT jsonb_build_object('productName',prod."Name",'unitName',u."ShortName",'classes',bom_classes(r."ProductId"),
 'positions',coalesce((SELECT jsonb_agg(jsonb_build_object('id',p."PositionKey",'componentId',coalesce(c."ProductId",0),
  'revisionId',coalesce(c."SourceRevisionId",c."Id",0),'unitId',coalesce(c."UnitId",0),'amount',p."Amount",'forQuantity',p."ForQuantity",
  'sortOrder',p."SortOrder",'condition',p."Condition",'formula',p."Formula",'excluded',p."Excluded",'inherited',false,
  'childVariantId',CASE WHEN c."IsConfigured" THEN c."Id" END,
  'bindings',coalesce((SELECT jsonb_object_agg('p'||b."DefinitionId",b."Expression") FROM bom_position_binding b WHERE b."RevisionId"=p."RevisionId" AND b."PositionKey"=p."PositionKey"),'{}')) ORDER BY p."SortOrder",p."PositionKey")
  FROM bom_position p LEFT JOIN bom_revision c ON c."Id"=p."ComponentRevisionId" WHERE p."RevisionId"=rid),'[]'),
 'parameters',coalesce((SELECT jsonb_agg(jsonb_build_object('key','p'||p."DefinitionId",'definitionId',p."DefinitionId",'name',d."Name",'type',p."Type",'required',p."Required",'min',p."Min",'max',p."Max",
  'default',coalesce(to_jsonb(p."IntegerValue"),to_jsonb(p."NumberValue"),to_jsonb(p."StringValue"),bom_date_json(p."DateTimeTicks"),to_jsonb(p."EnumValueId"),'null'),
  'enumIds',coalesce((SELECT jsonb_agg(o."EnumValueId" ORDER BY o."EnumValueId") FROM bom_parameter_option o WHERE o."RevisionId"=rid AND o."DefinitionId"=p."DefinitionId"),'[]'),
  'enumLabels',coalesce((SELECT jsonb_object_agg(e."Id"::text,coalesce(e."DisplayName",e."StringValue",e."NumberValue"::text,'#'||e."Id")) FROM bom_parameter_option o JOIN enum_value e ON e."Id"=o."EnumValueId" WHERE o."RevisionId"=rid AND o."DefinitionId"=p."DefinitionId"),'{}')) ORDER BY p."SortOrder",p."DefinitionId")
  FROM bom_parameter p JOIN parameter_definition d ON d."Id"=p."DefinitionId" WHERE p."RevisionId"=rid),'[]'),
 'calculations',coalesce((SELECT jsonb_object_agg('p'||p."DefinitionId",p."Formula") FROM bom_parameter p WHERE p."RevisionId"=rid AND p."Formula" IS NOT NULL),'{}'),
 'rules',coalesce((SELECT jsonb_agg(jsonb_build_object('expression',p."Expression",'message',p."Message") ORDER BY p."SortOrder") FROM bom_rule p WHERE p."RevisionId"=rid),'[]'))
 FROM bom_revision r JOIN product prod ON prod."Id"=r."ProductId" JOIN measure_unit u ON u."Id"=r."UnitId" WHERE r."Id"=rid
$$;
CREATE FUNCTION bom_effective_positions(rid int, nesting int DEFAULT 0) RETURNS jsonb LANGUAGE plpgsql STABLE AS $$
DECLARE own jsonb; basis jsonb; base_id int;
BEGIN
 IF nesting>32 THEN RAISE EXCEPTION 'Слишком длинная цепочка модификаций.'; END IF;
 SELECT bom_read_document(rid)->'positions',"BaseRevisionId" INTO own,base_id FROM bom_revision WHERE "Id"=rid;
 IF base_id IS NULL THEN RETURN own; END IF;
 basis:=bom_effective_positions(base_id,nesting+1);
 RETURN (SELECT coalesce(jsonb_agg(x ORDER BY (x->>'sortOrder')::int,x->>'id'),'[]') FROM (
  SELECT x || '{"inherited":true}'::jsonb x FROM jsonb_array_elements(basis) x WHERE NOT EXISTS(SELECT 1 FROM jsonb_array_elements(own) o WHERE o->>'id'=x->>'id')
  UNION ALL SELECT x FROM jsonb_array_elements(own) x
 ) merged WHERE NOT coalesce((x->>'excluded')::bool,false));
END $$;
CREATE FUNCTION bom_read_variant(rid int, nesting int DEFAULT 0) RETURNS jsonb LANGUAGE plpgsql STABLE AS $$
DECLARE result jsonb; r bom_revision;
BEGIN
 IF nesting>32 THEN RAISE EXCEPTION 'Слишком глубокий состав.'; END IF;
 SELECT * INTO STRICT r FROM bom_revision WHERE "Id"=rid AND "IsConfigured" AND "IsFinalized";
 SELECT jsonb_build_object('productId',r."ProductId",'revisionId',r."SourceRevisionId",'name',p."Name",'unitId',r."UnitId",'unit',u."ShortName",'classes',bom_classes(r."ProductId"),
  'positionId','','quantity',r."RootQuantity",'amount',r."RootAmount",'forQuantity',r."RootForQuantity",
  'values',coalesce((SELECT jsonb_object_agg('p'||v."DefinitionId",coalesce(to_jsonb(v."IntegerValue"),to_jsonb(v."NumberValue"),to_jsonb(v."StringValue"),bom_date_json(v."DateTimeTicks"),to_jsonb(v."EnumValueId"))) FROM bom_parameter v WHERE v."RevisionId"=rid AND num_nonnulls(v."IntegerValue",v."NumberValue",v."StringValue",v."DateTimeTicks",v."EnumValueId")=1),'{}'),
  'children',coalesce((SELECT jsonb_agg(bom_read_variant(pos."ComponentRevisionId",nesting+1) || jsonb_build_object('positionId',pos."PositionKey",'amount',pos."Amount",'forQuantity',pos."ForQuantity",'quantity',pos."ResolvedQuantity") ORDER BY pos."SortOrder",pos."PositionKey") FROM bom_position pos WHERE pos."RevisionId"=rid),'[]')) INTO result
 FROM product p JOIN measure_unit u ON u."Id"=r."UnitId" WHERE p."Id"=r."ProductId";
 RETURN result;
END $$;
-- The same typed importer is used for template defaults and configured values.
CREATE FUNCTION bom_import_parameters(rid int, document jsonb, actual jsonb DEFAULT NULL) RETURNS void LANGUAGE plpgsql AS $$
DECLARE p jsonb; v jsonb; d int; typ int; ordinal int:=0; k text;
BEGIN
 FOR p IN SELECT value FROM jsonb_array_elements(coalesce(document->'parameters','[]')) LOOP
  d:=(p->>'definitionId')::int; typ:=(p->>'type')::int; k:='p'||d;
  IF p->>'key'<>k THEN RAISE EXCEPTION 'Неверный ключ параметра %',p->>'key'; END IF;
  v:=CASE WHEN actual IS NULL THEN p->'default' ELSE actual->k END;
  INSERT INTO bom_parameter("RevisionId","DefinitionId","SortOrder","Type","Required","Min","Max","Formula","IntegerValue","NumberValue","StringValue","DateTimeTicks","EnumValueId")
  VALUES(rid,d,ordinal,typ,coalesce((p->>'required')::bool,false),(p->>'min')::numeric,(p->>'max')::numeric,CASE WHEN actual IS NULL THEN document->'calculations'->>k END,
    CASE WHEN typ=1 THEN (v#>>'{}')::int END, CASE WHEN typ=2 THEN (v#>>'{}')::numeric END,CASE WHEN typ=3 THEN v#>>'{}' END,
    CASE WHEN typ=4 THEN bom_date_ticks(v#>>'{}') END,CASE WHEN typ=5 THEN (v#>>'{}')::int END);
  INSERT INTO bom_parameter_option("RevisionId","DefinitionId","EnumValueId") SELECT rid,d,value::int FROM jsonb_array_elements_text(coalesce(p->'enumIds','[]'));
  ordinal:=ordinal+1;
 END LOOP;
 IF actual IS NOT NULL AND EXISTS(SELECT 1 FROM jsonb_object_keys(actual) AS supplied(parameter_key) WHERE NOT EXISTS(SELECT 1 FROM bom_parameter stored WHERE stored."RevisionId"=rid AND supplied.parameter_key='p'||stored."DefinitionId")) THEN RAISE EXCEPTION 'Неизвестное фактическое значение параметра'; END IF;
END $$;
CREATE FUNCTION bom_import_positions(rid int, document jsonb) RETURNS void LANGUAGE plpgsql AS $$
DECLARE p jsonb; child_id int; excluded bool; b record;
BEGIN
 FOR p IN SELECT value FROM jsonb_array_elements(coalesce(document->'positions','[]')) LOOP
  excluded:=coalesce((p->>'excluded')::bool,false);
  child_id:=CASE WHEN excluded THEN NULL ELSE coalesce((p->>'childVariantId')::int,(p->>'revisionId')::int) END;
  IF NOT excluded AND NOT EXISTS(SELECT 1 FROM bom_revision c WHERE c."Id"=child_id AND c."ProductId"=(p->>'componentId')::int AND c."UnitId"=(p->>'unitId')::int AND coalesce(c."SourceRevisionId",c."Id")=(p->>'revisionId')::int) THEN RAISE EXCEPTION 'Единица или версия не соответствует компоненту'; END IF;
  INSERT INTO bom_position("RevisionId","PositionKey","ComponentRevisionId","Amount","ForQuantity","ResolvedQuantity","SortOrder","Condition","Formula","Excluded") VALUES(rid,p->>'id',child_id,coalesce((p->>'amount')::numeric,1),coalesce((p->>'forQuantity')::numeric,1),NULL,coalesce((p->>'sortOrder')::int,0),p->>'condition',p->>'formula',excluded);
  FOR b IN SELECT * FROM jsonb_each_text(coalesce(p->'bindings','{}')) LOOP
   IF b.key !~ '^p[0-9]+$' THEN RAISE EXCEPTION 'Неверный ключ передачи параметра'; END IF;
   INSERT INTO bom_position_binding("RevisionId","PositionKey","DefinitionId","Expression") VALUES(rid,p->>'id',substring(b.key FROM 2)::int,b.value);
  END LOOP;
 END LOOP;
 INSERT INTO bom_rule("RevisionId","SortOrder","Expression","Message") SELECT rid,(ordinality-1)::int,value->>'expression',value->>'message' FROM jsonb_array_elements(coalesce(document->'rules','[]')) WITH ORDINALITY;
END $$;
CREATE FUNCTION register_change(product_id int, unit_id int, expected_revision_id int, base_revision_id int, document jsonb, reason text) RETURNS int LANGUAGE plpgsql AS $$
DECLARE rid int; latest_id int;
BEGIN
 PERFORM pg_advisory_xact_lock(734021,1);
 SELECT "Id" INTO latest_id FROM bom_revision WHERE "ProductId"=product_id AND NOT "IsConfigured" ORDER BY "Id" DESC LIMIT 1;
 IF latest_id IS DISTINCT FROM expected_revision_id THEN RAISE EXCEPTION 'Спецификация уже изменена.' USING ERRCODE='40001'; END IF;
 IF length(trim(reason)) NOT BETWEEN 1 AND 512 THEN RAISE EXCEPTION 'Укажите причину изменения.'; END IF;
 INSERT INTO bom_revision("ProductId","UnitId","IsConfigured","IsFinalized","PreviousId","BaseRevisionId","Reason","CreatedAt","RootAmount","RootForQuantity","RootQuantity") VALUES(product_id,unit_id,false,false,latest_id,base_revision_id,reason,now(),1,1,1) RETURNING "Id" INTO rid;
 PERFORM bom_import_parameters(rid,document); PERFORM bom_import_positions(rid,document);
 UPDATE bom_revision SET "IsFinalized"=true WHERE "Id"=rid;
 RETURN rid;
END $$;
CREATE FUNCTION bom_import_configured(node jsonb, variant_name text DEFAULT NULL, forced_id int DEFAULT NULL, creation_time timestamptz DEFAULT now(), nesting int DEFAULT 0) RETURNS int LANGUAGE plpgsql AS $$
DECLARE rid int; child jsonb; child_id int; ordinal int:=0; document jsonb;
BEGIN
 IF nesting>32 THEN RAISE EXCEPTION 'Слишком глубокий состав.'; END IF;
 rid:=coalesce(forced_id,nextval(pg_get_serial_sequence('bom_revision','Id'))::int);
 INSERT INTO bom_revision("Id","ProductId","UnitId","IsConfigured","IsFinalized","SourceRevisionId","Name","Reason","CreatedAt","RootAmount","RootForQuantity","RootQuantity")
 VALUES(rid,(node->>'productId')::int,(node->>'unitId')::int,true,false,(node->>'revisionId')::int,variant_name,'Конфигурация',creation_time,coalesce((node->>'amount')::numeric,(node->>'quantity')::numeric,1),coalesce((node->>'forQuantity')::numeric,1),coalesce((node->>'quantity')::numeric,1));
 document:=coalesce(node->'source','{"parameters":[]}'::jsonb);
 PERFORM bom_import_parameters(rid,document,coalesce(node->'values','{}'));
 FOR child IN SELECT value FROM jsonb_array_elements(coalesce(node->'children','[]')) LOOP
  IF child->>'configuredRevisionId' IS NOT NULL THEN
   child_id:=(child->>'configuredRevisionId')::int;
  ELSE
   child_id:=bom_import_configured(child,NULL,NULL,creation_time,nesting+1);
  END IF;
  IF abs(coalesce((child->>'amount')::numeric,(child->>'quantity')::numeric)-(child->>'quantity')::numeric*coalesce((child->>'forQuantity')::numeric,1))>abs(coalesce((child->>'forQuantity')::numeric,1))*power(10::numeric,-scale((child->>'quantity')::numeric))/2 THEN RAISE EXCEPTION 'Несогласованные сохранённые нормы'; END IF;
  INSERT INTO bom_position("RevisionId","PositionKey","ComponentRevisionId","Amount","ForQuantity","ResolvedQuantity","SortOrder","Condition","Formula","Excluded") VALUES(rid,child->>'positionId',child_id,coalesce((child->>'amount')::numeric,(child->>'quantity')::numeric),coalesce((child->>'forQuantity')::numeric,1),(child->>'quantity')::numeric,ordinal,NULL,NULL,false);
  ordinal:=ordinal+1;
 END LOOP;
 UPDATE bom_revision SET "IsFinalized"=true WHERE "Id"=rid;
 RETURN rid;
END $$;
CREATE FUNCTION bom_create_variant(revision_id int, variant_name text, snapshot jsonb) RETURNS int LANGUAGE plpgsql AS $$
BEGIN
 IF length(trim(variant_name)) NOT BETWEEN 1 AND 256 OR (snapshot->>'revisionId')::int IS DISTINCT FROM revision_id THEN RAISE EXCEPTION 'Неверное имя или исходная версия исполнения'; END IF;
 RETURN bom_import_configured(snapshot,trim(variant_name));
END $$;
CREATE FUNCTION create_modification(base_revision_id int, name text, short_name text, reason text) RETURNS int LANGUAGE plpgsql AS $$
DECLARE pid int; b bom_revision; document jsonb;
BEGIN
 SELECT * INTO STRICT b FROM bom_revision WHERE "Id"=base_revision_id AND NOT "IsConfigured";
 INSERT INTO product("Name","ShortName","ClassNodeId") SELECT name,short_name,"ClassNodeId" FROM product WHERE "Id"=b."ProductId" RETURNING "Id" INTO pid;
 document:=bom_read_document(b."Id") || '{"positions":[]}'::jsonb;
 PERFORM register_change(pid,b."UnitId",NULL,b."Id",document,reason);
 RETURN pid;
END $$;
CREATE FUNCTION find_bom(variant_id int, quantity numeric DEFAULT 1)
RETURNS TABLE(path text,depth int,product_id int,name text,unit_id int,unit text,required_quantity numeric,classes jsonb,leaf bool,revision_id int,norm numeric,for_quantity numeric,position_id text) LANGUAGE plpgsql STABLE AS $$
BEGIN
 IF quantity IS NULL OR quantity<=0 OR quantity>1000000000 OR NOT EXISTS(SELECT 1 FROM bom_revision WHERE "Id"=variant_id AND "IsConfigured" AND "IsFinalized") THEN RAISE EXCEPTION 'Неверное исполнение или количество.'; END IF;
 RETURN QUERY WITH RECURSIVE tree AS (
  SELECT r."Id" rid,''::text route,0 lvl,quantity q,r."RootAmount" amount,r."RootForQuantity" denominator,''::text position FROM bom_revision r WHERE r."Id"=variant_id
  UNION ALL SELECT pos."ComponentRevisionId",tree.route||'/'||pos."PositionKey",tree.lvl+1,tree.q*pos."ResolvedQuantity",pos."Amount",pos."ForQuantity",pos."PositionKey" FROM tree JOIN bom_position pos ON pos."RevisionId"=tree.rid WHERE tree.lvl<32
 ) SELECT tree.route,tree.lvl,r."ProductId",p."Name"::text,r."UnitId",u."ShortName"::text,tree.q,bom_classes(r."ProductId"),NOT EXISTS(SELECT 1 FROM bom_position pos WHERE pos."RevisionId"=tree.rid),r."SourceRevisionId",tree.amount,tree.denominator,tree.position::text
 FROM tree JOIN bom_revision r ON r."Id"=tree.rid JOIN product p ON p."Id"=r."ProductId" JOIN measure_unit u ON u."Id"=r."UnitId" ORDER BY tree.route;
END $$;
CREATE FUNCTION calc_bom(variant_id int, quantity numeric DEFAULT 1, class_id int DEFAULT NULL)
RETURNS TABLE(product_id int,name text,unit_id int,unit text,required_quantity numeric) LANGUAGE sql STABLE AS $$
 SELECT f.product_id,f.name,f.unit_id,f.unit,sum(f.required_quantity) FROM find_bom(variant_id,quantity) f
 WHERE f.leaf AND (class_id IS NULL OR f.classes @> to_jsonb(ARRAY[class_id]))
 GROUP BY f.product_id,f.name,f.unit_id,f.unit ORDER BY f.name,f.unit_id
$$;
CREATE FUNCTION calc_order_bom(order_id int, class_id int DEFAULT NULL)
RETURNS TABLE(product_id int,name text,unit_id int,unit text,required_quantity numeric) LANGUAGE sql STABLE AS $$
 SELECT f.product_id,f.name,f.unit_id,f.unit,sum(f.required_quantity) FROM bom_order_line l CROSS JOIN LATERAL calc_bom(l."ConfiguredRevisionId",l."Quantity",class_id) f WHERE l."OrderId"=order_id GROUP BY f.product_id,f.name,f.unit_id,f.unit ORDER BY f.name,f.unit_id
$$;
-- Saved specifications are immutable. Build related rows, then finalize the revision.
CREATE FUNCTION bom_validate_revision(rid int) RETURNS void LANGUAGE plpgsql AS $$
DECLARE r bom_revision;
BEGIN
 SELECT * INTO STRICT r FROM bom_revision WHERE "Id"=rid;
 IF NOT (r."RootAmount">0 AND r."RootAmount"<=1000000000 AND r."RootForQuantity">0 AND r."RootForQuantity"<=1000000000 AND r."RootQuantity">0 AND r."RootQuantity"<=1000000000) THEN RAISE EXCEPTION 'Неверная корневая норма'; END IF;
 IF r."PreviousId" IS NOT NULL AND NOT EXISTS(SELECT 1 FROM bom_revision p WHERE p."Id"=r."PreviousId" AND p."ProductId"=r."ProductId" AND NOT p."IsConfigured") THEN RAISE EXCEPTION 'Неверная предыдущая версия'; END IF;
 IF r."BaseRevisionId" IS NOT NULL AND NOT EXISTS(SELECT 1 FROM bom_revision b WHERE b."Id"=r."BaseRevisionId" AND NOT b."IsConfigured" AND b."UnitId"=r."UnitId" AND b."ProductId"<>r."ProductId") THEN RAISE EXCEPTION 'Неверная базовая версия'; END IF;
 IF r."IsConfigured" AND NOT EXISTS(SELECT 1 FROM bom_revision s WHERE s."Id"=r."SourceRevisionId" AND NOT s."IsConfigured" AND s."ProductId"=r."ProductId" AND s."UnitId"=r."UnitId") THEN RAISE EXCEPTION 'Неверный источник исполнения'; END IF;
 IF EXISTS(SELECT 1 FROM bom_parameter p WHERE p."RevisionId"=rid AND (p."NumberValue"::text IN ('NaN','Infinity','-Infinity') OR p."Min"::text IN ('NaN','Infinity','-Infinity') OR p."Max"::text IN ('NaN','Infinity','-Infinity') OR p."DateTimeTicks"<0 OR p."DateTimeTicks">3155378975999999999 OR r."IsConfigured" AND p."Required" AND p."Type"=3 AND length(trim(p."StringValue"))=0)) THEN RAISE EXCEPTION 'Неверное значение параметра'; END IF;
 IF EXISTS(SELECT 1 FROM bom_parameter p JOIN parameter_definition d ON d."Id"=p."DefinitionId" WHERE p."RevisionId"=rid AND (p."Type"<>d."ValueType" OR
  (coalesce(p."IntegerValue",p."NumberValue") IS NOT NULL AND ((p."Min" IS NOT NULL AND coalesce(p."IntegerValue",p."NumberValue")<p."Min") OR (p."Max" IS NOT NULL AND coalesce(p."IntegerValue",p."NumberValue")>p."Max"))) OR
  (r."IsConfigured" AND (p."Formula" IS NOT NULL OR p."Required" AND num_nonnulls(p."IntegerValue",p."NumberValue",p."StringValue",p."DateTimeTicks",p."EnumValueId")=0)) OR
  (p."EnumValueId" IS NOT NULL AND NOT EXISTS(SELECT 1 FROM bom_parameter_option o WHERE o."RevisionId"=rid AND o."DefinitionId"=p."DefinitionId" AND o."EnumValueId"=p."EnumValueId")))) THEN RAISE EXCEPTION 'Неверное значение, тип или ограничение параметра'; END IF;
 IF EXISTS(SELECT 1 FROM bom_parameter_option o JOIN bom_parameter p USING("RevisionId","DefinitionId") JOIN parameter_definition d ON d."Id"=p."DefinitionId" JOIN enum_value v ON v."Id"=o."EnumValueId" WHERE o."RevisionId"=rid AND (p."Type"<>5 OR v."EnumClassId" IS DISTINCT FROM d."EnumClassId")) THEN RAISE EXCEPTION 'Неверное допустимое значение перечисления'; END IF;
 IF EXISTS(SELECT 1 FROM bom_position p LEFT JOIN bom_revision c ON c."Id"=p."ComponentRevisionId" WHERE p."RevisionId"=rid AND
  (r."IsConfigured" AND (NOT c."IsConfigured" OR p."Excluded" OR p."Condition" IS NOT NULL OR p."Formula" IS NOT NULL OR p."ResolvedQuantity" IS NULL OR p."ResolvedQuantity"<=0 OR p."ResolvedQuantity">1000000000)
   OR NOT r."IsConfigured" AND p."ResolvedQuantity" IS NOT NULL)) THEN RAISE EXCEPTION 'Неверная фиксированная позиция'; END IF;
 IF r."IsConfigured" AND (EXISTS(SELECT 1 FROM bom_rule WHERE "RevisionId"=rid) OR EXISTS(SELECT 1 FROM bom_position_binding WHERE "RevisionId"=rid)) THEN RAISE EXCEPTION 'Исполнение не содержит исполняемых правил'; END IF;
 IF EXISTS(SELECT 1 FROM bom_position_binding b JOIN bom_position p USING("RevisionId","PositionKey") LEFT JOIN bom_revision c ON c."Id"=p."ComponentRevisionId" WHERE b."RevisionId"=rid AND (p."Excluded" OR c."IsConfigured" OR c."Id" IS NULL OR NOT EXISTS(SELECT 1 FROM bom_parameter target WHERE target."RevisionId"=c."Id" AND target."DefinitionId"=b."DefinitionId"))) THEN RAISE EXCEPTION 'Неверный параметр вложенной сборки'; END IF;
 IF EXISTS(WITH RECURSIVE walk AS (
  SELECT rid node,ARRAY[r."ProductId"] products,0 depth,false cycle
  UNION ALL SELECT CASE WHEN r."IsConfigured" OR child."IsConfigured" THEN child."Id" ELSE coalesce((SELECT latest."Id" FROM bom_revision latest WHERE latest."ProductId"=child."ProductId" AND NOT latest."IsConfigured" ORDER BY latest."Id" DESC LIMIT 1),child."Id") END,walk.products||child."ProductId",walk.depth+1,child."ProductId"=ANY(walk.products)
  FROM walk CROSS JOIN LATERAL jsonb_array_elements(bom_effective_positions(walk.node)) pos
  JOIN bom_revision child ON child."Id"=coalesce((pos->>'childVariantId')::int,(pos->>'revisionId')::int)
  WHERE NOT walk.cycle AND walk.depth<=32 AND NOT coalesce((pos->>'excluded')::bool,false)
 ) SELECT 1 FROM walk WHERE cycle OR depth>32) THEN RAISE EXCEPTION 'Цикл или глубина состава превышает 32'; END IF;
END $$;
CREATE FUNCTION bom_revision_guard() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF TG_OP='DELETE' THEN RAISE EXCEPTION 'История спецификаций неизменна'; END IF;
 IF TG_OP='INSERT' THEN
  IF NEW."IsFinalized" THEN RAISE EXCEPTION 'Сначала заполните строки спецификации'; END IF;
 ELSE
  IF OLD."IsFinalized" OR NOT NEW."IsFinalized" OR (to_jsonb(NEW)-'IsFinalized') IS DISTINCT FROM (to_jsonb(OLD)-'IsFinalized') THEN RAISE EXCEPTION 'История спецификаций неизменна'; END IF;
  PERFORM bom_validate_revision(NEW."Id");
 END IF;
 RETURN NEW;
END $$;
CREATE TRIGGER bom_revision_guard BEFORE INSERT OR UPDATE OR DELETE ON bom_revision FOR EACH ROW EXECUTE FUNCTION bom_revision_guard();
CREATE FUNCTION bom_revision_ready() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF NOT EXISTS(SELECT 1 FROM bom_revision WHERE "Id"=NEW."Id" AND "IsFinalized") THEN RAISE EXCEPTION 'Спецификация не завершена'; END IF;
 RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER bom_revision_ready AFTER INSERT ON bom_revision DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION bom_revision_ready();
CREATE FUNCTION bom_related_guard() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE rid int;
BEGIN
 rid:=CASE WHEN TG_OP='DELETE' THEN OLD."RevisionId" ELSE NEW."RevisionId" END;
 IF EXISTS(SELECT 1 FROM bom_revision WHERE "Id"=rid AND "IsFinalized") THEN RAISE EXCEPTION 'Сохранённая спецификация неизменна'; END IF;
 IF TG_OP='UPDATE' AND EXISTS(SELECT 1 FROM bom_revision WHERE "Id"=OLD."RevisionId" AND "IsFinalized") THEN RAISE EXCEPTION 'Сохранённая спецификация неизменна'; END IF;
 IF TG_OP='DELETE' THEN RETURN OLD; END IF;
 RETURN NEW;
END $$;
DO $$ DECLARE t text; BEGIN
 FOREACH t IN ARRAY ARRAY['bom_position','bom_parameter','bom_parameter_option','bom_position_binding','bom_rule'] LOOP
  EXECUTE format('CREATE TRIGGER bom_related_guard BEFORE INSERT OR UPDATE OR DELETE ON %I FOR EACH ROW EXECUTE FUNCTION bom_related_guard()',t);
 END LOOP;
END $$;
CREATE FUNCTION bom_order_line_guard() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
 IF NOT EXISTS(SELECT 1 FROM bom_revision WHERE "Id"=NEW."ConfiguredRevisionId" AND "IsConfigured" AND "IsFinalized") THEN RAISE EXCEPTION 'Заказ ссылается только на готовое исполнение'; END IF;
 RETURN NEW;
END $$;
CREATE TRIGGER bom_order_line_guard BEFORE INSERT OR UPDATE ON bom_order_line FOR EACH ROW EXECUTE FUNCTION bom_order_line_guard();
