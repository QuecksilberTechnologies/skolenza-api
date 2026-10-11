-- =============================================================================
-- StaffMemberWorkArrangement: use the generic Published Attendance PolicyVersion
-- =============================================================================
-- Existing legacy AttendancePolicy references are retained for historical rows.
-- New application writes use PolicyVersionId. No guessed legacy-data backfill is
-- performed because the two policy models do not have a guaranteed 1:1 mapping.

BEGIN;

ALTER TABLE skolenza."StaffMemberWorkArrangement"
    ALTER COLUMN "AttendancePolicyId" DROP NOT NULL;

ALTER TABLE skolenza."StaffMemberWorkArrangement"
    ADD COLUMN IF NOT EXISTS "PolicyVersionId" bigint;

CREATE INDEX IF NOT EXISTS "IX_StaffMemberWorkArrangement_PolicyVersionId"
    ON skolenza."StaffMemberWorkArrangement" ("PolicyVersionId");

DO $$
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'FK_StaffMemberWorkArrangement_PolicyVersion'
          AND conrelid = 'skolenza."StaffMemberWorkArrangement"'::regclass
    ) THEN
        ALTER TABLE skolenza."StaffMemberWorkArrangement"
            ADD CONSTRAINT "FK_StaffMemberWorkArrangement_PolicyVersion"
            FOREIGN KEY ("PolicyVersionId")
            REFERENCES skolenza."PolicyVersion" ("Id")
            ON DELETE RESTRICT;
    END IF;
END $$;

COMMIT;
