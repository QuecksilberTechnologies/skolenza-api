-- Adds StaffEmploymentType CRUD and bulk operation metadata plus plan coverage. No direct role grants or Host resets.
BEGIN;
LOCK TABLE skolenza."Module", skolenza."ModuleOperationMapping", skolenza."PlanModuleMapping",
    skolenza."InstitutionEnabledOperation", skolenza."RoleModuleAndPermission" IN SHARE ROW EXCLUSIVE MODE;

DO $staffMember_type_legacy_code$
DECLARE legacy_id integer; canonical_id integer;
BEGIN
    SELECT "Id" INTO legacy_id FROM skolenza."Module"
    WHERE "ModuleCode"='INSTITUTION_STAFF_MEMBER_TYPES' ORDER BY "Id" LIMIT 1;
    SELECT "Id" INTO canonical_id FROM skolenza."Module"
    WHERE "ModuleCode"='STAFF_MEMBER_TYPE' ORDER BY "Id" LIMIT 1;

    IF legacy_id IS NOT NULL AND canonical_id IS NOT NULL AND legacy_id <> canonical_id THEN
        RAISE EXCEPTION 'Conflicting INSTITUTION_STAFF_MEMBER_TYPES and STAFF_MEMBER_TYPE rows exist.';
    END IF;

    IF legacy_id IS NOT NULL AND canonical_id IS NULL THEN
        UPDATE skolenza."Module"
        SET "ModuleCode"='STAFF_MEMBER_TYPE',"UpdatedById"=1,"UpdatedDateTime"=CURRENT_TIMESTAMP
        WHERE "Id"=legacy_id;
    END IF;
END $staffMember_type_legacy_code$;
DO $seed$
DECLARE table_name text;
BEGIN
    FOREACH table_name IN ARRAY ARRAY['Module','ModuleOperationMapping','PlanModuleMapping'] LOOP
        EXECUTE format('SELECT setval(pg_get_serial_sequence(%L,''Id''), GREATEST(COALESCE((SELECT max("Id") FROM skolenza.%I),0),
            COALESCE(pg_sequence_last_value(pg_get_serial_sequence(%L,''Id'')::regclass),0),1),true)',
            'skolenza."' || table_name || '"',table_name,'skolenza."' || table_name || '"');
    END LOOP;
    IF (SELECT count(*) FROM skolenza."Module" WHERE "ModuleCode"='EMP_MGMT' AND "ModuleScope"=1) <> 1 THEN
        RAISE EXCEPTION 'Exactly one EMP_MGMT parent module is required';
    END IF;
END $seed$;
INSERT INTO skolenza."Module" ("InstitutionId","ModuleCode","ModuleName","DisplayName","URLPath","ParentModuleId",
    "IsLeafNode","IsModuleDisplayInUI","IsCommonMenu","IsActive","ImageIconWeb","ImageIconMobile",
    "ItemPriority","Remark","AddedById","AddedDateTime","UpdatedById","UpdatedDateTime","ModuleScope","PageName")
SELECT NULL,'STAFF_MEMBER_TYPE','StaffMember-Types','StaffMember Types','/staff-employment-types',"Id",
    TRUE,TRUE,FALSE,TRUE,"ImageIconWeb","ImageIconMobile","ItemPriority"+1,
    'Institution-owned StaffEmploymentType management',1,CURRENT_TIMESTAMP,1,CURRENT_TIMESTAMP,1,'institution-staff-employment-types'
FROM skolenza."Module" source WHERE source."ModuleCode"='EMP_MGMT'
AND NOT EXISTS (SELECT 1 FROM skolenza."Module" WHERE "ModuleCode"='STAFF_MEMBER_TYPE');

INSERT INTO skolenza."ModuleOperationMapping" ("ModuleId","OperationId","DataViewStructureId","PageTypeId",
    "PageURL","IconURL","IsCommonItem","IsOperational","Priority","Remark","IsActive","AddedById","AddedDateTime")
SELECT target."Id",op."Id",NULL,NULL,'/staff-employment-types','',FALSE,TRUE,op."OperationType",
    'StaffEmploymentType operation; authorization uses existing permission pipeline',TRUE,1,CURRENT_TIMESTAMP
FROM skolenza."Module" target CROSS JOIN skolenza."Operation" op
WHERE target."ModuleCode"='STAFF_MEMBER_TYPE'
  AND op."IsActive"=TRUE
  AND (lower(btrim(op."OperationName")) IN ('add','update','delete','view')
    OR (lower(btrim(op."OperationName"))='import' AND op."OperationType"=12)
    OR (lower(btrim(op."OperationName"))='export' AND op."OperationType"=11))
AND NOT EXISTS (SELECT 1 FROM skolenza."ModuleOperationMapping" existing
    WHERE existing."ModuleId"=target."Id" AND existing."OperationId"=op."Id");

INSERT INTO skolenza."PlanModuleMapping" ("SubscriptionPlanId","ModuleId","IsActive","Remark","AddedById","AddedDateTime")
SELECT DISTINCT source."SubscriptionPlanId",target."Id",TRUE,'StaffEmploymentType inherited from EMP_LIST plan coverage',1,CURRENT_TIMESTAMP
FROM skolenza."PlanModuleMapping" source
JOIN skolenza."Module" baseline ON baseline."Id"=source."ModuleId" AND baseline."ModuleCode"='EMP_LIST'
CROSS JOIN skolenza."Module" target
WHERE source."IsActive"=TRUE AND target."ModuleCode"='STAFF_MEMBER_TYPE'
AND NOT EXISTS (SELECT 1 FROM skolenza."PlanModuleMapping" existing
    WHERE existing."SubscriptionPlanId"=source."SubscriptionPlanId" AND existing."ModuleId"=target."Id");

-- Existing enabled institutions receive only the active CRUD module-operation catalogue.
INSERT INTO skolenza."InstitutionEnabledOperation" ("InstitutionId","ModuleId","OperationId","IsOperationUsed","IsEnabled","AddedById","AddedDateTime")
SELECT enabled_module."InstitutionId", mapping."ModuleId", mapping."OperationId",
    COALESCE(mapping."IsOperational", TRUE), TRUE, 1, CURRENT_TIMESTAMP
FROM skolenza."InstitutionEnabledModule" AS enabled_module
INNER JOIN skolenza."Module" AS target
    ON target."Id" = enabled_module."ModuleId"
INNER JOIN skolenza."ModuleOperationMapping" AS mapping
    ON mapping."ModuleId" = target."Id"
WHERE target."ModuleCode" = 'STAFF_MEMBER_TYPE'
  AND enabled_module."IsEnabled" = TRUE
  AND mapping."IsActive" = TRUE
  AND EXISTS (
      SELECT 1
      FROM skolenza."Operation" AS operation
      WHERE operation."Id" = mapping."OperationId"
        AND operation."IsActive" = TRUE
        AND (lower(btrim(operation."OperationName")) IN ('add','update','delete','view')
          OR (lower(btrim(operation."OperationName"))='import' AND operation."OperationType"=12)
          OR (lower(btrim(operation."OperationName"))='export' AND operation."OperationType"=11)))
  AND NOT EXISTS (
      SELECT 1
      FROM skolenza."InstitutionEnabledOperation" AS existing
      WHERE existing."InstitutionId" = enabled_module."InstitutionId"
        AND existing."ModuleId" = mapping."ModuleId"
        AND existing."OperationId" = mapping."OperationId");
COMMIT;
-- Existing institutions: use the established plan-entitlement synchronization and role-grant UI/API.
