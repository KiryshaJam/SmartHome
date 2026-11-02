-- Classifier operations (PR 1)
CREATE OR REPLACE FUNCTION add_class_node(p_name text,p_short text,p_parent integer DEFAULT NULL,p_terminal boolean DEFAULT false,p_order integer DEFAULT 0,p_unit integer DEFAULT NULL)
RETURNS integer LANGUAGE sql AS $$ INSERT INTO class_node("Name","ShortName","ParentId","IsTerminal","SortOrder","MeasureUnitId") VALUES(p_name,p_short,p_parent,p_terminal,p_order,p_unit) RETURNING "Id" $$;
CREATE OR REPLACE FUNCTION delete_class_node(p_id integer) RETURNS boolean LANGUAGE plpgsql AS $$ BEGIN
 IF EXISTS(SELECT 1 FROM class_node WHERE "ParentId"=p_id) OR EXISTS(SELECT 1 FROM product WHERE "ClassNodeId"=p_id) THEN RAISE EXCEPTION 'Нельзя удалить используемый класс.'; END IF;
 DELETE FROM class_node WHERE "Id"=p_id; RETURN FOUND; END $$;
CREATE OR REPLACE FUNCTION add_product(p_name text,p_short text,p_class integer) RETURNS integer LANGUAGE sql AS $$ INSERT INTO product("Name","ShortName","ClassNodeId") VALUES(p_name,p_short,p_class) RETURNING "Id" $$;
CREATE OR REPLACE FUNCTION move_product(p_id integer,p_class integer) RETURNS boolean LANGUAGE plpgsql AS $$ BEGIN UPDATE product SET "ClassNodeId"=p_class WHERE "Id"=p_id; RETURN FOUND; END $$;
CREATE OR REPLACE FUNCTION delete_product(p_id integer) RETURNS boolean LANGUAGE plpgsql AS $$ BEGIN DELETE FROM product WHERE "Id"=p_id; RETURN FOUND; END $$;
CREATE OR REPLACE FUNCTION get_products_by_class(p_class integer,p_descendants boolean DEFAULT true) RETURNS SETOF product LANGUAGE sql AS $$ SELECT p.* FROM product p WHERE p."ClassNodeId"=p_class OR (p_descendants AND p."ClassNodeId" IN (SELECT "Id" FROM find_children(p_class))) ORDER BY p."Name" $$;

-- Enumeration operations (PR 2). Value validation is shared with API through triggers.
CREATE OR REPLACE FUNCTION add_enum_class(p_name text,p_short text,p_type integer,p_order integer DEFAULT 0,p_unit integer DEFAULT NULL) RETURNS integer LANGUAGE sql AS $$ INSERT INTO enum_class("Name","ShortName","ValueType","SortOrder","MeasureUnitId") VALUES(p_name,p_short,p_type,p_order,p_unit) RETURNING "Id" $$;
CREATE OR REPLACE FUNCTION delete_enum_class(p_id integer) RETURNS boolean LANGUAGE plpgsql AS $$ BEGIN DELETE FROM enum_class WHERE "Id"=p_id; RETURN FOUND; END $$;
CREATE OR REPLACE FUNCTION delete_enum_value(p_id integer) RETURNS boolean LANGUAGE plpgsql AS $$ BEGIN DELETE FROM enum_value WHERE "Id"=p_id; RETURN FOUND; END $$;
CREATE OR REPLACE FUNCTION update_enum_class_sort_order(p_id integer,p_order integer) RETURNS boolean LANGUAGE plpgsql AS $$ BEGIN UPDATE enum_class SET "SortOrder"=p_order WHERE "Id"=p_id; RETURN FOUND; END $$;
CREATE OR REPLACE FUNCTION update_enum_value_sort_order(p_enum_value_id integer,p_sort_order integer) RETURNS boolean LANGUAGE plpgsql AS $$
DECLARE source enum_value; other integer; temporary integer := -2147483648;
BEGIN
 PERFORM pg_advisory_xact_lock(734021,1);
 SELECT * INTO source FROM enum_value WHERE "Id"=p_enum_value_id;
 IF NOT FOUND THEN RAISE EXCEPTION 'Значение перечисления не найдено.'; END IF;
 IF source."SortOrder"=p_sort_order THEN RETURN true; END IF;
 SELECT "Id" INTO other FROM enum_value WHERE "EnumClassId"=source."EnumClassId" AND "SortOrder"=p_sort_order;
 IF other IS NOT NULL THEN
   WHILE EXISTS(SELECT 1 FROM enum_value WHERE "EnumClassId"=source."EnumClassId" AND "SortOrder"=temporary) LOOP temporary:=temporary+1; END LOOP;
   UPDATE enum_value SET "SortOrder"=temporary WHERE "Id"=other;
 END IF;
 UPDATE enum_value SET "SortOrder"=p_sort_order WHERE "Id"=p_enum_value_id;
 IF other IS NOT NULL THEN UPDATE enum_value SET "SortOrder"=source."SortOrder" WHERE "Id"=other; END IF;
 RETURN true;
