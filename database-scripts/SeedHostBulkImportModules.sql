-- Host bulk permissions use Import/Export on existing functional modules.
-- Separate *_BULK Module rows are obsolete and removed in FK-safe order.
-- Idempotent: safe to execute repeatedly after the canonical module seed.
BEGIN;

LOCK TABLE skolenza."Module", skolenza."Operation",
    skolenza."ModuleOperationMapping", skolenza."HostRoleModuleAndPermission",
    skolenza."RoleModuleAndPermission", skolenza."InstitutionEnabledOperation",
    skolenza."InstitutionEnabledModule", skolenza."PlanModuleMapping"
    IN SHARE ROW EXCLUSIVE MODE;

DO $host_bulk_operations$
DECLARE
    definition record;
    operation_type integer;
    operation_id integer;
BEGIN
    -- Preserve existing Host grants on the corresponding functional module.
    INSERT INTO skolenza."HostRoleModuleAndPermission"
        ("HostRoleId","ModuleId","OperationId","IsActive","IsSoftDeleted",
         "AddedById","AddedDateTime","UpdatedById","UpdatedDateTime")
    SELECT grant_row."HostRoleId",target."Id",grant_row."OperationId",
           grant_row."IsActive",grant_row."IsSoftDeleted",grant_row."AddedById",
           grant_row."AddedDateTime",grant_row."UpdatedById",grant_row."UpdatedDateTime"
    FROM (VALUES
        ('HOST_CARD_BULK','HOST_INSTITUTION_RFID_MANAGEMENT'),
        ('HOST_DEVICE_BULK','HOST_DEVICE_SETUP'),
        ('HOST_MODULE_CATALOGUE_BULK','HOST_MODULES'),
        ('HOST_SUBMODULE_CATALOGUE_BULK','HOST_SUBMODULES'),
        ('HOST_OPERATION_CATALOGUE_BULK','HOST_OPERATIONS'),
        ('HOST_MODULE_OPERATION_CATALOGUE_BULK','HOST_MODULE_OPERATIONS')
    ) AS move(child_code,target_code)
    JOIN skolenza."Module" child
      ON child."ModuleCode"=move.child_code AND child."ModuleScope"=2
    JOIN skolenza."Module" target
      ON target."ModuleCode"=move.target_code AND target."ModuleScope"=2
    JOIN skolenza."HostRoleModuleAndPermission" grant_row
      ON grant_row."ModuleId"=child."Id"
    WHERE NOT EXISTS (
        SELECT 1 FROM skolenza."HostRoleModuleAndPermission" existing
        WHERE existing."HostRoleId"=grant_row."HostRoleId"
          AND existing."ModuleId"=target."Id"
          AND existing."OperationId"=grant_row."OperationId"
          AND existing."IsSoftDeleted"=grant_row."IsSoftDeleted");

    FOREACH operation_type IN ARRAY ARRAY[11,12]
    LOOP
        SELECT "Id" INTO operation_id FROM skolenza."Operation"
        WHERE "OperationType"=operation_type
        ORDER BY "IsActive" DESC,"Id" LIMIT 1;

        IF operation_id IS NULL THEN
            INSERT INTO skolenza."Operation"
                ("OperationName","OperationType","Remark","IsActive","IconImage",
                 "AddedById","AddedDateTime")
            VALUES (CASE operation_type WHEN 11 THEN 'Export' ELSE 'Import' END,
                operation_type,'Bulk spreadsheet action on an existing functional module.',
                TRUE,CASE operation_type WHEN 11 THEN 'download' ELSE 'upload' END,
                1,CURRENT_TIMESTAMP)
            RETURNING "Id" INTO operation_id;
        END IF;

        FOR definition IN SELECT * FROM (VALUES
            ('HOST_INSTITUTION_RFID_MANAGEMENT','/app/institution-card-inventory'),
            ('HOST_DEVICE_SETUP','/app/device-masters'),
            ('HOST_MODULES','/app/modules'),
            ('HOST_SUBMODULES','/app/modules/submodules'),
            ('HOST_OPERATIONS','/app/modules/operations'),
            ('HOST_MODULE_OPERATIONS','/app/modules/module-operations')
        ) AS target(code,page_url)
        LOOP
            INSERT INTO skolenza."ModuleOperationMapping"
                ("ModuleId","OperationId","PageURL","IconURL","IsCommonItem",
                 "IsOperational","Priority","Remark","IsActive","AddedById","AddedDateTime")
            SELECT module."Id",operation_id,definition.page_url,
                   CASE operation_type WHEN 11 THEN 'download' ELSE 'upload' END,
                   FALSE,TRUE,operation_type,
                   'Bulk action on the existing Host functional module.',TRUE,1,CURRENT_TIMESTAMP
            FROM skolenza."Module" module
            WHERE module."ModuleCode"=definition.code AND module."ModuleScope"=2
              AND NOT EXISTS (SELECT 1 FROM skolenza."ModuleOperationMapping" existing
                  WHERE existing."ModuleId"=module."Id"
                    AND existing."OperationId"=operation_id);

            UPDATE skolenza."ModuleOperationMapping" mapping
            SET "IsActive"=TRUE,"IsOperational"=TRUE,"UpdatedById"=1,
                "UpdatedDateTime"=CURRENT_TIMESTAMP
            FROM skolenza."Module" module
            WHERE mapping."ModuleId"=module."Id"
              AND mapping."OperationId"=operation_id
              AND module."ModuleCode"=definition.code
              AND module."ModuleScope"=2;
        END LOOP;
    END LOOP;


END $host_bulk_operations$;

COMMIT;

SELECT module."Id",module."ModuleCode",module."ModuleScope",module."PageName",
       operation."Id" AS "OperationId",operation."OperationName"
FROM skolenza."Module" module
JOIN skolenza."ModuleOperationMapping" mapping
  ON mapping."ModuleId"=module."Id" AND mapping."IsActive"
JOIN skolenza."Operation" operation
  ON operation."Id"=mapping."OperationId" AND operation."IsActive"
WHERE module."ModuleCode" IN
    ('HOST_INSTITUTION_RFID_MANAGEMENT','HOST_DEVICE_SETUP','HOST_MODULES',
     'HOST_SUBMODULES','HOST_OPERATIONS','HOST_MODULE_OPERATIONS')
  AND operation."OperationType" IN (11,12)
ORDER BY module."ModuleCode",operation."OperationType";
