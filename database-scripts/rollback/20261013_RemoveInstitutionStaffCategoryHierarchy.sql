BEGIN;
ALTER TABLE skolenza."StaffMember" DROP CONSTRAINT IF EXISTS "FK_StaffMember_InstitutionCategory";
ALTER TABLE skolenza."StaffPositionMaster" DROP CONSTRAINT IF EXISTS "FK_StaffPositionMaster_InstitutionCategory";
ALTER TABLE skolenza."StaffMember" DROP COLUMN IF EXISTS "InstitutionStaffCategoryMasterId";
ALTER TABLE skolenza."StaffPositionMaster" DROP COLUMN IF EXISTS "InstitutionStaffCategoryMasterId";
DROP FUNCTION IF EXISTS skolenza."SeedInstitutionStaffCategoryHierarchy"(bigint, bigint);
DROP TABLE IF EXISTS skolenza."StaffCategoryJobFamily";
DROP TABLE IF EXISTS skolenza."InstitutionStaffCategoryMaster";
COMMIT;
