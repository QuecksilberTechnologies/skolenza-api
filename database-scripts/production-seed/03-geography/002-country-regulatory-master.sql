-- Canonical compliance and statutory masters for every supported country.
-- Countries are resolved by stable CountryCode values; database Id values are never assumed.
BEGIN;
SELECT pg_advisory_xact_lock(2026092901);

TRUNCATE TABLE
    skolenza."ComplianceTypeMaster",
    skolenza."StatutoryType"
RESTART IDENTITY CASCADE;

WITH compliance_seed("CountryCode", "Name") AS
(
    VALUES
        ('IN', 'Provident Fund (PF)'),
        ('IN', 'StaffMember State Insurance (ESI)'),
        ('IN', 'Professional Tax (PT)'),
        ('IN', 'Gratuity'),
        ('IN', 'Labour Welfare Fund (LWF)'),
        ('CN', 'Basic Pension Insurance'),
        ('CN', 'Basic Medical Insurance'),
        ('CN', 'Unemployment Insurance'),
        ('CN', 'Housing Provident Fund'),
        ('CN', 'Work Injury Insurance'),
        ('CN', 'Maternity Insurance'),
        ('DE', 'Statutory Pension Insurance'),
        ('DE', 'Statutory Health Insurance'),
        ('DE', 'Unemployment Insurance'),
        ('DE', 'Long-Term Care Insurance'),
        ('DE', 'Statutory Accident Insurance'),
        ('US', 'Social Security'),
        ('US', 'Medicare'),
        ('US', 'Federal Unemployment Tax (FUTA)'),
        ('US', 'State Unemployment Tax (SUTA)'),
        ('US', 'Federal Income Tax Withholding'),
        ('US', 'State and Local Income Tax Withholding')
),
global_compliance_seed("Name") AS
(
    VALUES
        ('Payroll and Tax Compliance'),
        ('Social Protection Compliance'),
        ('Employment and Labour Compliance'),
        ('Statutory Reporting Compliance')
),
complete_compliance_seed("CountryCode", "Name") AS
(
    SELECT upper(country."CountryCode"), global_seed."Name"
    FROM skolenza."Country" country
    CROSS JOIN global_compliance_seed global_seed
    WHERE country."IsActive" = TRUE

    UNION ALL

    SELECT seed."CountryCode", seed."Name"
    FROM compliance_seed seed
)
INSERT INTO skolenza."ComplianceTypeMaster"
    ("Name", "CountryId", "IsActive")
SELECT seed."Name", country."Id", TRUE
FROM complete_compliance_seed seed
JOIN skolenza."Country" country
  ON upper(country."CountryCode") = seed."CountryCode"
WHERE country."IsActive" = TRUE
ORDER BY country."Id", seed."Name";

WITH statutory_seed
    ("CountryCode", "Code", "Name", "StaffMemberRequired", "EmployerRequired") AS
(
    VALUES
        ('IN', 'EPF', 'StaffMembers Provident Fund', TRUE, TRUE),
        ('IN', 'ESI', 'StaffMembers State Insurance', TRUE, TRUE),
        ('IN', 'PT', 'Professional Tax', TRUE, FALSE),
        ('IN', 'GRATUITY', 'Gratuity', FALSE, TRUE),
        ('IN', 'LWF', 'Labour Welfare Fund', TRUE, TRUE),
        ('CN', 'CN_PENSION', 'Basic Pension Insurance', TRUE, TRUE),
        ('CN', 'CN_MEDICAL', 'Basic Medical Insurance', TRUE, TRUE),
        ('CN', 'CN_UNEMPLOYMENT', 'Unemployment Insurance', TRUE, TRUE),
        ('CN', 'CN_HOUSING', 'Housing Provident Fund', TRUE, TRUE),
        ('CN', 'CN_WORK_INJURY', 'Work Injury Insurance', FALSE, TRUE),
        ('CN', 'CN_MATERNITY', 'Maternity Insurance', FALSE, TRUE),
        ('DE', 'DE_PENSION', 'Statutory Pension Insurance', TRUE, TRUE),
        ('DE', 'DE_HEALTH', 'Statutory Health Insurance', TRUE, TRUE),
        ('DE', 'DE_UNEMPLOYMENT', 'Unemployment Insurance', TRUE, TRUE),
        ('DE', 'DE_CARE', 'Long-Term Care Insurance', TRUE, TRUE),
        ('DE', 'DE_ACCIDENT', 'Statutory Accident Insurance', FALSE, TRUE),
        ('US', 'US_SOCIAL_SECURITY', 'Social Security', TRUE, TRUE),
        ('US', 'US_MEDICARE', 'Medicare', TRUE, TRUE),
        ('US', 'US_FUTA', 'Federal Unemployment Tax', FALSE, TRUE),
        ('US', 'US_SUTA', 'State Unemployment Tax', FALSE, TRUE),
        ('US', 'US_FED_WITHHOLDING', 'Federal Income Tax Withholding', TRUE, FALSE),
        ('US', 'US_STATE_LOCAL', 'State and Local Income Tax Withholding', TRUE, FALSE)
)
INSERT INTO skolenza."StatutoryType"
    ("Code", "Name", "CountryId", "IsStaffMemberContributionRequired",
     "IsEmployerContributionRequired", "IsActive", "AddedById", "AddedDateTime")
SELECT seed."Code", seed."Name", country."Id", seed."StaffMemberRequired",
       seed."EmployerRequired", TRUE, 1, CURRENT_TIMESTAMP
FROM statutory_seed seed
JOIN skolenza."Country" country
  ON upper(country."CountryCode") = seed."CountryCode"
WHERE country."IsActive" = TRUE
ORDER BY country."Id", seed."Code";

DO
$$
DECLARE
    incomplete_country_count bigint;
BEGIN
    SELECT count(*) INTO incomplete_country_count
    FROM skolenza."Country" country
    WHERE country."IsActive" = TRUE
      AND
      (
          SELECT count(*)
          FROM skolenza."ComplianceTypeMaster" compliance
          WHERE compliance."CountryId" = country."Id"
            AND compliance."IsActive" = TRUE
      ) < 4;

    IF incomplete_country_count <> 0 THEN
        RAISE EXCEPTION
            'Country compliance seed failed: incomplete active countries %.',
            incomplete_country_count;
    END IF;

    IF EXISTS
    (
        SELECT 1
        FROM skolenza."Country" country
        WHERE country."CountryCode" IN ('IN', 'CN', 'DE', 'US')
          AND NOT EXISTS
          (
              SELECT 1
              FROM skolenza."StatutoryType" statutory
              WHERE statutory."CountryId" = country."Id"
                AND statutory."IsActive" = TRUE
          )
    )
    THEN
        RAISE EXCEPTION 'Verified statutory seed is incomplete for a supported jurisdiction.';
    END IF;

    IF (SELECT count(*) FROM skolenza."ComplianceTypeMaster") <> 1018
       OR (SELECT count(*) FROM skolenza."StatutoryType") <> 22 THEN
        RAISE EXCEPTION 'Country regulatory seed row counts are incomplete.';
    END IF;
END;
$$;

COMMIT;
