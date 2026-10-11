BEGIN;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'skolenza'
          AND table_name = 'InstitutionProfile'
          AND column_name = 'TotalBranches'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'skolenza'
          AND table_name = 'InstitutionProfile'
          AND column_name = 'TotalCampuses'
    ) THEN
        ALTER TABLE skolenza."InstitutionProfile"
            RENAME COLUMN "TotalBranches" TO "TotalCampuses";
    END IF;
END $$;

COMMIT;