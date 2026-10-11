BEGIN;
ALTER TABLE skolenza."Module" DISABLE TRIGGER "TR_Module_PageNameImmutable";
UPDATE skolenza."Module"
SET "PageName" = CASE "ModuleCode"
    WHEN 'INSTITUTION_DEPARTMENTS' THEN 'departments'
    WHEN 'INSTITUTION_STAFF_DESIGNATIONS' THEN 'staff-designations'
    WHEN 'INSTITUTION_ACCESS' THEN 'access-roles'
END,
    "UpdatedById" = 1,
    "UpdatedDateTime" = CURRENT_TIMESTAMP
WHERE "ModuleCode" IN (
    'INSTITUTION_DEPARTMENTS',
    'INSTITUTION_STAFF_DESIGNATIONS',
    'INSTITUTION_ACCESS');
ALTER TABLE skolenza."Module" ENABLE TRIGGER "TR_Module_PageNameImmutable";
COMMIT;
