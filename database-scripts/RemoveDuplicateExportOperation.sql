BEGIN;

DO $cleanup$
DECLARE
    canonical_export_id integer;
    duplicate_export_id integer;
BEGIN
    SELECT "Id" INTO canonical_export_id
    FROM skolenza."Operation"
    WHERE lower(btrim("OperationName")) = 'export'
      AND "OperationType" = 11
    ORDER BY "Id"
    LIMIT 1;

    IF canonical_export_id IS NULL THEN
        RAISE EXCEPTION 'Canonical Export operation (OperationType 11) was not found.';
    END IF;

    FOR duplicate_export_id IN
        SELECT "Id"
        FROM skolenza."Operation"
        WHERE lower(btrim("OperationName")) = 'export'
          AND "Id" <> canonical_export_id
    LOOP
        INSERT INTO skolenza."ModuleOperationMapping"
        ("ModuleId","OperationId","PageURL","IconURL","IsCommonItem","IsOperational",
         "Priority","Remark","IsActive","AddedById","AddedDateTime","UpdatedById","UpdatedDateTime")
        SELECT legacy."ModuleId",canonical_export_id,legacy."PageURL",legacy."IconURL",
               legacy."IsCommonItem",legacy."IsOperational",legacy."Priority",
               'Migrated from duplicate Export operation.',legacy."IsActive",
               legacy."AddedById",legacy."AddedDateTime",1,CURRENT_TIMESTAMP
        FROM skolenza."ModuleOperationMapping" legacy
        WHERE legacy."OperationId"=duplicate_export_id
          AND NOT EXISTS (SELECT 1 FROM skolenza."ModuleOperationMapping" current
              WHERE current."ModuleId"=legacy."ModuleId"
                AND current."OperationId"=canonical_export_id);

        INSERT INTO skolenza."InstitutionEnabledOperation"
        ("InstitutionId","ModuleId","OperationId","IsOperationUsed","IsEnabled",
         "AddedById","AddedDateTime","UpdatedById","UpdatedDateTime")
        SELECT legacy."InstitutionId",legacy."ModuleId",canonical_export_id,
               legacy."IsOperationUsed",legacy."IsEnabled",legacy."AddedById",
               legacy."AddedDateTime",1,CURRENT_TIMESTAMP
        FROM skolenza."InstitutionEnabledOperation" legacy
        WHERE legacy."OperationId"=duplicate_export_id
          AND NOT EXISTS (SELECT 1 FROM skolenza."InstitutionEnabledOperation" current
              WHERE current."InstitutionId"=legacy."InstitutionId"
                AND current."ModuleId"=legacy."ModuleId"
                AND current."OperationId"=canonical_export_id);

        INSERT INTO skolenza."RoleModuleAndPermission"
        ("RoleId","ModuleId","OperationId","HasAccess","IsActive","Remark",
         "IsOperational","ImageIcon","AddedById","AddedDateTime","UpdatedById",
         "UpdateDateTime","SoftDeletedById","DeletedDateTime","IsSoftDeleted","UpdatedDateTime")
        SELECT legacy."RoleId",legacy."ModuleId",canonical_export_id,legacy."HasAccess",
               legacy."IsActive",'Migrated from duplicate Export operation.',
               legacy."IsOperational",legacy."ImageIcon",legacy."AddedById",
               legacy."AddedDateTime",1,CURRENT_TIMESTAMP,legacy."SoftDeletedById",
               legacy."DeletedDateTime",legacy."IsSoftDeleted",CURRENT_TIMESTAMP
        FROM skolenza."RoleModuleAndPermission" legacy
        WHERE legacy."OperationId"=duplicate_export_id
          AND NOT EXISTS (SELECT 1 FROM skolenza."RoleModuleAndPermission" current
              WHERE current."RoleId" IS NOT DISTINCT FROM legacy."RoleId"
                AND current."ModuleId"=legacy."ModuleId"
                AND current."OperationId"=canonical_export_id
                AND current."IsSoftDeleted"=legacy."IsSoftDeleted");

        DELETE FROM skolenza."HostRoleModuleAndPermission" WHERE "OperationId"=duplicate_export_id;
        DELETE FROM skolenza."RoleModuleAndPermission" WHERE "OperationId"=duplicate_export_id;
        DELETE FROM skolenza."InstitutionEnabledOperation" WHERE "OperationId"=duplicate_export_id;
        DELETE FROM skolenza."ModuleOperationMapping" WHERE "OperationId"=duplicate_export_id;
        DELETE FROM skolenza."Operation" WHERE "Id"=duplicate_export_id;
    END LOOP;

    IF (SELECT count(*) FROM skolenza."Operation" WHERE lower(btrim("OperationName"))='export') <> 1 THEN
        RAISE EXCEPTION 'Export operation cleanup failed.';
    END IF;
END
$cleanup$;

COMMIT;
