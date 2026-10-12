-- Reverses only the schema objects added by the matching integrity migration.
-- It does not modify business data.
BEGIN;

DROP TRIGGER IF EXISTS "TR_UserRole_ValidateInstitution" ON skolenza."UserRole";
DROP FUNCTION IF EXISTS skolenza."ValidateUserRoleInstitution"();

ALTER TABLE skolenza."StaffMember"
    DROP CONSTRAINT IF EXISTS "FK_StaffMember_Institution_Designation",
    DROP CONSTRAINT IF EXISTS "FK_StaffMember_Institution_Department",
    DROP CONSTRAINT IF EXISTS "FK_StaffMember_Department";

ALTER TABLE skolenza."Designation"
    DROP CONSTRAINT IF EXISTS "FK_Designation_Institution_Department",
    DROP CONSTRAINT IF EXISTS "UQ_Designation_Institution_Id";

ALTER TABLE skolenza."Department"
    DROP CONSTRAINT IF EXISTS "UQ_Department_Institution_Id";

ALTER TABLE skolenza."Role"
    DROP CONSTRAINT IF EXISTS "FK_Role_Institution",
    DROP CONSTRAINT IF EXISTS "CK_Role_RoleType",
    ALTER COLUMN "InstitutionId" DROP NOT NULL,
    ALTER COLUMN "RoleType" DROP NOT NULL;

COMMIT;