END $$;
CREATE OR REPLACE FUNCTION is_enum_value_allowed(p_enum_class_id integer,p_string_value varchar,p_number_value numeric,p_icon_value varchar) RETURNS boolean LANGUAGE sql AS $$
 SELECT num_nonnulls(p_string_value,p_number_value,p_icon_value)=1 AND EXISTS(
 SELECT 1 FROM enum_value ev JOIN enum_class ec ON ec."Id"=ev."EnumClassId" WHERE ec."Id"=p_enum_class_id AND
 CASE ec."ValueType" WHEN 1 THEN ev."StringValue"=p_string_value WHEN 2 THEN ev."NumberValue"=p_number_value WHEN 3 THEN ev."IconValue"=p_icon_value ELSE false END) $$;

-- Parameter descriptions, bindings, values, selection and grouping (PR 3).
CREATE OR REPLACE FUNCTION describe_parameter(p_name text,p_short text,p_type integer,p_unit integer DEFAULT NULL,p_enum integer DEFAULT NULL) RETURNS integer LANGUAGE sql AS $$
 INSERT INTO parameter_definition("Name","ShortName","ValueType","MeasureUnitId","EnumClassId") VALUES(p_name,p_short,p_type,p_unit,p_enum) RETURNING "Id" $$;
CREATE OR REPLACE FUNCTION bind_class_parameter(p_class integer,p_definition integer,p_group integer DEFAULT NULL,p_order integer DEFAULT 0,p_required boolean DEFAULT false,p_min numeric DEFAULT NULL,p_max numeric DEFAULT NULL) RETURNS integer LANGUAGE sql AS $$
 INSERT INTO class_parameter("ClassNodeId","ParameterDefinitionId","ParameterGroupId","SortOrder","IsRequired","IsInherited","MinNumberValue","MaxNumberValue") VALUES(p_class,p_definition,p_group,p_order,p_required,false,p_min,p_max) RETURNING "Id" $$;
CREATE OR REPLACE FUNCTION update_class_parameter(p_id integer,p_group integer,p_order integer,p_required boolean,p_min numeric,p_max numeric) RETURNS boolean LANGUAGE plpgsql AS $$ BEGIN
 UPDATE class_parameter SET "ParameterGroupId"=p_group,"SortOrder"=p_order,"IsRequired"=p_required,"MinNumberValue"=p_min,"MaxNumberValue"=p_max WHERE "Id"=p_id; RETURN FOUND; END $$;
