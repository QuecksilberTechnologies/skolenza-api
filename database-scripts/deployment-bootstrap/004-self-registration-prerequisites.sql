\if :{?smtp_username}
\else
\echo 'smtp_username is required'
\quit
\endif

\if :{?smtp_secret}
\else
\echo 'smtp_secret is required'
\quit
\endif

BEGIN;

INSERT INTO skolenza."Gender" ("Id", "GenderName")
VALUES
    (1, 'Male'),
    (2, 'Female'),
    (3, 'Other')
ON CONFLICT ("Id") DO UPDATE
SET "GenderName" = EXCLUDED."GenderName";

SELECT setval(
    pg_get_serial_sequence('skolenza."Gender"', 'Id'),
    GREATEST((SELECT MAX("Id") FROM skolenza."Gender"), 1),
    TRUE);

-- Global Staff Type templates copied into each newly registered institution by the
-- established onboarding repository. IDs remain stable because ID 1 is the
-- permanent-staff template used by the creation pipeline.
INSERT INTO skolenza."StaffEmploymentType"
(
    "Id", "InstitutionId", "TypeName", "Description", "Remark", "IsActive",
    "AddedById", "AddedDateTime", "IsSoftDeleted"
)
VALUES
    (1, NULL, 'Permanent', 'Staff member appointed on a permanent basis', 'Global onboarding template', TRUE, 1, CURRENT_TIMESTAMP, FALSE),
    (2, NULL, 'Guest', 'Guest faculty or visiting staff member', 'Global staff type template', TRUE, 1, CURRENT_TIMESTAMP, FALSE),
    (3, NULL, 'Contractual', 'Staff member engaged for a fixed contractual term', 'Global staff type template', TRUE, 1, CURRENT_TIMESTAMP, FALSE),
    (4, NULL, 'Technical', 'Technical and laboratory support staff member', 'Global staff type template', TRUE, 1, CURRENT_TIMESTAMP, FALSE),
    (5, NULL, 'Teaching', 'Academic teaching staff member', 'Global staff type template', TRUE, 1, CURRENT_TIMESTAMP, FALSE),
    (6, NULL, 'Non-Teaching', 'Administrative or operational non-teaching staff member', 'Global staff type template', TRUE, 1, CURRENT_TIMESTAMP, FALSE)
ON CONFLICT ("Id") DO UPDATE
SET "InstitutionId" = NULL,
    "TypeName" = EXCLUDED."TypeName",
    "Description" = EXCLUDED."Description",
    "Remark" = EXCLUDED."Remark",
    "IsActive" = TRUE,
    "IsSoftDeleted" = FALSE;

SELECT setval(
    pg_get_serial_sequence('skolenza."StaffEmploymentType"', 'Id'),
    GREATEST((SELECT MAX("Id") FROM skolenza."StaffEmploymentType"), 1),
    TRUE);

-- Host-owned template used by both self-registration and Host-created Institution
-- onboarding. The mail service supplies the Institution context and verification
-- placeholders at runtime.
UPDATE skolenza."EmailTemplate"
SET "TemplateName" = 'Institution Welcome',
    "Subject" = 'Welcome to {{InstitutionName}}',
    "Body" = '<p>Dear <strong>{{UserName}}</strong>,</p><p>Welcome to <strong>{{InstitutionName}}</strong>.</p><p>Please <a href="{{VerificationUrl}}">set your password</a> to activate your account.</p><p>This link expires in {{LinkExpiryMinutes}} minutes.</p><p>Regards,<br>{{InstitutionName}} Team</p>',
    "FromName" = 'Skolenza Notifications',
    "Category" = 'System',
    "LanguageCode" = 'en',
    "IsActive" = TRUE,
    "UpdatedById" = 1,
    "UpdatedDateTime" = CURRENT_TIMESTAMP
WHERE UPPER(BTRIM("TemplateCode")) = 'WELCOME_EMAIL';

INSERT INTO skolenza."EmailTemplate"
(
    "TemplateName", "TemplateCode", "Subject", "Body", "FromName",
    "Category", "LanguageCode", "IsActive", "AddedById", "AddedDateTime"
)
SELECT
    'Institution Welcome',
    'WELCOME_EMAIL',
    'Welcome to {{InstitutionName}}',
    '<p>Dear <strong>{{UserName}}</strong>,</p><p>Welcome to <strong>{{InstitutionName}}</strong>.</p><p>Please <a href="{{VerificationUrl}}">set your password</a> to activate your account.</p><p>This link expires in {{LinkExpiryMinutes}} minutes.</p><p>Regards,<br>{{InstitutionName}} Team</p>',
    'Skolenza Notifications',
    'System',
    'en',
    TRUE,
    1,
    CURRENT_TIMESTAMP
WHERE NOT EXISTS
(
    SELECT 1
    FROM skolenza."EmailTemplate"
    WHERE UPPER(BTRIM("TemplateCode")) = 'WELCOME_EMAIL'
);

INSERT INTO skolenza."DefaultEmailConfig"
(
    "ConfigName",
    "SmtpHost",
    "SmtpPort",
    "SmtpUsername",
    "SmtpPasswordEncrypted",
    "FromEmail",
    "FromName",
    "IsActive",
    "IsDefault",
    "SecrateKey",
    "CreatedDateTime",
    "UpdatedDateTime"
)
VALUES
(
    'DEFAULT_REGISTRATION_SMTP',
    'smtp-relay.brevo.com',
    587,
    :'smtp_username',
    :'smtp_secret',
    'admin@quecksilber.in',
    'Skolenza Registration',
    TRUE,
    TRUE,
    :'smtp_secret',
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
)
ON CONFLICT ("ConfigName") DO UPDATE
SET "SmtpHost" = EXCLUDED."SmtpHost",
    "SmtpPort" = EXCLUDED."SmtpPort",
    "SmtpUsername" = EXCLUDED."SmtpUsername",
    "SmtpPasswordEncrypted" = EXCLUDED."SmtpPasswordEncrypted",
    "FromEmail" = EXCLUDED."FromEmail",
    "FromName" = EXCLUDED."FromName",
    "IsActive" = TRUE,
    "IsDefault" = TRUE,
    "SecrateKey" = EXCLUDED."SecrateKey",
    "UpdatedDateTime" = CURRENT_TIMESTAMP;

DO $verification$
BEGIN
    IF (SELECT COUNT(*) FROM skolenza."Gender") < 3 THEN
        RAISE EXCEPTION 'Gender seed is incomplete';
    END IF;

    IF (SELECT COUNT(*) FROM skolenza."StaffEmploymentType"
        WHERE "InstitutionId" IS NULL
          AND "IsActive" = TRUE
          AND "IsSoftDeleted" = FALSE) < 6 THEN
        RAISE EXCEPTION 'Global Staff Type templates are incomplete';
    END IF;

    IF (SELECT COUNT(*) FROM skolenza."EmailTemplate"
        WHERE UPPER(BTRIM("TemplateCode")) = 'WELCOME_EMAIL'
          AND "IsActive" = TRUE) <> 1 THEN
        RAISE EXCEPTION 'Active Institution welcome email template is missing or duplicated';
    END IF;

    IF (SELECT COUNT(*) FROM skolenza."DefaultEmailConfig"
        WHERE "ConfigName" = 'DEFAULT_REGISTRATION_SMTP'
          AND "IsActive" = TRUE
          AND "IsDefault" = TRUE) <> 1 THEN
        RAISE EXCEPTION 'Active registration SMTP configuration is missing';
    END IF;
END
$verification$;

COMMIT;
