SELECT conrelid::regclass AS table_name,
       conname,
       pg_get_constraintdef(oid) AS definition
FROM pg_constraint
WHERE connamespace = 'skolenza'::regnamespace
  AND conrelid IN (
      'skolenza."HostRoleModuleAndPermission"'::regclass,
      'skolenza."ModuleOperationMapping"'::regclass,
      'skolenza."PlanModuleMapping"'::regclass,
      'skolenza."RoleModuleAndPermission"'::regclass,
      'skolenza."InstitutionEnabledModule"'::regclass,
      'skolenza."InstitutionEnabledOperation"'::regclass)
  AND contype IN ('p', 'u')
ORDER BY 1, 2;

WITH ids AS (
    SELECT "Id", "ModuleCode"
    FROM skolenza."Module"
    WHERE "ModuleCode" IN (
        'DEPARTMENT', 'INSTITUTION_DEPARTMENTS',
        'STAFF_DESIGNATIONS', 'INSTITUTION_STAFF_DESIGNATIONS',
        'ACCESS_ROLES', 'INSTITUTION_ACCESS'))
SELECT i."ModuleCode",
       i."Id",
       (SELECT count(*) FROM skolenza."ModuleOperationMapping" x WHERE x."ModuleId" = i."Id") AS op_map,
       (SELECT count(*) FROM skolenza."PlanModuleMapping" x WHERE x."ModuleId" = i."Id") AS plan_map,
       (SELECT count(*) FROM skolenza."RoleModuleAndPermission" x WHERE x."ModuleId" = i."Id") AS role_map,
       (SELECT count(*) FROM skolenza."HostRoleModuleAndPermission" x WHERE x."ModuleId" = i."Id") AS host_role_map,
       (SELECT count(*) FROM skolenza."InstitutionEnabledModule" x WHERE x."ModuleId" = i."Id") AS enabled_module,
       (SELECT count(*) FROM skolenza."InstitutionEnabledOperation" x WHERE x."ModuleId" = i."Id") AS enabled_op
FROM ids i
ORDER BY i."ModuleCode";
