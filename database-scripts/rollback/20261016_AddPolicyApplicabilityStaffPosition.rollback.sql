BEGIN;

DROP INDEX IF EXISTS skolenza."IX_PolicyApplicability_StaffPositionMasterId";

ALTER TABLE skolenza."PolicyApplicability"
    DROP CONSTRAINT IF EXISTS "FK_PolicyApplicability_StaffPositionMaster";

ALTER TABLE skolenza."PolicyApplicability"
    DROP COLUMN IF EXISTS "StaffPositionMasterId";

COMMIT;
