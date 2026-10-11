-- Institution address now has one authoritative home: Campus.Address.
-- This intentionally removes the duplicate, legacy InstitutionProfile.Address data.
-- Run once before deploying the matching API build.

BEGIN;

ALTER TABLE IF EXISTS skolenza."InstitutionProfile"
    DROP COLUMN IF EXISTS "Address";

COMMIT;
