-- Preserve the former rule: recurring heads eligible, one-time heads excluded.
BEGIN;
ALTER TABLE skolenza."StudentFeeStructureItem"
    ADD COLUMN IF NOT EXISTS "IsDiscountEligible" boolean NOT NULL DEFAULT true;
ALTER TABLE skolenza."StudentAdmissionFeeQuoteLine"
    ADD COLUMN IF NOT EXISTS "IsDiscountEligible" boolean NOT NULL DEFAULT true;
UPDATE skolenza."StudentFeeStructureItem"
    SET "IsDiscountEligible" = false
    WHERE "ChargeNature" = 2 AND "IsDiscountEligible" = true;
UPDATE skolenza."StudentAdmissionFeeQuoteLine"
    SET "IsDiscountEligible" = false
    WHERE "ChargeNature" = 2 AND "IsDiscountEligible" = true;
COMMIT;
