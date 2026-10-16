BEGIN;

ALTER TABLE skolenza."PolicyApplicability"
    ADD COLUMN IF NOT EXISTS "StaffPositionMasterId" integer;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'FK_PolicyApplicability_StaffPositionMaster'
          AND connamespace = 'skolenza'::regnamespace
    ) THEN
        ALTER TABLE skolenza."PolicyApplicability"
            ADD CONSTRAINT "FK_PolicyApplicability_StaffPositionMaster"
            FOREIGN KEY ("StaffPositionMasterId")
            REFERENCES skolenza."StaffPositionMaster"("Id")
            ON DELETE RESTRICT;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS "IX_PolicyApplicability_StaffPositionMasterId"
    ON skolenza."PolicyApplicability" ("StaffPositionMasterId");

COMMIT;
