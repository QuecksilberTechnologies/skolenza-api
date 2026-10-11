BEGIN;

-- Retire the legacy staffMember-insurance menu and every permission/plan mapping
-- that points to it. Generic policy modules are selected by different stable
-- codes and are intentionally outside this cleanup.
CREATE TEMP TABLE retired_policy_module_ids ON COMMIT DROP AS
SELECT "Id"
FROM skolenza."Module"
WHERE "ModuleCode" = 'EMP_INSURANCE';

DELETE FROM skolenza."InstitutionEnabledOperation"
WHERE "ModuleId" IN (SELECT "Id" FROM retired_policy_module_ids);

DELETE FROM skolenza."InstitutionEnabledModule"
WHERE "ModuleId" IN (SELECT "Id" FROM retired_policy_module_ids);

DELETE FROM skolenza."RoleModuleAndPermission"
WHERE "ModuleId" IN (SELECT "Id" FROM retired_policy_module_ids);

DELETE FROM skolenza."HostRoleModuleAndPermission"
WHERE "ModuleId" IN (SELECT "Id" FROM retired_policy_module_ids);

DELETE FROM skolenza."PlanModuleMapping"
WHERE "ModuleId" IN (SELECT "Id" FROM retired_policy_module_ids);

DELETE FROM skolenza."ModuleOperationMapping"
WHERE "ModuleId" IN (SELECT "Id" FROM retired_policy_module_ids);

DELETE FROM skolenza."Module"
WHERE "Id" IN (SELECT "Id" FROM retired_policy_module_ids);

-- These tables belong to the replaced policy implementations. Their live
-- equivalents are Policy, PolicyVersion, PolicyRule, PolicyApplicability,
-- PolicyAssignment, PolicyDocument and the related generic policy tables.
DROP TABLE IF EXISTS skolenza."StaffMemberPolicyDependentMapping" CASCADE;
DROP TABLE IF EXISTS skolenza."StaffMemberPolicyEnrollment" CASCADE;
DROP TABLE IF EXISTS skolenza."InsurancePolicyDocument" CASCADE;
DROP TABLE IF EXISTS skolenza."PolicyTypeInsuranceMapping" CASCADE;
DROP TABLE IF EXISTS skolenza."InsurancePolicy" CASCADE;
DROP TABLE IF EXISTS skolenza."StaffMemberLeaveBalance" CASCADE;
DROP TABLE IF EXISTS skolenza."StaffMemberLeavePolicyMapping" CASCADE;
DROP TABLE IF EXISTS skolenza."LeaveSandwichRuleMapping" CASCADE;
DROP TABLE IF EXISTS skolenza."LeaveSandwichRule" CASCADE;
DROP TABLE IF EXISTS skolenza."DayCombination" CASCADE;
DROP TABLE IF EXISTS skolenza."LeaveRule" CASCADE;
DROP TABLE IF EXISTS skolenza."PolicyLeaveTypeMapping" CASCADE;
DROP TABLE IF EXISTS skolenza."AccommodationAllowancePolicyByDesignation" CASCADE;
DROP TABLE IF EXISTS skolenza."MealAllowancePolicyByDesignation" CASCADE;
DROP TABLE IF EXISTS skolenza."TravelAllowancePolicyByDesignation" CASCADE;
DROP TABLE IF EXISTS skolenza."PolicyTypeDocument" CASCADE;
DROP TABLE IF EXISTS skolenza."UnStructuredPolicyTypeMappingWithStaffEmploymentType" CASCADE;

ALTER TABLE IF EXISTS skolenza."LeaveRequest"
    DROP COLUMN IF EXISTS "LeavePolicyId";

COMMIT;
