BEGIN;
ALTER TABLE skolenza."StaffMember" DROP CONSTRAINT IF EXISTS "FK_StaffMember_StaffCategory", DROP CONSTRAINT IF EXISTS "CK_StaffMember_StaffCategoryId";
DROP INDEX IF EXISTS skolenza."IX_StaffMember_StaffCategoryId";
UPDATE skolenza."StaffMember" SET "StaffCategoryId" = CASE "StaffCategoryId" WHEN 1 THEN 4 WHEN 2 THEN 1 WHEN 3 THEN 2 WHEN 4 THEN 5 WHEN 5 THEN 3 ELSE NULL END;
DROP TABLE IF EXISTS skolenza."StaffCategory";
COMMIT;
