BEGIN;

ALTER TABLE skolenza."Employee"
    DROP CONSTRAINT IF EXISTS "CK_Employee_StaffCategoryId";

ALTER TABLE skolenza."Employee"
    DROP COLUMN IF EXISTS "StaffCategoryId";

COMMIT;