CREATE OR REPLACE FUNCTION get_class_parameters(p_class integer) RETURNS SETOF class_parameter LANGUAGE sql AS $$ SELECT * FROM class_parameter WHERE "ClassNodeId"=p_class ORDER BY "SortOrder","Id" $$;
CREATE OR REPLACE FUNCTION write_product_parameter(p_product integer,p_parameter integer,p_integer integer DEFAULT NULL,p_number numeric DEFAULT NULL,p_string text DEFAULT NULL,p_date timestamptz DEFAULT NULL,p_enum integer DEFAULT NULL) RETURNS integer LANGUAGE sql AS $$
 INSERT INTO product_parameter_value("ProductId","ClassParameterId","IntegerValue","NumberValue","StringValue","DateTimeValue","EnumValueId") VALUES(p_product,p_parameter,p_integer,p_number,p_string,p_date,p_enum)
 ON CONFLICT("ProductId","ClassParameterId") DO UPDATE SET "IntegerValue"=EXCLUDED."IntegerValue","NumberValue"=EXCLUDED."NumberValue","StringValue"=EXCLUDED."StringValue","DateTimeValue"=EXCLUDED."DateTimeValue","EnumValueId"=EXCLUDED."EnumValueId" RETURNING "Id" $$;
CREATE OR REPLACE FUNCTION get_product_parameters(p_product integer) RETURNS jsonb LANGUAGE sql AS $$
 SELECT coalesce(jsonb_agg(jsonb_build_object('parameter',to_jsonb(cp),'definition',to_jsonb(pd),'value',to_jsonb(v),'group',g."Name") ORDER BY g."SortOrder",cp."SortOrder",cp."Id"),'[]'::jsonb)
 FROM product p JOIN class_parameter cp ON cp."ClassNodeId"=p."ClassNodeId" JOIN parameter_definition pd ON pd."Id"=cp."ParameterDefinitionId"
 LEFT JOIN parameter_group g ON g."Id"=cp."ParameterGroupId" LEFT JOIN product_parameter_value v ON v."ProductId"=p."Id" AND v."ClassParameterId"=cp."Id" WHERE p."Id"=p_product $$;
CREATE OR REPLACE FUNCTION group_product_parameters(p_product integer) RETURNS jsonb LANGUAGE sql AS $$
 SELECT coalesce(jsonb_object_agg(group_name,items),'{}'::jsonb) FROM (
 SELECT coalesce(item->>'group','Общие') group_name,jsonb_agg(item) items FROM jsonb_array_elements(get_product_parameters(p_product)) item GROUP BY coalesce(item->>'group','Общие')) groups $$;
CREATE OR REPLACE FUNCTION filter_products(p_class integer,p_parameter integer DEFAULT NULL,p_descendants boolean DEFAULT true,p_integer integer DEFAULT NULL,p_min numeric DEFAULT NULL,p_max numeric DEFAULT NULL,p_text text DEFAULT NULL,p_from timestamptz DEFAULT NULL,p_to timestamptz DEFAULT NULL,p_enum integer DEFAULT NULL)
RETURNS SETOF product LANGUAGE plpgsql AS $$
DECLARE cp class_parameter; kind integer; ids integer[];
BEGIN
 SELECT array_agg("Id") INTO ids FROM find_children(p_class) WHERE p_descendants OR "Id"=p_class;
 IF ids IS NULL THEN RAISE EXCEPTION 'Класс не найден.'; END IF;
 IF p_parameter IS NULL THEN RETURN QUERY SELECT * FROM product WHERE "ClassNodeId"=ANY(ids) ORDER BY "Name"; RETURN; END IF;
 SELECT * INTO cp FROM class_parameter WHERE "Id"=p_parameter;
 IF NOT FOUND OR NOT cp."ClassNodeId"=ANY(ids) THEN RAISE EXCEPTION 'Параметр не принадлежит выбранной ветви.'; END IF;
 SELECT "ValueType" INTO kind FROM parameter_definition WHERE "Id"=cp."ParameterDefinitionId";
 IF p_min>p_max OR p_from>p_to OR NOT (CASE kind WHEN 1 THEN p_integer IS NOT NULL WHEN 2 THEN p_min IS NOT NULL OR p_max IS NOT NULL WHEN 3 THEN length(btrim(p_text))>0 WHEN 4 THEN p_from IS NOT NULL OR p_to IS NOT NULL WHEN 5 THEN p_enum IS NOT NULL ELSE false END) THEN RAISE EXCEPTION 'Неверные условия отбора.'; END IF;
 RETURN QUERY SELECT p.* FROM product p WHERE p."ClassNodeId"=ANY(ids) AND EXISTS(
 SELECT 1 FROM product_parameter_value v JOIN class_parameter b ON b."Id"=v."ClassParameterId" WHERE v."ProductId"=p."Id" AND b."ClassNodeId"=p."ClassNodeId" AND b."ParameterDefinitionId"=cp."ParameterDefinitionId" AND CASE kind
 WHEN 1 THEN v."IntegerValue"=p_integer
 WHEN 2 THEN (p_min IS NULL OR v."NumberValue">=p_min) AND (p_max IS NULL OR v."NumberValue"<=p_max)
 WHEN 3 THEN position(lower(p_text) in lower(v."StringValue"))>0
 WHEN 4 THEN (p_from IS NULL OR v."DateTimeValue">=p_from) AND (p_to IS NULL OR v."DateTimeValue"<=p_to)
 WHEN 5 THEN v."EnumValueId"=p_enum ELSE false END) ORDER BY p."Name";
