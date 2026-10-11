-- Removes the standalone EMP_PASSWORD_MANAGEMENT module and moves its
-- Reset Password permission to the existing STAFF_DIRECTORY module.

BEGIN;

DO $preconditions$
DECLARE
    staffMember_list_id integer;
    reset_password_id integer;
    missing_institution_count integer;
    missing_plan_count integer;
BEGIN
    SELECT "Id" INTO staffMember_list_id
    FROM skolenza."Module"
    WHERE "ModuleCode" = 'STAFF_DIRECTORY';

    SELECT "Id" INTO reset_password_id
    FROM skolenza."Operation"
    WHERE "Id" = 21
      AND lower(btrim("OperationName")) = 'reset password';

    IF staffMember_list_id IS NULL THEN
        RAISE EXCEPTION 'STAFF_DIRECTORY module was not found.';
    END IF;

    IF reset_password_id IS NULL THEN
        RAISE EXCEPTION 'Operation Id 21 must be Reset Password.';
    END IF;

    SELECT count(*) INTO missing_institution_count
    FROM skolenza."InstitutionEnabledModule" legacy
    WHERE legacy."ModuleId" = 38
      AND NOT EXISTS
      (
          SELECT 1 FROM skolenza."InstitutionEnabledModule" staffMember_list
          WHERE staffMember_list."InstitutionId" = legacy."InstitutionId"
            AND staffMember_list."ModuleId" = staffMember_list_id
      );

    IF missing_institution_count <> 0 THEN
        RAISE EXCEPTION '% institution(s) have Module 38 without STAFF_DIRECTORY entitlement.', missing_institution_count;
    END IF;

    SELECT count(*) INTO missing_plan_count
    FROM skolenza."PlanModuleMapping" legacy
    WHERE legacy."ModuleId" = 38
      AND NOT EXISTS
      (
          SELECT 1 FROM skolenza."PlanModuleMapping" staffMember_list
          WHERE staffMember_list."SubscriptionPlanId" = legacy."SubscriptionPlanId"
            AND staffMember_list."ModuleId" = staffMember_list_id
      );

    IF missing_plan_count <> 0 THEN
        RAISE EXCEPTION '% plan(s) have Module 38 without STAFF_DIRECTORY entitlement.', missing_plan_count;
    END IF;
END
$preconditions$;

UPDATE skolenza."Operation"
SET "Remark" = 'Reset the login password of a selected institution staffMember',
    "OperationType" = 2,
    "IsActive" = true,
    "IconImage" = 'key-round',
    "UpdatedById" = 1,
    "UpdatedDateTime" = CURRENT_TIMESTAMP
WHERE "Id" = 21
  AND lower(btrim("OperationName")) = 'reset password';

INSERT INTO skolenza."ModuleOperationMapping"
(
    "ModuleId", "OperationId", "PageURL", "IconURL", "IsCommonItem",
    "IsOperational", "Priority", "Remark", "IsActive", "AddedById", "AddedDateTime"
)
SELECT module."Id", 21, module."URLPath", 'key-round', false, true, 10,
       'Reset a selected staffMember password from the StaffMembers module.',
       true, 1, CURRENT_TIMESTAMP
FROM skolenza."Module" module
WHERE module."ModuleCode" = 'STAFF_DIRECTORY'
  AND NOT EXISTS
  (
      SELECT 1 FROM skolenza."ModuleOperationMapping" existing
      WHERE existing."ModuleId" = module."Id" AND existing."OperationId" = 21
  );

UPDATE skolenza."ModuleOperationMapping" mapping
SET "PageURL" = module."URLPath",
    "IconURL" = 'key-round',
    "IsCommonItem" = false,
    "IsOperational" = true,
    "Priority" = 10,
    "Remark" = 'Reset a selected staffMember password from the StaffMembers module.',
    "IsActive" = true,
    "UpdatedById" = 1,
    "UpdatedDateTime" = CURRENT_TIMESTAMP
FROM skolenza."Module" module
WHERE module."ModuleCode" = 'STAFF_DIRECTORY'
  AND mapping."ModuleId" = module."Id"
  AND mapping."OperationId" = 21;

INSERT INTO skolenza."InstitutionEnabledOperation"
(
    "InstitutionId", "ModuleId", "OperationId", "IsOperationUsed", "IsEnabled",
    "AddedById", "AddedDateTime", "UpdatedById", "UpdatedDateTime"
)
SELECT legacy."InstitutionId", staffMember_list."Id", 21,
       legacy."IsOperationUsed", legacy."IsEnabled",
       legacy."AddedById", legacy."AddedDateTime", 1, CURRENT_TIMESTAMP
