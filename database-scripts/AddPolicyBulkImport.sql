-- Run after AddHostBulkImport.sql. Transactional and safe to rerun.
-- Policy Type=12, Policy Definition=13, Policy Assignment=14.
BEGIN;
ALTER TABLE skolenza."BulkImportJob"
    DROP CONSTRAINT IF EXISTS "BulkImportJob_Master_check";
ALTER TABLE skolenza."BulkImportJob"
    ADD CONSTRAINT "BulkImportJob_Master_check"
    CHECK ("Master" BETWEEN 1 AND 14);
COMMIT;
