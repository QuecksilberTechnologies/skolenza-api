-- Run after AddStaffMemberBulkImport.sql. Transactional, non-destructive upgrade.
-- Host master values partition Host actor IDs from institution-staffMember actor IDs.
BEGIN;
ALTER TABLE skolenza."BulkImportJob" DROP CONSTRAINT IF EXISTS "BulkImportJob_Master_check";
ALTER TABLE skolenza."BulkImportJob" ADD CONSTRAINT "BulkImportJob_Master_check"
    CHECK ("Master" BETWEEN 1 AND 11);
-- Match existing manual-create duplicate semantics; abort if legacy duplicates exist.
CREATE UNIQUE INDEX IF NOT EXISTS "UX_DeviceMaster_Bulk_Code"
    ON skolenza."DeviceMaster" (lower("DeviceCode")) WHERE "IsSoftDeleted" = FALSE;
CREATE UNIQUE INDEX IF NOT EXISTS "UX_DeviceMaster_Bulk_Company_Model"
    ON skolenza."DeviceMaster" (lower("CompanyName"), lower("ModelNo")) WHERE "IsSoftDeleted" = FALSE;
CREATE UNIQUE INDEX IF NOT EXISTS "UX_InstitutionCardMaster_Bulk_Hash"
    ON skolenza."InstitutionCardMaster" ("InstitutionId", "CardNumberLookupHash") WHERE "IsSoftDeleted" = FALSE;
COMMIT;
