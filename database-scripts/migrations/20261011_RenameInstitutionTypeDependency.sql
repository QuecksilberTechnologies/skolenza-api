BEGIN;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'skolenza'
          AND table_name = 'Institution'
          AND column_name = 'TenantIndustryId'
    ) THEN
        ALTER TABLE skolenza."Institution"
            RENAME COLUMN "TenantIndustryId" TO "InstitutionTypeId";
    END IF;
END
$$;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE connamespace = 'skolenza'::regnamespace
          AND conname = 'FK_Tenant_TenantIndustry'
    ) THEN
        ALTER TABLE skolenza."Institution"
            RENAME CONSTRAINT "FK_Tenant_TenantIndustry"
            TO "FK_Institution_InstitutionType";
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
          AND column_name = 'IndustryName'
    ) THEN
        ALTER TABLE skolenza."InstitutionType"
            RENAME COLUMN "IndustryName" TO "InstitutionTypeName";
    END IF;
END
$$;

COMMIT;
