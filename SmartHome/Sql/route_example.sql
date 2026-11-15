-- Учебный маршрут комплекта умного дома. Требует подготовленный каталог и
-- фиксированное исполнение «Проводной — 2 датчика» из конструкторской спецификации.
-- Повторный запуск проверяет существующий пример, не создавая дубликаты.
BEGIN;
SELECT pg_advisory_xact_lock(734021, 1);
DO $$
DECLARE
    kit_id int; cable_id int; fastener_id int; revision_id int;
    kit_unit int; cable_unit int; fastener_unit int; parameter_id int;
    operation_id int; profession_id int; center_id int; qualification_id int;
    cable_step int; fastener_step int; first_step int; second_step int; final_step int;
    setting_count int; setting_one int; setting_two int; template_id int;
    material_cost numeric; labor_cost numeric; work_minutes numeric; quantity numeric;
BEGIN
    SELECT "Id" INTO STRICT kit_id FROM product WHERE "ShortName" = 'smart-home-kit';
    SELECT "Id" INTO STRICT cable_id FROM product WHERE "ShortName" = 'signal-cable';
    SELECT "Id" INTO STRICT fastener_id FROM product WHERE "ShortName" = 'mounting-fastener';
    SELECT "Id" INTO STRICT revision_id FROM bom_revision
    WHERE "ProductId" = kit_id AND "Name" = 'Проводной — 2 датчика'
      AND "IsConfigured" AND "IsFinalized";
    SELECT c."MeasureUnitId" INTO STRICT kit_unit FROM product p
    JOIN class_node c ON c."Id" = p."ClassNodeId" WHERE p."Id" = kit_id;
    SELECT c."MeasureUnitId" INTO STRICT cable_unit FROM product p
    JOIN class_node c ON c."Id" = p."ClassNodeId" WHERE p."Id" = cable_id;
    SELECT c."MeasureUnitId" INTO STRICT fastener_unit FROM product p
    JOIN class_node c ON c."Id" = p."ClassNodeId" WHERE p."Id" = fastener_id;
    IF EXISTS(SELECT 1 FROM product WHERE
       ("Id" = cable_id AND "PurchasePrice" IS NOT NULL AND "PurchasePrice" <> 40)
       OR ("Id" = fastener_id AND "PurchasePrice" IS NOT NULL AND "PurchasePrice" <> 12)) THEN
        RAISE EXCEPTION 'Цены материалов отличаются от учебного примера; изменения отменены.';
    END IF;
    UPDATE product SET "PurchasePrice" = 40 WHERE "Id" = cable_id AND "PurchasePrice" IS NULL;
    UPDATE product SET "PurchasePrice" = 12 WHERE "Id" = fastener_id AND "PurchasePrice" IS NULL;

    SELECT "Id" INTO parameter_id FROM parameter_definition WHERE "ShortName" = 'route_extra_cable';
    IF parameter_id IS NULL THEN
        INSERT INTO parameter_definition("Name", "ShortName", "ValueType", "MeasureUnitId", "EnumClassId")
        VALUES ('Длина дополнительного кабеля', 'route_extra_cable', 2, cable_unit, NULL)
        RETURNING "Id" INTO parameter_id;
    ELSIF NOT EXISTS(SELECT 1 FROM parameter_definition WHERE "Id" = parameter_id
      AND "ValueType" = 2 AND "MeasureUnitId" = cable_unit AND "EnumClassId" IS NULL) THEN
        RAISE EXCEPTION 'Определение параметра не соответствует учебному примеру.';
    END IF;
    SELECT "Id" INTO STRICT operation_id FROM class_node WHERE "ShortName" = 'route_assembly';
    SELECT "Id" INTO STRICT profession_id FROM class_node WHERE "ShortName" = 'route_assembler';
    SELECT w."Id" INTO STRICT center_id FROM work_center w JOIN department d ON d."Id" = w."DepartmentId"
    WHERE w."Name" = 'Монтажный пост' AND d."Name" = 'Сборочный участок';
    SELECT v."Id" INTO STRICT qualification_id FROM enum_value v
    JOIN enum_class c ON c."Id" = v."EnumClassId" WHERE c."ShortName" = 'route_qualification'
      AND coalesce(v."DisplayName", v."StringValue") = '3 разряд';
    SELECT count(*) INTO setting_count FROM tech_route_setting WHERE "ProductId" = kit_id
      AND "Name" IN ('Типовой монтаж комплекта', 'Проводной 1 м', 'Проводной 2 м');
    IF setting_count = 0 THEN
        INSERT INTO tech_step("Kind", "ProductId", "ResultName", "UnitId", "Number", "Tooling",
                              "SetupMinutes", "UnitMinutes", "HourlyRate")
        VALUES ('purchase', cable_id, 'Кабель', cable_unit, 0, '', 0, 0, 0) RETURNING "Id" INTO cable_step;
        INSERT INTO tech_step("Kind", "ProductId", "ResultName", "UnitId", "Number", "Tooling",
                              "SetupMinutes", "UnitMinutes", "HourlyRate")
        VALUES ('purchase', fastener_id, 'Крепёж', fastener_unit, 0, '', 0, 0, 0) RETURNING "Id" INTO fastener_step;
        INSERT INTO tech_step("Kind", "ProductId", "ResultName", "UnitId", "Number", "OperationId",
                              "WorkCenterId", "ProfessionId", "QualificationId", "Tooling",
                              "SetupMinutes", "UnitMinutes", "HourlyRate")
        VALUES ('production', kit_id, 'Подготовить линии', kit_unit, 10, operation_id,
                center_id, profession_id, qualification_id, 'Кусачки', 5, 3, 540) RETURNING "Id" INTO first_step;
        INSERT INTO tech_step("Kind", "ProductId", "ResultName", "UnitId", "Number", "OperationId",
                              "WorkCenterId", "ProfessionId", "QualificationId", "Tooling",
                              "SetupMinutes", "UnitMinutes", "HourlyRate")
        VALUES ('production', kit_id, 'Уложить и закрепить линии', kit_unit, 20, operation_id,
                center_id, profession_id, qualification_id, 'Тестер / отвёртка', 6, 8, 540) RETURNING "Id" INTO second_step;
        INSERT INTO tech_step("Kind", "ProductId", "ResultName", "UnitId", "Number", "OperationId",
                              "WorkCenterId", "ProfessionId", "QualificationId", "Tooling",
                              "SetupMinutes", "UnitMinutes", "HourlyRate")
        VALUES ('production', kit_id, 'Проверить и укомплектовать', kit_unit, 30, operation_id,
                center_id, profession_id, qualification_id, 'Тестер', 4, 4, 660) RETURNING "Id" INTO final_step;
        INSERT INTO tech_input("StepId", "SourceId", "Amount", "ForQuantity", "ParameterId")
        VALUES (first_step, cable_step, 4, 1, NULL), (second_step, first_step, 1, 1, NULL),
               (second_step, fastener_step, 2, 1, NULL), (final_step, second_step, 1, 1, NULL),
               (final_step, cable_step, 1, 1, parameter_id);
        INSERT INTO tech_route_setting("Name", "ProductId", "RevisionId", "FinalStepId", "Kind", "ParameterId", "ParameterValue")
        VALUES ('Типовой монтаж комплекта', kit_id, NULL, final_step, 'template', parameter_id, 1),
               ('Проводной 1 м', kit_id, revision_id, final_step, 'application', parameter_id, 1),
               ('Проводной 2 м', kit_id, revision_id, final_step, 'application', parameter_id, 2);
    ELSIF setting_count <> 3 THEN
        RAISE EXCEPTION 'Найден неполный или неоднозначный учебный пример; изменения отменены.';
    END IF;
    SELECT "Id", "FinalStepId" INTO STRICT template_id, final_step FROM tech_route_setting
    WHERE "ProductId" = kit_id AND "Name" = 'Типовой монтаж комплекта' AND "Kind" = 'template'
      AND "ParameterId" = parameter_id AND "ParameterValue" = 1 AND "RevisionId" IS NULL;
    SELECT "Id" INTO STRICT setting_one FROM tech_route_setting WHERE "ProductId" = kit_id
      AND "Name" = 'Проводной 1 м' AND "Kind" = 'application' AND "FinalStepId" = final_step
      AND "ParameterId" = parameter_id AND "ParameterValue" = 1 AND "RevisionId" = revision_id;
    SELECT "Id" INTO STRICT setting_two FROM tech_route_setting WHERE "ProductId" = kit_id
      AND "Name" = 'Проводной 2 м' AND "Kind" = 'application' AND "FinalStepId" = final_step
      AND "ParameterId" = parameter_id AND "ParameterValue" = 2 AND "RevisionId" = revision_id;
    FOREACH quantity IN ARRAY ARRAY[1, 10] LOOP
        SELECT sum(cost) INTO material_cost FROM sum_mr(kit_id, quantity, setting_one);
        SELECT sum(cost), sum(minutes) INTO labor_cost, work_minutes FROM tr_to(kit_id, quantity, setting_one);
        IF material_cost IS DISTINCT FROM (CASE quantity WHEN 1 THEN 224 ELSE 2240 END)
           OR labor_cost IS DISTINCT FROM (CASE quantity WHEN 1 THEN 286 ELSE 1573 END)
           OR work_minutes IS DISTINCT FROM (CASE quantity WHEN 1 THEN 30 ELSE 165 END) THEN
            RAISE EXCEPTION 'Контрольные затраты не совпали с учебным примером.';
        END IF;
    END LOOP;
    SELECT sum(cost) INTO material_cost FROM sum_mr(kit_id, 1, setting_two);
    SELECT sum(cost) INTO labor_cost FROM tr_to(kit_id, 1, setting_two);
    IF material_cost IS DISTINCT FROM 264 OR labor_cost IS DISTINCT FROM 286 THEN
        RAISE EXCEPTION 'Параметр 2 м не дал стоимость 550 рублей.';
    END IF;
END $$;
COMMIT;
