-- CANONICAL STAFF_MEMBER RESET + MIGRATION + SEED BUNDLE
-- Run this single file for StaffMember identity catalogue setup.
-- It is idempotent and safe to rerun.
-- Existing StaffMemberIdentity records are never deleted.

BEGIN;
SELECT pg_advisory_xact_lock(7421061201);

-- 1. Clear only previous catalogue rules/documents created by this seed.
DELETE FROM skolenza."CountryIdentityRule" rule
WHERE rule."IdentityCategoryDocumentId" IN (
    SELECT document."Id"
    FROM skolenza."IdentityCategoryDocument" document
    WHERE upper(document."Code") IN (
        'AADHAAR', 'PAN', 'CNIC', 'SSN', 'EMIRATES_ID', 'NATIONAL_ID_CN',
        'PASSPORT', 'EPIC', 'ITIN', 'SIN', 'UAN', 'NINO'
    )
);

DELETE FROM skolenza."IdentityCategoryDocument" document
WHERE upper(document."Code") IN (
    'AADHAAR', 'PAN', 'CNIC', 'SSN', 'EMIRATES_ID', 'NATIONAL_ID_CN',
    'PASSPORT', 'NINO'
)
AND NOT EXISTS (
    SELECT 1
    FROM skolenza."StaffMemberIdentity" identity_record
    WHERE identity_record."IdentityCategoryDocumentId" = document."Id"
);

DELETE FROM skolenza."IdentityCategory" category
WHERE upper(category."Code") = 'GOVT'
AND NOT EXISTS (
    SELECT 1
    FROM skolenza."IdentityCategoryDocument" document
    WHERE document."IdentityCategoryId" = category."Id"
);

-- 2. StaffMember operational-default migration.
CREATE TABLE IF NOT EXISTS skolenza."InstitutionStaffSectionDefault" (
    "InstitutionId" bigint NOT NULL REFERENCES skolenza."Institution"("Id"),
    "ModuleCode" varchar(64) NOT NULL,
    "IsEditAllowed" boolean NOT NULL DEFAULT false,
    "UpdatedById" bigint NOT NULL,
    "UpdatedDateTime" timestamp with time zone NOT NULL DEFAULT now(),
    PRIMARY KEY ("InstitutionId", "ModuleCode"),
    CHECK ("ModuleCode" IN ('EMP_WORK_LOCATIONS', 'EMP_DEVICES',
        'EMP_WORK_ARRANGEMENT', 'EMP_WORK_PATTERN', 'EMP_OVERRIDES'))
);

-- 3. Identity category and document masters.
INSERT INTO skolenza."IdentityCategory"
    ("Code", "Name", "Description", "IsActive", "AddedDateTime")
SELECT 'GOVT', 'Government Issued Identity',
       'Government-issued identity documents', true, now()
WHERE NOT EXISTS (
    SELECT 1 FROM skolenza."IdentityCategory" WHERE upper("Code") = 'GOVT'
);

DO $$
BEGIN
    IF (SELECT count(*) FROM skolenza."IdentityCategory"
        WHERE upper("Code") = 'GOVT') <> 1 THEN
        RAISE EXCEPTION 'Ambiguous GOVT identity category';
    END IF;
END $$;

INSERT INTO skolenza."IdentityCategoryDocument"
    ("IdentityCategoryId", "Code", "DocumentName", "Description",
     "IsUnique", "IsActive", "AddedDateTime")
SELECT category."Id", seed.code, seed.name, seed.description, true, true, now()
FROM skolenza."IdentityCategory" category
CROSS JOIN (VALUES
    ('AADHAAR', 'Aadhaar Card', 'UIDAI identity number; not proof of citizenship'),
    ('PAN', 'PAN Card', 'Indian tax identity number'),
    ('CNIC', 'CNIC', 'Pakistan national identity card'),
    ('SSN', 'Social Security Number', 'United States social security identifier'),
    ('EMIRATES_ID', 'Emirates ID', 'UAE citizen and resident identity card'),
    ('NATIONAL_ID_CN', 'Resident Identity Card', 'China resident identity card'),
    ('NINO', 'National Insurance Number', 'United Kingdom national insurance identifier'),
    ('PASSPORT', 'Passport', 'Government-issued passport')
) AS seed(code, name, description)
WHERE upper(category."Code") = 'GOVT'
  AND NOT EXISTS (
      SELECT 1 FROM skolenza."IdentityCategoryDocument" existing
      WHERE upper(existing."Code") = seed.code
  );

-- 4. Passport is a safe worldwide baseline. Additional documents are mapped
-- only to the issuing country; the API never hardcodes country/document pairs.
INSERT INTO skolenza."CountryIdentityRule"
    ("CountryId", "IdentityCategoryDocumentId", "IsMandatory", "IsActive", "AddedDateTime")
SELECT country."Id", document."Id", false, true, now()
FROM skolenza."Country" country
JOIN skolenza."IdentityCategoryDocument" document
  ON upper(document."Code") = 'PASSPORT'
 AND document."IsActive" = true
WHERE country."IsActive" = true

UNION ALL

SELECT country."Id", document."Id", false, true, now()
FROM (VALUES
    ('IN', 'IND', 'India', 'AADHAAR'),
    ('IN', 'IND', 'India', 'PAN'),
    ('IN', 'IND', 'India', 'EPIC'),
    ('IN', 'IND', 'India', 'UAN'),
    ('PK', 'PAK', 'Pakistan', 'CNIC'),
    ('US', 'USA', 'United States', 'SSN'),
    ('US', 'USA', 'United States', 'ITIN'),
    ('CA', 'CAN', 'Canada', 'SIN'),
    ('GB', 'GBR', 'United Kingdom', 'NINO'),
    ('AE', 'ARE', 'United Arab Emirates', 'EMIRATES_ID'),
    ('CN', 'CHN', 'China', 'NATIONAL_ID_CN')
) AS seed(iso2, iso3, country_name, document_code)
JOIN skolenza."Country" country
  ON upper(country."CountryCode") IN (seed.iso2, seed.iso3)
  OR lower(country."CountryName") = lower(seed.country_name)
JOIN skolenza."IdentityCategoryDocument" document
  ON upper(document."Code") = seed.document_code
 AND document."IsActive" = true
WHERE country."IsActive" = true;

-- Final schema-wide identity synchronization. Earlier stages intentionally use
-- established explicit IDs for canonical operations and master rows. Align each
-- sequence after every seed stage so the next application insert cannot collide.
DO
$$
DECLARE
    identity_column record;
    sequence_name text;
    maximum_id bigint;
BEGIN
    FOR identity_column IN
        SELECT table_name
        FROM information_schema.columns
        WHERE table_schema = 'skolenza'
          AND column_name = 'Id'
    LOOP
        sequence_name := pg_get_serial_sequence(
            format('skolenza.%I', identity_column.table_name),
            'Id'
        );

        IF sequence_name IS NOT NULL THEN
            EXECUTE format(
                'SELECT COALESCE(MAX("Id"), 0) FROM skolenza.%I',
                identity_column.table_name
            ) INTO maximum_id;

            IF maximum_id = 0 THEN
                PERFORM setval(sequence_name::regclass, 1, FALSE);
            ELSE
                PERFORM setval(sequence_name::regclass, maximum_id, TRUE);
            END IF;
        END IF;
    END LOOP;
END;
$$;

COMMIT;
