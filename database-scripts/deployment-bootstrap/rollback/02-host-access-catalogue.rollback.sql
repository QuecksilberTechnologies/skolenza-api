BEGIN;
DELETE FROM skolenza."HostRoleModuleAndPermission" WHERE "HostRoleId"=1;
DELETE FROM skolenza."ModuleOperationMapping" WHERE "ModuleId" IN (SELECT "Id" FROM skolenza."Module" WHERE "ModuleScope" IN (2,3));
DELETE FROM skolenza."Module" WHERE "ModuleScope" IN (2,3);
DELETE FROM skolenza."Operation" WHERE NOT EXISTS (SELECT 1 FROM skolenza."ModuleOperationMapping" m WHERE m."OperationId"=skolenza."Operation"."Id");
COMMIT;
