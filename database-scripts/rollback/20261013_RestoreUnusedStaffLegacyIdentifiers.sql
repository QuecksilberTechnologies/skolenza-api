BEGIN;

ALTER TABLE skolenza."StaffMember"
    ADD COLUMN IF NOT EXISTS "FunctionalId" integer NULL,
    ADD COLUMN IF NOT EXISTS "ReferalId" integer NULL;

COMMIT;
