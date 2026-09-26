\set ON_ERROR_STOP on
BEGIN;
CREATE FUNCTION pg_temp.reject(statement text) RETURNS void LANGUAGE plpgsql AS $$
BEGIN
 BEGIN EXECUTE statement;
 EXCEPTION WHEN raise_exception OR check_violation OR foreign_key_violation OR unique_violation OR string_data_right_truncation THEN RETURN;
 END;
 RAISE EXCEPTION 'Unexpected acceptance: %',statement USING ERRCODE='XX000';
END $$;
DO $$
DECLARE root integer; leaf integer; sibling integer; other integer; def integer; cp integer; target integer; item integer; enumid integer; ev integer; second integer; v integer; k integer; d integer; binding integer;
BEGIN
 root:=add_class_node('SQL root','sql-root');
 leaf:=add_class_node('SQL leaf','sql-leaf',root,true);
 sibling:=add_class_node('SQL sibling','sql-sibling',root,true);
 other:=add_class_node('SQL other','sql-other',NULL,true);
 def:=describe_parameter('SQL number','sql-number',2);
 cp:=bind_class_parameter(root,def,NULL,1,true,1,10);
 SELECT "Id" INTO target FROM class_parameter WHERE "ClassNodeId"=leaf AND "ParameterDefinitionId"=def;
 item:=add_product('SQL item','sql-item',leaf);
 PERFORM pg_temp.reject(format('SELECT complete_product(%s)',item));
 PERFORM write_product_parameter(item,target,NULL,9);
 PERFORM complete_product(item);
 PERFORM pg_temp.reject(format('DELETE FROM product_parameter_value WHERE "ProductId"=%s',item));
 PERFORM pg_temp.reject(format('SELECT write_product_parameter(%s,%s,NULL,999)',item,target));
 PERFORM pg_temp.reject(format('SELECT write_product_parameter(%s,%s,NULL,9,''mixed'')',item,target));
 PERFORM pg_temp.reject(format('SELECT update_class_parameter(%s,NULL,1,true,1,8)',target));
 PERFORM pg_temp.reject(format('SELECT move_product(%s,%s)',item,other));
 IF (SELECT "ClassNodeId" FROM product WHERE "Id"=item)<>leaf THEN RAISE EXCEPTION 'Rejected move changed product'; END IF;
 PERFORM move_product(item,sibling);
 IF EXISTS(SELECT 1 FROM product_parameter_value v JOIN class_parameter c ON c."Id"=v."ClassParameterId" WHERE v."ProductId"=item AND c."ClassNodeId"<>sibling) THEN RAISE EXCEPTION 'Move lost parameter mapping'; END IF;
 PERFORM move_product(item,leaf);
 IF (SELECT count(*) FROM filter_products(root,cp,true,NULL,8,10))<>1 THEN RAISE EXCEPTION 'Subtree SQL search failed'; END IF;
 PERFORM pg_temp.reject(format('SELECT filter_products(%s,%s)',other,cp));
 PERFORM pg_temp.reject(format('SELECT add_class_node(''bad child'',''bad-child'',%s)',leaf));
 PERFORM pg_temp.reject(format('SELECT move_class_node(%s,%s)',root,leaf));
 PERFORM pg_temp.reject(format('SELECT delete_class_node(%s)',leaf));
 PERFORM pg_temp.reject('SELECT describe_parameter(''bad type'',''bad-type'',99)');
 enumid:=add_enum_class('SQL enum','sql-enum',1);
 ev:=add_enum_value(enumid,'Alpha',NULL,NULL,'Alpha',1);
 second:=add_enum_value(enumid,'Beta',NULL,NULL,'Beta',2);
 PERFORM update_enum_value_sort_order(ev,2);
 IF (SELECT "SortOrder" FROM enum_value WHERE "Id"=second)<>1 THEN RAISE EXCEPTION 'Order swap failed'; END IF;
 PERFORM pg_temp.reject(format('SELECT add_enum_value(%s,NULL,12,NULL,NULL,3)',enumid));
 PERFORM pg_temp.reject(format('SELECT add_enum_value(%s,'' '',NULL,NULL,NULL,3)',enumid));
 PERFORM pg_temp.reject(format('SELECT add_enum_value(%s,''Alpha'',NULL,NULL,NULL,3)',enumid));
 IF is_enum_value_allowed(enumid,'Alpha',9,NULL) THEN RAISE EXCEPTION 'Mixed enum check accepted'; END IF;
 IF NOT is_enum_value_allowed(enumid,'Alpha',NULL,NULL) THEN RAISE EXCEPTION 'Valid enum check failed'; END IF;
 UPDATE product SET "IsDraft"=true WHERE "Id"=item;
 FOR k IN 1..5 LOOP
   d:=describe_parameter('SQL type '||k,'sql-type-'||k,k,NULL,CASE WHEN k=5 THEN enumid END);
   binding:=bind_class_parameter(root,d,NULL,k+1,false);
   SELECT "Id" INTO target FROM class_parameter WHERE "ClassNodeId"=leaf AND "ParameterDefinitionId"=d;
   v:=write_product_parameter(item,target,CASE WHEN k=1 THEN 9 END,CASE WHEN k=2 THEN 9 END,CASE WHEN k=3 THEN 'Test' END,CASE WHEN k=4 THEN '2026-01-01T00:00:00Z'::timestamptz END,CASE WHEN k=5 THEN ev END);
   IF (SELECT count(*) FROM filter_products(root,binding,true,CASE WHEN k=1 THEN 9 END,CASE WHEN k=2 THEN 8 END,CASE WHEN k=2 THEN 10 END,CASE WHEN k=3 THEN 'test' END,CASE WHEN k=4 THEN '2025-01-01T00:00:00Z'::timestamptz END,NULL,CASE WHEN k=5 THEN ev END))<>1 THEN RAISE EXCEPTION 'SQL type % search failed',k; END IF;
 END LOOP;
 PERFORM pg_temp.reject(format('SELECT delete_enum_value(%s)',ev));
 PERFORM pg_temp.reject(format('SELECT delete_enum_class(%s)',enumid));
 IF jsonb_array_length(get_product_parameters(item))<>6 THEN RAISE EXCEPTION 'Card missing parameters'; END IF;
 IF NOT group_product_parameters(item) ? 'Общие' THEN RAISE EXCEPTION 'Grouping failed'; END IF;
 PERFORM complete_product(item);
 PERFORM delete_product(item);
END $$;
ROLLBACK;
