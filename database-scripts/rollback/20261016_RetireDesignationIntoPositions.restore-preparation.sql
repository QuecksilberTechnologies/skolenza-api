-- Recovery preparation for a verified, full pg_dump custom-format backup.
-- Stop API/worker writes and verify the selected target before recovery.
-- Tested against an isolated restore of the pre-cutover Render backup on 2026-10-16.
-- These additive FKs may not exist in the old backup and otherwise block pg_restore --clean.
-- After this file, restore the complete verified backup with:
-- pg_restore --exit-on-error --clean --if-exists --no-owner --no-acl -d <confirmed-target> <verified-backup.dump>
-- The restore recovers the pre-cutover state; writes after that backup must be reconciled before recovery.
BEGIN;
ALTER TABLE IF EXISTS skolenza."PolicyApplicability"
    DROP CONSTRAINT IF EXISTS "FK_PolicyApplicability_StaffPositionMaster";
ALTER TABLE IF EXISTS skolenza."StaffMemberManagerMapping"
    DROP CONSTRAINT IF EXISTS "FK_StaffMemberManagerMapping_StaffPositionMaster";
COMMIT;
