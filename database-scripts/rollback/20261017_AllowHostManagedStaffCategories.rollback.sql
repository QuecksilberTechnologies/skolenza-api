BEGIN;

-- Refuse rollback once a staff member uses an extended Host category.
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM skolenza."StaffMember"
        WHERE "StaffCategoryId" IS NOT NULL AND "StaffCategoryId" NOT BETWEEN 1 AND 5
    ) THEN
        RAISE EXCEPTION 'Rollback blocked: staff members use Host-managed categories outside the original range.';
    END IF;
END $$;

ALTER TABLE skolenza."StaffMember"
    DROP CONSTRAINT IF EXISTS "CK_StaffMember_StaffCategoryId";
ALTER TABLE skolenza."StaffMember"
    ADD CONSTRAINT "CK_StaffMember_StaffCategoryId"
    CHECK ("StaffCategoryId" IS NULL OR "StaffCategoryId" BETWEEN 1 AND 5);

COMMIT;
