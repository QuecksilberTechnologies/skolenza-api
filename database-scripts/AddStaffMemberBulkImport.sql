-- StaffMember import uses the existing durable queue. Fail on conflicting legacy
-- identities rather than merging, overwriting or deleting staff member accounts.
BEGIN;
ALTER TABLE skolenza."BulkImportJob" DROP CONSTRAINT IF EXISTS "BulkImportJob_Master_check";
ALTER TABLE skolenza."BulkImportJob" ADD CONSTRAINT "BulkImportJob_Master_check"
    CHECK ("Master" BETWEEN 1 AND 5);
CREATE UNIQUE INDEX IF NOT EXISTS "UX_StaffMember_Institution_Code"
    ON skolenza."StaffMember" ("InstitutionId", lower(btrim("EmployementCode")))
    WHERE "EmployementCode" IS NOT NULL AND btrim("EmployementCode") <> '';
CREATE UNIQUE INDEX IF NOT EXISTS "UX_LoginCredential_NormalizedLogin"
    ON skolenza."LoginCredential" (lower(btrim("LoginId")));
COMMIT;


