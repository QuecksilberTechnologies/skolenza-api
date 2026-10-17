BEGIN;

-- The Host-managed catalogue owns valid category IDs. The original five-value
-- range is obsolete; the existing FK continues to reject unknown categories.
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'skolenza."StaffMember"'::regclass
          AND confrelid = 'skolenza."StaffCategory"'::regclass
          AND contype = 'f'
    ) THEN
        RAISE EXCEPTION 'Staff Category foreign key is required before removing the legacy range restriction.';
    END IF;
END $$;

ALTER TABLE skolenza."StaffMember"
    DROP CONSTRAINT IF EXISTS "CK_StaffMember_StaffCategoryId";
COMMENT ON COLUMN skolenza."StaffMember"."StaffCategoryId" IS
    'Central Host-managed Staff Category catalogue FK; IDs are not restricted to the original seed categories.';

COMMIT;
