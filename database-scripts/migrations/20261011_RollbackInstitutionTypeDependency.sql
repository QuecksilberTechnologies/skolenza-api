BEGIN;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'skolenza'
          AND table_name = 'Institution'
          AND column_name = 'InstitutionTypeId'
    ) THEN
        ALTER TABLE skolenza."Institution"
            RENAME COLUMN "InstitutionTypeId" TO "TenantIndustryId";
    END IF;
END
$$;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE connamespace = 'skolenza'::regnamespace
          AND conname = 'FK_Institution_InstitutionType'
    ) THEN
        ALTER TABLE skolenza."Institution"
            RENAME CONSTRAINT "FK_Institution_InstitutionType"
            TO "FK_Tenant_TenantIndustry";
    END IF;
END
$$;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'skolenza'
          AND table_name = 'InstitutionType'
          AND column_name = 'InstitutionTypeName'
    ) THEN
        ALTER TABLE skolenza."InstitutionType"
            RENAME COLUMN "InstitutionTypeName" TO "IndustryName";
    END IF;
END
$$;

COMMIT;
