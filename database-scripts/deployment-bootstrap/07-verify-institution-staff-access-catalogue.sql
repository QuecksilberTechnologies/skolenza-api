\set ON_ERROR_STOP on

WITH migrated_modules AS (
    SELECT *
    FROM skolenza."Module"
    WHERE "Remark" LIKE 'Skolenza migration of %'
       OR "ModuleCode" IN ('STAFF_CATEGORIES', 'STAFF_FUNCTIONS')
), expected AS (
    SELECT 48::bigint AS module_count, 223::bigint AS mapping_count, 8::bigint AS institution_type_count
), actual AS (
    SELECT
        (SELECT count(*) FROM migrated_modules) AS module_count,
        (SELECT count(*)
         FROM skolenza."ModuleOperationMapping" mm
         JOIN migrated_modules m ON m."Id" = mm."ModuleId"
         WHERE mm."IsActive" = TRUE) AS mapping_count,
        (SELECT count(*)
         FROM skolenza."InstitutionType"
         WHERE "Remark" LIKE 'InstitutionType code: %'
           AND "IsActive" = TRUE
           AND "IsSoftDeted" IS NOT TRUE) AS institution_type_count
)
SELECT
    actual.*,
    actual.module_count = expected.module_count
      AND actual.mapping_count = expected.mapping_count
      AND actual.institution_type_count = expected.institution_type_count AS verification_passed
FROM actual
CROSS JOIN expected;

DO $$
DECLARE
    module_count bigint;
    mapping_count bigint;
    institution_type_count bigint;
BEGIN
    SELECT count(*) INTO module_count
    FROM skolenza."Module"
    WHERE "Remark" LIKE 'Skolenza migration of %'
       OR "ModuleCode" IN ('STAFF_CATEGORIES', 'STAFF_FUNCTIONS');

    SELECT count(*) INTO mapping_count
    FROM skolenza."ModuleOperationMapping" mm
    JOIN skolenza."Module" m ON m."Id" = mm."ModuleId"
    WHERE (m."Remark" LIKE 'Skolenza migration of %'
       OR m."ModuleCode" IN ('STAFF_CATEGORIES', 'STAFF_FUNCTIONS'))
      AND mm."IsActive" = TRUE;

    SELECT count(*) INTO institution_type_count
    FROM skolenza."InstitutionType"
    WHERE "Remark" LIKE 'InstitutionType code: %'
      AND "IsActive" = TRUE
      AND "IsSoftDeted" IS NOT TRUE;

    IF module_count <> 48 OR mapping_count <> 223 OR institution_type_count <> 8 THEN
        RAISE EXCEPTION
            'Institution/Staff catalogue verification failed: modules %, mappings %, institution types %',
            module_count, mapping_count, institution_type_count;
    END IF;
END $$;