END $$;

CREATE OR REPLACE FUNCTION copy_parameter_to_descendants(p_class integer,p_parameter integer) RETURNS integer LANGUAGE plpgsql AS $$
DECLARE affected integer;
BEGIN
 INSERT INTO class_parameter("ClassNodeId","ParameterDefinitionId","ParameterGroupId","SortOrder","IsRequired","IsInherited","MinNumberValue","MaxNumberValue")
 SELECT c."Id",p."ParameterDefinitionId",p."ParameterGroupId",p."SortOrder",p."IsRequired",true,p."MinNumberValue",p."MaxNumberValue"
 FROM find_children(p_class) c CROSS JOIN class_parameter p
 WHERE c."Id"<>p_class AND p."Id"=p_parameter AND p."ClassNodeId"=p_class
 ON CONFLICT("ClassNodeId","ParameterDefinitionId") DO NOTHING;
 GET DIAGNOSTICS affected=ROW_COUNT;
 RETURN affected;
END $$;

CREATE OR REPLACE FUNCTION add_parameter_group(p_name text,p_short text,p_order integer DEFAULT 0) RETURNS integer LANGUAGE sql AS $$ INSERT INTO parameter_group("Name","ShortName","SortOrder") VALUES(p_name,p_short,p_order) RETURNING "Id" $$;
CREATE OR REPLACE FUNCTION get_parameter_definitions() RETURNS SETOF parameter_definition LANGUAGE sql AS $$ SELECT * FROM parameter_definition ORDER BY "Name" $$;
CREATE OR REPLACE FUNCTION delete_parameter_definition(p_id integer) RETURNS boolean LANGUAGE plpgsql AS $$ BEGIN DELETE FROM parameter_definition WHERE "Id"=p_id; RETURN FOUND; END $$;
CREATE OR REPLACE FUNCTION delete_class_parameter(p_class integer,p_parameter integer) RETURNS boolean LANGUAGE plpgsql AS $$
DECLARE source class_parameter;
BEGIN
 PERFORM pg_advisory_xact_lock(734021,1);
 SELECT * INTO source FROM class_parameter WHERE "ClassNodeId"=p_class AND "Id"=p_parameter;
 IF NOT FOUND THEN RETURN false; END IF;
 IF source."IsInherited" THEN RAISE EXCEPTION 'Удалите параметр в исходном родительском классе.'; END IF;
 DELETE FROM class_parameter WHERE "ParameterDefinitionId"=source."ParameterDefinitionId" AND "IsInherited" AND "ClassNodeId" IN(SELECT "Id" FROM find_children(p_class) WHERE "Id"<>p_class);
 DELETE FROM class_parameter WHERE "Id"=p_parameter;
 RETURN true;
END $$;
