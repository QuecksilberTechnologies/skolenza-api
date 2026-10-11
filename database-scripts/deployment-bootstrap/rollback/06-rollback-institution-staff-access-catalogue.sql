\set ON_ERROR_STOP on
BEGIN;

CREATE TEMP TABLE tmp_migrated_module_ids ("Id" integer PRIMARY KEY) ON COMMIT DROP;
INSERT INTO tmp_migrated_module_ids
SELECT "Id"
FROM skolenza."Module"
WHERE "Remark" LIKE 'Skolenza migration of %'
   OR "ModuleCode" IN ('STAFF_CATEGORIES', 'STAFF_FUNCTIONS');

DELETE FROM skolenza."ModuleOperationMapping"
WHERE "ModuleId" IN (SELECT "Id" FROM tmp_migrated_module_ids);

UPDATE skolenza."Module"
SET "ParentModuleId" = NULL
WHERE "Id" IN (SELECT "Id" FROM tmp_migrated_module_ids);

DELETE FROM skolenza."Module"
WHERE "Id" IN (SELECT "Id" FROM tmp_migrated_module_ids);

DELETE FROM skolenza."InstitutionType"
WHERE "Remark" LIKE 'InstitutionType code: %';

COMMIT;
