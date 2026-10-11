-- Final verification for the canonical production seed.
-- This script is read-only and fails when the expected clean baseline is not present.
BEGIN;

DO $$
DECLARE
    host_count bigint;
    institution_count bigint;
    staffMember_count bigint;
    unexpected_host_count bigint;
    duplicate_login_count bigint;
BEGIN
    SELECT count(*) INTO host_count FROM skolenza."HostUser"
    WHERE "IsActive" AND NOT "IsSoftDeleted";

    SELECT count(*) INTO unexpected_host_count FROM skolenza."HostUser"
    WHERE lower(btrim("LoginId")) NOT IN
          ('mca.deepesh@gmail.com', 'sujeet@skolenza.com');

    SELECT count(*) INTO duplicate_login_count
    FROM
    (
        SELECT lower(btrim("LoginId"))
        FROM skolenza."HostUser"
        GROUP BY lower(btrim("LoginId"))
        HAVING count(*) > 1
    ) duplicate_login;

    SELECT count(*) INTO institution_count FROM skolenza."Institution";
    SELECT count(*) INTO staffMember_count FROM skolenza."StaffMember";

    IF host_count <> 2 OR unexpected_host_count <> 0 OR duplicate_login_count <> 0 THEN
        RAISE EXCEPTION
            'Host verification failed: active %, unexpected %, duplicate logins %.',
            host_count,
            unexpected_host_count,
            duplicate_login_count;
    END IF;

    IF institution_count <> 0 OR staffMember_count <> 0 THEN
        RAISE EXCEPTION
            'Business-data cleanup failed: institutions %, staffMembers %.',
            institution_count,
            staffMember_count;
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM skolenza."HostRole" role
        WHERE role."Id" = 1
          AND role."Name" = 'Host-Super-Admin'
          AND role."IsActive"
          AND NOT role."IsSoftDeleted"
    ) THEN
        RAISE EXCEPTION 'Canonical Host-Super-Admin role is missing.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM skolenza."HostRoleModuleAndPermission") THEN
        RAISE EXCEPTION 'Host permission baseline is empty.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM skolenza."Module")
       OR NOT EXISTS (SELECT 1 FROM skolenza."Operation")
       OR NOT EXISTS (SELECT 1 FROM skolenza."ModuleOperationMapping") THEN
        RAISE EXCEPTION 'Module/operation master seed is incomplete.';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM skolenza."ModuleOperationMapping" mapping
        INNER JOIN skolenza."Module" module
            ON module."Id" = mapping."ModuleId"
        INNER JOIN skolenza."Operation" operation
            ON operation."Id" = mapping."OperationId"
        WHERE module."ModuleCode" = 'HOST_INSTITUTION_CREATE'
          AND operation."OperationType" = 1
          AND LOWER(BTRIM(operation."OperationName")) = 'add'
          AND mapping."IsActive"
    ) THEN
        RAISE EXCEPTION 'HOST_INSTITUTION_CREATE/Add mapping is missing.';
    END IF;

    IF EXISTS
    (
        SELECT 1
        FROM skolenza."ModuleOperationMapping" mapping
        INNER JOIN skolenza."Module" module
            ON module."Id" = mapping."ModuleId"
        INNER JOIN skolenza."Operation" operation
            ON operation."Id" = mapping."OperationId"
        WHERE module."ModuleCode" = 'HOST_INSTITUTION_LIST'
          AND operation."OperationType" = 1
          AND LOWER(BTRIM(operation."OperationName")) = 'add'
    ) THEN
        RAISE EXCEPTION 'Obsolete HOST_INSTITUTION_LIST/Add mapping still exists.';
    END IF;

    IF
    (
        SELECT COUNT(DISTINCT LOWER(BTRIM(operation."OperationName")))
        FROM skolenza."ModuleOperationMapping" mapping
        INNER JOIN skolenza."Module" module
            ON module."Id" = mapping."ModuleId"
        INNER JOIN skolenza."Operation" operation
            ON operation."Id" = mapping."OperationId"
        WHERE module."ModuleCode" = 'HOST_INSTITUTION_LIST'
          AND LOWER(BTRIM(operation."OperationName")) IN
              ('view', 'update', 'delete', 'active', 'inactive', 'restore')
          AND mapping."IsActive"
    ) <> 6 THEN
        RAISE EXCEPTION 'HOST_INSTITUTION_LIST lifecycle mappings are incomplete.';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM skolenza."StaffEmploymentType"
        WHERE "Id" = 1
          AND "InstitutionId" IS NULL
          AND "TypeName" = 'Permanent'
          AND "IsActive"
          AND NOT "IsSoftDeleted"
    ) THEN
        RAISE EXCEPTION 'Global Permanent staffMember onboarding template is missing.';
    END IF;

    IF (SELECT count(*)
        FROM skolenza."StaffEmploymentType"
        WHERE "InstitutionId" IS NULL
          AND "TypeName" IN
              ('Permanent', 'Contract', 'Intern', 'Part-Time', 'Freelancer', 'Probationer')
          AND "IsActive"
          AND NOT "IsSoftDeleted") <> 6 THEN
        RAISE EXCEPTION 'Canonical global staffMember type catalogue is incomplete.';
    END IF;

    IF (SELECT count(*)
        FROM skolenza."SubscriptionPlan"
        WHERE "IsActive"
          AND NOT "IsSoftDeleted") <> 3 THEN
        RAISE EXCEPTION 'Exactly three active subscription plans are required.';
    END IF;

    IF (SELECT count(*) FROM skolenza."AttendanceDeviceType"
        WHERE "DeviceTypeCode" IN ('BIOMETRIC', 'WEB', 'MOBILE', 'MANUAL')
          AND "IsActive") <> 4 THEN
        RAISE EXCEPTION 'Attendance device type seed is incomplete.';
    END IF;

    IF (SELECT count(*) FROM skolenza."DefaultEmailConfig"
        WHERE "IsActive" AND "IsDefault") <> 1 THEN
        RAISE EXCEPTION 'Exactly one active default email configuration is required.';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM skolenza."DefaultEmailConfig"
        WHERE "ConfigName" = 'DEFAULT_REGISTRATION_SMTP'
          AND "IsActive"
          AND "IsDefault"
          AND NULLIF(BTRIM("SmtpHost"), '') IS NOT NULL
          AND "SmtpPort" BETWEEN 1 AND 65535
          AND NULLIF(BTRIM("SmtpUsername"), '') IS NOT NULL
          AND NULLIF(BTRIM("SmtpPasswordEncrypted"), '') IS NOT NULL
          AND NULLIF(BTRIM("FromEmail"), '') IS NOT NULL
          AND NULLIF(BTRIM("FromName"), '') IS NOT NULL
          AND NULLIF(BTRIM("SecrateKey"), '') IS NOT NULL
    ) THEN
        RAISE EXCEPTION 'Canonical default email configuration is incomplete.';
    END IF;

    IF (SELECT count(*) FROM skolenza."ComplianceTypeMaster") <> 1018
       OR (SELECT count(*) FROM skolenza."StatutoryType") <> 22 THEN
        RAISE EXCEPTION 'Compliance or statutory master seed is incomplete.';
    END IF;

    IF EXISTS
    (
        SELECT 1
        FROM skolenza."Country" country
        WHERE country."IsActive"
          AND
          (
              (SELECT count(*)
               FROM skolenza."ComplianceTypeMaster" compliance
               WHERE compliance."CountryId" = country."Id"
                 AND compliance."IsActive") < 4
              OR
              (country."CountryCode" IN ('IN', 'CN', 'DE', 'US') AND NOT EXISTS
              (
                  SELECT 1
                  FROM skolenza."StatutoryType" statutory
                  WHERE statutory."CountryId" = country."Id"
                    AND statutory."IsActive"
              ))
          )
    ) THEN
        RAISE EXCEPTION 'A supported country is missing compliance or statutory data.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM skolenza."IdentityCategory")
       OR NOT EXISTS (SELECT 1 FROM skolenza."IdentityCategoryDocument")
       OR NOT EXISTS (SELECT 1 FROM skolenza."CountryIdentityRule") THEN
        RAISE EXCEPTION 'StaffMember identity master catalogue is incomplete.';
    END IF;

    IF (SELECT count(*) FROM skolenza."Country" WHERE "IsActive") <> 249 THEN
        RAISE EXCEPTION 'ISO country catalogue is incomplete.';
    END IF;

    IF (SELECT count(*) FROM skolenza."Country"
        WHERE "CountryCode" IN ('IN', 'CN', 'DE', 'US')) <> 4 THEN
        RAISE EXCEPTION 'Detailed four-country geography seed is incomplete.';
    END IF;
