-- ================================================================
-- Purpose : Supports the default StaffMemberContact row and locality selection.
-- Safety  : Additive/idempotent; existing contact values are not rewritten.
-- ================================================================

BEGIN;

ALTER TABLE skolenza."StaffMemberContact"
    ALTER COLUMN "ContactNumber" DROP NOT NULL,
    ALTER COLUMN "ContactName" TYPE varchar(302);

ALTER TABLE skolenza."StaffMemberContact"
    ADD COLUMN IF NOT EXISTS "LocalityId" integer NULL;

DO $$
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'FK_StaffMemberContact_Locality'
          AND conrelid = 'skolenza."StaffMemberContact"'::regclass
    ) THEN
        ALTER TABLE skolenza."StaffMemberContact"
            ADD CONSTRAINT "FK_StaffMemberContact_Locality"
            FOREIGN KEY ("LocalityId")
            REFERENCES skolenza."Locality" ("Id")
            ON DELETE SET NULL;
    END IF;
END
$$;

CREATE INDEX IF NOT EXISTS "IX_StaffMemberContact_LocalityId"
    ON skolenza."StaffMemberContact" ("LocalityId");

COMMIT;
