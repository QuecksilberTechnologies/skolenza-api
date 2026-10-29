-- Convert the Student-owned master identifiers and references to 64-bit bigint.
-- CountryId and StateId retain their source catalogue integer types.
-- Safe for existing empty or populated master tables; values are preserved.

BEGIN;

ALTER TABLE skolenza."StudentBenefitSchemeMaster"
    DROP CONSTRAINT IF EXISTS "FK_StudentBenefitSchemeMaster_EvidenceRequirement";

ALTER TABLE skolenza."StudentDocumentRequirementMaster"
    ALTER COLUMN "Id" TYPE bigint USING "Id"::bigint,
    ALTER COLUMN "TemplateSourceId" TYPE bigint USING "TemplateSourceId"::bigint;

ALTER TABLE skolenza."StudentFeeFrequencyMaster"
    ALTER COLUMN "Id" TYPE bigint USING "Id"::bigint,
    ALTER COLUMN "TemplateSourceId" TYPE bigint USING "TemplateSourceId"::bigint;

ALTER TABLE skolenza."StudentFeeComponentMaster"
    ALTER COLUMN "Id" TYPE bigint USING "Id"::bigint,
    ALTER COLUMN "TemplateSourceId" TYPE bigint USING "TemplateSourceId"::bigint;

ALTER TABLE skolenza."StudentBenefitSchemeMaster"
    ALTER COLUMN "Id" TYPE bigint USING "Id"::bigint,
    ALTER COLUMN "TemplateSourceId" TYPE bigint USING "TemplateSourceId"::bigint,
    ALTER COLUMN "EvidenceRequirementId" TYPE bigint USING "EvidenceRequirementId"::bigint;

DO $$
DECLARE
    table_name text;
    sequence_name regclass;
BEGIN
    FOREACH table_name IN ARRAY ARRAY[
        'StudentDocumentRequirementMaster',
        'StudentFeeFrequencyMaster',
        'StudentFeeComponentMaster',
        'StudentBenefitSchemeMaster'
    ]
    LOOP
        sequence_name := pg_get_serial_sequence(
            format('skolenza.%I', table_name),
            'Id'
        )::regclass;

        IF sequence_name IS NOT NULL THEN
            EXECUTE format('ALTER SEQUENCE %s AS bigint', sequence_name);
        END IF;
    END LOOP;
END $$;

ALTER TABLE skolenza."StudentBenefitSchemeMaster"
    ADD CONSTRAINT "FK_StudentBenefitSchemeMaster_EvidenceRequirement"
    FOREIGN KEY ("EvidenceRequirementId")
    REFERENCES skolenza."StudentDocumentRequirementMaster"("Id") ON DELETE RESTRICT;

COMMIT;
