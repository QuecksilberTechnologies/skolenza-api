-- Enforces one institution boundary across access and staff organization masters.
-- Stops on conflicting data; it never reassigns an existing row.
BEGIN;

LOCK TABLE skolenza."Role", skolenza."Department", skolenza."Designation",
    skolenza."StaffMember", skolenza."UserRole" IN SHARE ROW EXCLUSIVE MODE;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM skolenza."Role" role
        LEFT JOIN skolenza."Institution" institution ON institution."Id" = role."InstitutionId"
        WHERE role."InstitutionId" IS NULL OR institution."Id" IS NULL
    ) THEN RAISE EXCEPTION 'Role has a missing or invalid InstitutionId.'; END IF;

    IF EXISTS (SELECT 1 FROM skolenza."Role" WHERE "RoleType" IS NULL OR "RoleType" NOT BETWEEN 1 AND 6)
    THEN RAISE EXCEPTION 'RoleType is outside the central catalogue (1-6).'; END IF;

    IF EXISTS (
        SELECT 1 FROM skolenza."Designation" designation
        LEFT JOIN skolenza."Department" department
          ON department."Id" = designation."DepartmentId"
         AND department."InstitutionId" = designation."InstitutionId"
        WHERE department."Id" IS NULL
    ) THEN RAISE EXCEPTION 'Designation references a missing or other-institution Department.'; END IF;

    IF EXISTS (
        SELECT 1 FROM skolenza."StaffMember" staff_member
        LEFT JOIN skolenza."Department" department
          ON department."Id" = staff_member."DepartmentId"
         AND department."InstitutionId" = staff_member."InstitutionId"
        WHERE staff_member."DepartmentId" IS NOT NULL AND department."Id" IS NULL
    ) THEN RAISE EXCEPTION 'StaffMember references a missing or other-institution Department.'; END IF;

    IF EXISTS (
        SELECT 1 FROM skolenza."StaffMember" staff_member
        LEFT JOIN skolenza."Designation" designation
          ON designation."Id" = staff_member."DesignationId"
         AND designation."InstitutionId" = staff_member."InstitutionId"
        WHERE staff_member."DesignationId" IS NOT NULL AND designation."Id" IS NULL
    ) THEN RAISE EXCEPTION 'StaffMember references a missing or other-institution Designation.'; END IF;

    IF EXISTS (
        SELECT 1 FROM skolenza."UserRole" user_role
        JOIN skolenza."StaffMember" staff_member ON staff_member."Id" = user_role."StaffMemberId"
        JOIN skolenza."Role" role ON role."Id" = user_role."RoleId"
        WHERE staff_member."InstitutionId" IS DISTINCT FROM role."InstitutionId"
    ) THEN RAISE EXCEPTION 'UserRole connects different institutions.'; END IF;
END;
$$;

ALTER TABLE skolenza."Role" ALTER COLUMN "InstitutionId" SET NOT NULL;
ALTER TABLE skolenza."Role" ALTER COLUMN "RoleType" SET NOT NULL;
ALTER TABLE skolenza."Role" DROP CONSTRAINT IF EXISTS "CK_Role_RoleType";
ALTER TABLE skolenza."Role" ADD CONSTRAINT "CK_Role_RoleType" CHECK ("RoleType" BETWEEN 1 AND 6);

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='FK_Role_Institution') THEN
        ALTER TABLE skolenza."Role" ADD CONSTRAINT "FK_Role_Institution"
        FOREIGN KEY ("InstitutionId") REFERENCES skolenza."Institution"("Id") ON DELETE CASCADE;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='UQ_Department_Institution_Id') THEN
        ALTER TABLE skolenza."Department" ADD CONSTRAINT "UQ_Department_Institution_Id" UNIQUE ("InstitutionId", "Id");
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='UQ_Designation_Institution_Id') THEN
        ALTER TABLE skolenza."Designation" ADD CONSTRAINT "UQ_Designation_Institution_Id" UNIQUE ("InstitutionId", "Id");
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='FK_Designation_Institution_Department') THEN
        ALTER TABLE skolenza."Designation" ADD CONSTRAINT "FK_Designation_Institution_Department"
        FOREIGN KEY ("InstitutionId", "DepartmentId") REFERENCES skolenza."Department"("InstitutionId", "Id") ON DELETE RESTRICT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='FK_StaffMember_Department') THEN
        ALTER TABLE skolenza."StaffMember" ADD CONSTRAINT "FK_StaffMember_Department"
        FOREIGN KEY ("DepartmentId") REFERENCES skolenza."Department"("Id") ON DELETE RESTRICT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='FK_StaffMember_Institution_Department') THEN
        ALTER TABLE skolenza."StaffMember" ADD CONSTRAINT "FK_StaffMember_Institution_Department"
        FOREIGN KEY ("InstitutionId", "DepartmentId") REFERENCES skolenza."Department"("InstitutionId", "Id") ON DELETE RESTRICT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='FK_StaffMember_Institution_Designation') THEN
        ALTER TABLE skolenza."StaffMember" ADD CONSTRAINT "FK_StaffMember_Institution_Designation"
        FOREIGN KEY ("InstitutionId", "DesignationId") REFERENCES skolenza."Designation"("InstitutionId", "Id") ON DELETE RESTRICT;
    END IF;
END;
$$;

CREATE OR REPLACE FUNCTION skolenza."ValidateUserRoleInstitution"()
RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE staff_institution_id bigint; role_institution_id bigint;
BEGIN
    IF NEW."StaffMemberId" IS NULL OR NEW."RoleId" IS NULL THEN RETURN NEW; END IF;
    SELECT "InstitutionId" INTO staff_institution_id FROM skolenza."StaffMember" WHERE "Id"=NEW."StaffMemberId";
    SELECT "InstitutionId" INTO role_institution_id FROM skolenza."Role" WHERE "Id"=NEW."RoleId";
    IF staff_institution_id IS NULL OR role_institution_id IS NULL
       OR staff_institution_id IS DISTINCT FROM role_institution_id THEN
        RAISE EXCEPTION 'UserRole StaffMember and Role must belong to the same institution.' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS "TR_UserRole_ValidateInstitution" ON skolenza."UserRole";
CREATE TRIGGER "TR_UserRole_ValidateInstitution"
BEFORE INSERT OR UPDATE OF "StaffMemberId", "RoleId" ON skolenza."UserRole"
FOR EACH ROW EXECUTE FUNCTION skolenza."ValidateUserRoleInstitution"();

COMMIT;