END $$;

SELECT "Id", "Name", "LoginId", "HostRoleId", "IsActive", "IsSoftDeleted"
FROM skolenza."HostUser"
ORDER BY "Id";

SELECT
    (SELECT count(*) FROM skolenza."Module") AS "ModuleCount",
    (SELECT count(*) FROM skolenza."Operation") AS "OperationCount",
    (SELECT count(*) FROM skolenza."ModuleOperationMapping") AS "ModuleOperationCount",
    (SELECT count(*) FROM skolenza."Country") AS "CountryCount",
    (SELECT count(*) FROM skolenza."ComplianceTypeMaster") AS "ComplianceTypeCount",
    (SELECT count(*) FROM skolenza."StatutoryType") AS "StatutoryTypeCount",
    (SELECT count(*) FROM skolenza."SubscriptionPlan") AS "SubscriptionPlanCount",
    (SELECT count(*) FROM skolenza."StaffEmploymentType" WHERE "InstitutionId" IS NULL) AS "GlobalStaffEmploymentTypeCount",
    (SELECT count(*) FROM skolenza."State") AS "StateCount",
    (SELECT count(*) FROM skolenza."District") AS "DistrictCount",
    (SELECT count(*) FROM skolenza."Locality") AS "LocalityCount";

ROLLBACK;
