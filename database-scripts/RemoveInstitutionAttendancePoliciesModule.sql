BEGIN;

DO $precondition$
BEGIN
    IF EXISTS
    (
        SELECT 1 FROM skolenza."Module"
        WHERE "ParentModuleId" = 67
    ) THEN
        RAISE EXCEPTION 'Module Id 67 has child modules and cannot be removed.';
    END IF;
END
$precondition$;

DELETE FROM skolenza."HostRoleModuleAndPermission" WHERE "ModuleId" = 67;
DELETE FROM skolenza."RoleModuleAndPermission" WHERE "ModuleId" = 67;
DELETE FROM skolenza."InstitutionEnabledOperation" WHERE "ModuleId" = 67;
DELETE FROM skolenza."InstitutionEnabledModule" WHERE "ModuleId" = 67;
DELETE FROM skolenza."PlanModuleMapping" WHERE "ModuleId" = 67;
DELETE FROM skolenza."ModuleOperationMapping" WHERE "ModuleId" = 67;
DELETE FROM skolenza."Module"
WHERE "Id" = 67
  AND "ModuleCode" = 'INSTITUTION_ATTENDANCE_POLICIES';

DO $verification$
BEGIN
    IF EXISTS
    (
        SELECT 1 FROM skolenza."Module"
        WHERE "Id" = 67 OR "ModuleCode" = 'INSTITUTION_ATTENDANCE_POLICIES'
    )
    OR EXISTS (SELECT 1 FROM skolenza."ModuleOperationMapping" WHERE "ModuleId" = 67)
    OR EXISTS (SELECT 1 FROM skolenza."InstitutionEnabledModule" WHERE "ModuleId" = 67)
    OR EXISTS (SELECT 1 FROM skolenza."InstitutionEnabledOperation" WHERE "ModuleId" = 67)
    OR EXISTS (SELECT 1 FROM skolenza."PlanModuleMapping" WHERE "ModuleId" = 67)
    OR EXISTS (SELECT 1 FROM skolenza."RoleModuleAndPermission" WHERE "ModuleId" = 67)
    OR EXISTS (SELECT 1 FROM skolenza."HostRoleModuleAndPermission" WHERE "ModuleId" = 67) THEN
        RAISE EXCEPTION 'Institution Attendance Policies Module Id 67 cleanup verification failed.';
    END IF;
END
$verification$;

COMMIT;
