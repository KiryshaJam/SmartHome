-- Only a disposable database containing the routes_api.py fixture.
BEGIN;
DO $$ DECLARE pid int; rid int; s int; missing numeric; expected numeric; got numeric; n numeric; c record;
BEGIN
 SELECT "Id" INTO STRICT pid FROM product WHERE "Name"='Комплект умного дома';
 SELECT "Id","FinalStepId" INTO STRICT rid,s FROM tech_route_setting WHERE "Name"='Проводной 1 м';
 FOREACH n IN ARRAY ARRAY[1,10] LOOP
  SELECT sum(cost) INTO got FROM sum_mr(pid,n,rid);
  expected := CASE n WHEN 1 THEN 224 ELSE 2240 END;
  IF got<>expected THEN RAISE EXCEPTION 'SUM_MR %, expected %',got,expected; END IF;
  SELECT sum(cost) INTO got FROM tr_to(pid,n,rid);
  expected := CASE n WHEN 1 THEN 286 ELSE 1573 END;
  IF got<>expected THEN RAISE EXCEPTION 'TR_TO %, expected %',got,expected; END IF;
 END LOOP;
 -- Deferred guards must also reject direct SQL writes, independently of API.
 BEGIN
  INSERT INTO tech_input("StepId","SourceId","Amount","ForQuantity")
  SELECT min("Id"),s,1,1 FROM tech_step WHERE "ResultName"='Подготовить линии';
  SET CONSTRAINTS ALL IMMEDIATE;
  RAISE EXCEPTION 'Cycle accepted' USING ERRCODE='XX000';
 EXCEPTION WHEN raise_exception THEN NULL; END;
 BEGIN
  UPDATE tech_route_setting SET "ParameterValue"=NULL WHERE "Id"=rid;
  SET CONSTRAINTS ALL IMMEDIATE;
  RAISE EXCEPTION 'Missing parameter accepted' USING ERRCODE='XX000';
 EXCEPTION WHEN check_violation THEN NULL; END;
 BEGIN
  UPDATE class_node SET "MeasureUnitId"=(SELECT "Id" FROM measure_unit WHERE "ShortName"='шт.') WHERE "Id"=(SELECT "ClassNodeId" FROM product WHERE "Name"='Кабель сигнальный');
  RAISE EXCEPTION 'Priced unit changed' USING ERRCODE='XX000';
 EXCEPTION WHEN raise_exception THEN NULL; END;
 -- Three-argument SQL creation remains available; the fourth argument carries price.
 SELECT add_product('SQL price fixture','sql_route_price_fixture',(SELECT "ClassNodeId" FROM product WHERE "Name"='Кабель сигнальный'),0) INTO s;
 IF (SELECT "PurchasePrice" FROM product WHERE "Id"=s) IS DISTINCT FROM 0 THEN RAISE EXCEPTION 'Zero price lost'; END IF;
 PERFORM move_product(s,(SELECT "ClassNodeId" FROM product WHERE "Name"='Кабель усиленный'));
 IF (SELECT "PurchasePrice" FROM product WHERE "Id"=s) IS DISTINCT FROM 0 THEN RAISE EXCEPTION 'Legacy move lost price'; END IF;
 PERFORM set_product_price(s,NULL);
 IF (SELECT "PurchasePrice" FROM product WHERE "Id"=s) IS NOT NULL THEN RAISE EXCEPTION 'Clear price failed'; END IF;
 RAISE NOTICE 'Direct SQL: control totals, cycle, missing parameter, unit and legacy price functions passed';
END $$;
ROLLBACK;
