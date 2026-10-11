WITH RECURSIVE tree AS (
    SELECT "Id", "ModuleCode", "ParentModuleId", 1 AS depth
    FROM skolenza."Module"
    WHERE "ParentModuleId" IS NULL
    UNION ALL
    SELECT module."Id", module."ModuleCode", module."ParentModuleId", tree.depth + 1
    FROM skolenza."Module" module
    JOIN tree ON tree."Id" = module."ParentModuleId")
SELECT depth, count(*) FROM tree GROUP BY depth ORDER BY depth;

SELECT module."ModuleCode",
       parent."ModuleCode" AS parent_code,
       module."URLPath",
       module."PageName",
       count(mapping."Id") AS operation_count
FROM skolenza."Module" module
LEFT JOIN skolenza."Module" parent ON parent."Id" = module."ParentModuleId"
LEFT JOIN skolenza."ModuleOperationMapping" mapping ON mapping."ModuleId" = module."Id"
WHERE module."ModuleCode" IN (
    'INSTITUTION_DEPARTMENTS', 'INSTITUTION_STAFF_DESIGNATIONS', 'INSTITUTION_ACCESS',
    'DEPARTMENT', 'STAFF_DESIGNATIONS', 'ACCESS_ROLES')
GROUP BY module."ModuleCode", parent."ModuleCode", module."URLPath", module."PageName"
ORDER BY module."ModuleCode";

DO $$
DECLARE parent_id integer;
DECLARE child_id integer;
BEGIN
    INSERT INTO skolenza."Module"
        ("ModuleCode", "ModuleName", "DisplayName", "ParentModuleId", "IsLeafNode",
         "IsModuleDisplayInUI", "IsCommonMenu", "IsActive", "ModuleScope", "PageName")
    VALUES
        ('TEST_TWO_LEVEL_PARENT', 'Test Parent', 'Test Parent', NULL, false,
         false, false, true, 2, 'test-two-level-parent')
    RETURNING "Id" INTO parent_id;

    INSERT INTO skolenza."Module"
        ("ModuleCode", "ModuleName", "DisplayName", "ParentModuleId", "IsLeafNode",
         "IsModuleDisplayInUI", "IsCommonMenu", "IsActive", "ModuleScope", "PageName")
    VALUES
        ('TEST_TWO_LEVEL_CHILD', 'Test Child', 'Test Child', parent_id, true,
         false, false, true, 2, 'test-two-level-child')
    RETURNING "Id" INTO child_id;

    BEGIN
        INSERT INTO skolenza."Module"
            ("ModuleCode", "ModuleName", "DisplayName", "ParentModuleId", "IsLeafNode",
             "IsModuleDisplayInUI", "IsCommonMenu", "IsActive", "ModuleScope", "PageName")
        VALUES ('TEST_TWO_LEVEL_GRANDCHILD', 'Test Grandchild', 'Test Grandchild', child_id, true,
                false, false, true, 2, 'test-two-level-grandchild');
        RAISE EXCEPTION 'Two-level trigger did not reject a grandchild.';
    EXCEPTION WHEN raise_exception THEN
        IF SQLERRM = 'Two-level trigger did not reject a grandchild.' THEN
            RAISE;
        END IF;
    END;

    DELETE FROM skolenza."Module" WHERE "Id" = child_id;
    DELETE FROM skolenza."Module" WHERE "Id" = parent_id;
END $$;