FROM skolenza."InstitutionEnabledOperation" legacy
CROSS JOIN skolenza."Module" staffMember_list
WHERE legacy."ModuleId" = 38
  AND legacy."OperationId" = 21
  AND staffMember_list."ModuleCode" = 'STAFF_DIRECTORY'
  AND NOT EXISTS
  (
      SELECT 1 FROM skolenza."InstitutionEnabledOperation" existing
      WHERE existing."InstitutionId" = legacy."InstitutionId"
        AND existing."ModuleId" = staffMember_list."Id"
        AND existing."OperationId" = 21
  );

INSERT INTO skolenza."RoleModuleAndPermission"
(
    "RoleId", "ModuleId", "OperationId", "HasAccess", "IsActive", "Remark",
    "IsOperational", "ImageIcon", "AddedById", "AddedDateTime",
    "UpdatedById", "UpdateDateTime", "SoftDeletedById", "DeletedDateTime",
    "IsSoftDeleted", "UpdatedDateTime"
)
SELECT legacy."RoleId", staffMember_list."Id", 21, legacy."HasAccess", legacy."IsActive",
       'Reset Password permission moved from retired Module 38 to STAFF_DIRECTORY.',
       legacy."IsOperational", legacy."ImageIcon", legacy."AddedById", legacy."AddedDateTime",
       1, CURRENT_TIMESTAMP, legacy."SoftDeletedById", legacy."DeletedDateTime",
       legacy."IsSoftDeleted", CURRENT_TIMESTAMP
FROM skolenza."RoleModuleAndPermission" legacy
CROSS JOIN skolenza."Module" staffMember_list
WHERE legacy."ModuleId" = 38
  AND legacy."OperationId" = 21
  AND staffMember_list."ModuleCode" = 'STAFF_DIRECTORY'
  AND NOT EXISTS
  (
      SELECT 1 FROM skolenza."RoleModuleAndPermission" existing
      WHERE existing."RoleId" = legacy."RoleId"
        AND existing."ModuleId" = staffMember_list."Id"
        AND existing."OperationId" = 21
        AND existing."IsSoftDeleted" IS DISTINCT FROM true
  );

DELETE FROM skolenza."HostRoleModuleAndPermission" WHERE "ModuleId" = 38;
DELETE FROM skolenza."RoleModuleAndPermission" WHERE "ModuleId" = 38;
DELETE FROM skolenza."InstitutionEnabledOperation" WHERE "ModuleId" = 38;
DELETE FROM skolenza."InstitutionEnabledModule" WHERE "ModuleId" = 38;
DELETE FROM skolenza."PlanModuleMapping" WHERE "ModuleId" = 38;
DELETE FROM skolenza."ModuleOperationMapping" WHERE "ModuleId" = 38;

DO $children$
BEGIN
    IF EXISTS (SELECT 1 FROM skolenza."Module" WHERE "ParentModuleId" = 38) THEN
        RAISE EXCEPTION 'Module 38 still has child modules and cannot be deleted.';
    END IF;
END
$children$;

DELETE FROM skolenza."Module" WHERE "Id" = 38 OR "ModuleCode" = 'EMP_PASSWORD_MANAGEMENT';

DO $verification$
DECLARE
    staffMember_list_id integer;
BEGIN
    SELECT "Id" INTO staffMember_list_id FROM skolenza."Module" WHERE "ModuleCode" = 'STAFF_DIRECTORY';

    IF EXISTS (SELECT 1 FROM skolenza."Module" WHERE "Id" = 38 OR "ModuleCode" = 'EMP_PASSWORD_MANAGEMENT')
       OR EXISTS (SELECT 1 FROM skolenza."ModuleOperationMapping" WHERE "ModuleId" = 38)
       OR EXISTS (SELECT 1 FROM skolenza."InstitutionEnabledModule" WHERE "ModuleId" = 38)
       OR EXISTS (SELECT 1 FROM skolenza."InstitutionEnabledOperation" WHERE "ModuleId" = 38)
       OR EXISTS (SELECT 1 FROM skolenza."RoleModuleAndPermission" WHERE "ModuleId" = 38)
       OR EXISTS (SELECT 1 FROM skolenza."PlanModuleMapping" WHERE "ModuleId" = 38) THEN
        RAISE EXCEPTION 'Module 38 cleanup verification failed.';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1 FROM skolenza."ModuleOperationMapping"
        WHERE "ModuleId" = staffMember_list_id AND "OperationId" = 21 AND "IsActive" = true
    ) THEN
        RAISE EXCEPTION 'STAFF_DIRECTORY Reset Password mapping was not created.';
    END IF;
END
$verification$;

COMMIT;
