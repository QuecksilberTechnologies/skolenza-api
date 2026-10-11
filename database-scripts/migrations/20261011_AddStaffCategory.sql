BEGIN;

ALTER TABLE skolenza."Employee"
    ADD COLUMN IF NOT EXISTS "StaffCategoryId" smallint;

UPDATE skolenza."Employee"
SET "StaffCategoryId" = 4
WHERE "StaffCategoryId" IS NULL;

ALTER TABLE skolenza."Employee"
    ALTER COLUMN "StaffCategoryId" DROP NOT NULL,
    ALTER COLUMN "StaffCategoryId" DROP DEFAULT;

ALTER TABLE skolenza."Employee"
    DROP CONSTRAINT IF EXISTS "CK_Employee_StaffCategoryId";

ALTER TABLE skolenza."Employee"
    ADD CONSTRAINT "CK_Employee_StaffCategoryId"
    CHECK ("StaffCategoryId" IS NULL OR "StaffCategoryId" IN (1, 2, 3, 4, 5));

COMMENT ON COLUMN skolenza."Employee"."StaffCategoryId" IS
    'Staff category: 1 Teaching, 2 Non-Teaching, 3 Technical, 4 Administrative, 5 Support.';

COMMIT;
