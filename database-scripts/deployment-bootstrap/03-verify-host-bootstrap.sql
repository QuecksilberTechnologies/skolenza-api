-- Read-only verification for the Skolenza platform bootstrap.
DO $$
DECLARE
    role_count integer;
    user_count integer;
    module_count integer;
    operation_count integer;
    mapping_count integer;
    permission_count integer;
BEGIN
    SELECT count(*) INTO role_count FROM skolenza."HostRole"
    WHERE "Id" = 1 AND "Name" = 'Host-Super-Admin' AND "IsActive" AND NOT "IsSoftDeleted";
    SELECT count(*) INTO user_count FROM skolenza."HostUser"
    WHERE lower("Email") IN ('mca.deepesh@gmail.com', 'sujeet@skolenza.com')
      AND "IsActive" AND NOT "IsSoftDeleted";
    SELECT count(*) INTO module_count FROM skolenza."Module"
    WHERE "ModuleScope" IN (2, 3) AND "IsActive";
    SELECT count(DISTINCT o."Id") INTO operation_count
    FROM skolenza."Operation" o
    JOIN skolenza."ModuleOperationMapping" mom ON mom."OperationId" = o."Id"
    JOIN skolenza."Module" m ON m."Id" = mom."ModuleId"
    WHERE m."ModuleScope" IN (2, 3) AND m."IsActive" AND mom."IsActive" AND o."IsActive";
    SELECT count(*) INTO mapping_count
    FROM skolenza."ModuleOperationMapping" mom
    JOIN skolenza."Module" m ON m."Id" = mom."ModuleId"
    WHERE m."ModuleScope" IN (2, 3) AND m."IsActive" AND mom."IsActive";
    SELECT count(*) INTO permission_count
    FROM skolenza."HostRoleModuleAndPermission" p
    JOIN skolenza."Module" m ON m."Id" = p."ModuleId"
    WHERE p."HostRoleId" = 1 AND p."IsActive" AND m."ModuleScope" = 2;

    IF (role_count, user_count, module_count, operation_count, mapping_count, permission_count)
       IS DISTINCT FROM (1, 2, 44, 12, 120, 110) THEN
        RAISE EXCEPTION 'Bootstrap reconciliation failed: role %, users %, modules %, operations %, mappings %, permissions %',
            role_count, user_count, module_count, operation_count, mapping_count, permission_count;
    END IF;
END $$;

SELECT m."ModuleCode", m."DisplayName", m."URLPath", count(mom."Id") AS "OperationCount"
FROM skolenza."Module" m
LEFT JOIN skolenza."ModuleOperationMapping" mom
  ON mom."ModuleId" = m."Id" AND mom."IsActive"
WHERE m."ModuleScope" IN (2, 3) AND m."IsActive"
GROUP BY m."Id"
ORDER BY m."ItemPriority", m."Id";
