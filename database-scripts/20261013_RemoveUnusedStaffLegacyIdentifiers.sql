BEGIN;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM skolenza."StaffMember"
        WHERE "FunctionalId" IS NOT NULL OR "ReferalId" IS NOT NULL
    ) THEN
        RAISE EXCEPTION 'FunctionalId or ReferalId contains data; explicit migration mapping is required before retirement.';
    END IF;
END $$;

ALTER TABLE skolenza."StaffMember"
    DROP COLUMN IF EXISTS "FunctionalId",
    DROP COLUMN IF EXISTS "ReferalId";

COMMIT;
