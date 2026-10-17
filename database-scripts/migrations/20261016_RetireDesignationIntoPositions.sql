-- Approved 2026-10-16: migrate existing titles without loss, then retire the legacy master.
-- Prerequisites: verified full backup, current professional/category schema and approved title mapping.
-- Run psql -v commit_changes=0 -v retire_legacy=0 -f this-file.sql for a rollback-only rehearsal.
-- Stage with commit_changes=1, retire_legacy=0 while the old API is running.
-- Set retire_legacy=1 ONLY after the Position-only API/UI cutover has passed its gates.
-- Unknown titles, ambiguous matches and conflicting staff assignments stop the entire transaction.
\set ON_ERROR_STOP on
BEGIN;
SET LOCAL lock_timeout = '10s';

ALTER TABLE skolenza."PolicyApplicability"
    ADD COLUMN IF NOT EXISTS "StaffPositionMasterId" integer;
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_PolicyApplicability_StaffPositionMaster'
        AND conrelid = 'skolenza."PolicyApplicability"'::regclass) THEN
        ALTER TABLE skolenza."PolicyApplicability"
            ADD CONSTRAINT "FK_PolicyApplicability_StaffPositionMaster"
            FOREIGN KEY ("StaffPositionMasterId") REFERENCES skolenza."StaffPositionMaster"("Id") ON DELETE RESTRICT;
    END IF;
END $$;
CREATE INDEX IF NOT EXISTS "IX_PolicyApplicability_StaffPositionMasterId"
    ON skolenza."PolicyApplicability" ("StaffPositionMasterId");

ALTER TABLE skolenza."StaffMemberManagerMapping"
    ADD COLUMN IF NOT EXISTS "StaffPositionMasterId" integer;
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_StaffMemberManagerMapping_StaffPositionMaster'
        AND conrelid = 'skolenza."StaffMemberManagerMapping"'::regclass) THEN
        ALTER TABLE skolenza."StaffMemberManagerMapping"
            ADD CONSTRAINT "FK_StaffMemberManagerMapping_StaffPositionMaster"
            FOREIGN KEY ("StaffPositionMasterId") REFERENCES skolenza."StaffPositionMaster"("Id") ON DELETE RESTRICT;
    END IF;
END $$;

SELECT to_regclass('skolenza."Designation"') IS NOT NULL AS legacy_present \gset
\if :legacy_present
CREATE TEMP TABLE position_cutover_map (
    legacy_id integer PRIMARY KEY,
    institution_id bigint NOT NULL,
    position_id integer NOT NULL
) ON COMMIT DROP;

DO $$
DECLARE
    old_title record;
    mapped_position integer;
    matching_count integer;
    family_code text;
    category_code text;
    family_id integer;
    category_id integer;
    category_type smallint;
BEGIN
    IF to_regclass('skolenza."Designation"') IS NULL THEN
        RETURN;
    END IF;
    FOR old_title IN SELECT * FROM skolenza."Designation" ORDER BY "InstitutionId", "Id" LOOP
        SELECT count(*), min("Id") INTO matching_count, mapped_position
        FROM skolenza."StaffPositionMaster"
        WHERE "InstitutionId" = old_title."InstitutionId" AND NOT "IsSoftDeleted"
          AND lower(trim("Name")) = lower(trim(old_title."DesignationName"));
        IF matching_count > 1 THEN
            RAISE EXCEPTION 'Ambiguous Position mapping for legacy title % in institution %', old_title."Id", old_title."InstitutionId";
        END IF;
        IF matching_count = 0 THEN
            CASE lower(trim(old_title."DesignationName"))
                WHEN 'accounts officer' THEN family_code := 'FINANCE_ACCOUNTS'; category_code := 'NON_TEACHING';
                WHEN 'librarian' THEN family_code := 'LIBRARY'; category_code := 'NON_TEACHING';
                WHEN 'it support executive' THEN family_code := 'INFORMATION_TECHNOLOGY'; category_code := 'NON_TEACHING';
                WHEN 'student counsellor' THEN family_code := 'LEARNER_WELLBEING'; category_code := 'SPECIALIST';
                ELSE RAISE EXCEPTION 'No approved Position mapping for legacy title % in institution %', old_title."Id", old_title."InstitutionId";
            END CASE;

            SELECT "Id", "StaffCategoryTypeId" INTO STRICT category_id, category_type
            FROM skolenza."InstitutionStaffCategoryMaster"
            WHERE "InstitutionId" = old_title."InstitutionId" AND "Code" = category_code
              AND "IsActive" AND NOT "IsSoftDeleted";

            IF family_code = 'LEARNER_WELLBEING' AND NOT EXISTS (
                SELECT 1 FROM skolenza."StaffJobFamilyMaster"
                WHERE "InstitutionId" = old_title."InstitutionId" AND "Code" = family_code AND NOT "IsSoftDeleted") THEN
                INSERT INTO skolenza."StaffJobFamilyMaster"
                    ("InstitutionId", "Code", "Name", "Description", "IsActive", "IsSystemDefined", "DisplayOrder")
                VALUES (old_title."InstitutionId", family_code, 'Student Wellbeing',
                    'Student counselling and wellbeing support.', true, false, 100);
            END IF;
            SELECT "Id" INTO STRICT family_id FROM skolenza."StaffJobFamilyMaster"
            WHERE "InstitutionId" = old_title."InstitutionId" AND "Code" = family_code
              AND "IsActive" AND NOT "IsSoftDeleted";

            INSERT INTO skolenza."StaffCategoryJobFamily"
                ("InstitutionId", "InstitutionStaffCategoryMasterId", "StaffJobFamilyMasterId")
            VALUES (old_title."InstitutionId", category_id, family_id)
            ON CONFLICT DO NOTHING;

            INSERT INTO skolenza."StaffPositionMaster"
                ("InstitutionId", "StaffJobFamilyMasterId", "StaffCategoryId", "InstitutionStaffCategoryMasterId",
                 "DepartmentId", "Code", "Name", "Description", "IsActive", "IsSystemDefined", "DisplayOrder")
            VALUES (old_title."InstitutionId", family_id, category_type, category_id, old_title."DepartmentId",
                upper(regexp_replace(trim(old_title."DesignationName"), '[^a-zA-Z0-9]+', '_', 'g')),
                old_title."DesignationName", old_title."Description", old_title."IsActive", false, 100)
            RETURNING "Id" INTO mapped_position;
        END IF;
        INSERT INTO position_cutover_map VALUES (old_title."Id", old_title."InstitutionId", mapped_position);
    END LOOP;

    IF EXISTS (
        SELECT 1 FROM skolenza."StaffMember" staff
        JOIN position_cutover_map mapping ON mapping.legacy_id = staff."DesignationId"
        JOIN skolenza."StaffPositionAssignment" assignment ON assignment."StaffMemberId" = staff."Id"
            AND assignment."IsPrimary" AND assignment."IsActive"
        WHERE assignment."StaffPositionMasterId" <> mapping.position_id
    ) THEN
        RAISE EXCEPTION 'A current staff Position conflicts with the approved legacy mapping; resolve before cutover';
    END IF;
END $$;

UPDATE skolenza."PolicyApplicability" target
SET "StaffPositionMasterId" = mapping.position_id
FROM position_cutover_map mapping
WHERE target."DesignationId" = mapping.legacy_id AND target."InstitutionId" = mapping.institution_id
  AND target."StaffPositionMasterId" IS NULL;

UPDATE skolenza."StaffMemberManagerMapping" target
SET "StaffPositionMasterId" = mapping.position_id
FROM position_cutover_map mapping
WHERE target."DesignationId" = mapping.legacy_id AND target."InstitutionId" = mapping.institution_id
  AND target."StaffPositionMasterId" IS NULL;

CREATE TEMP TABLE created_position_assignments ON COMMIT DROP AS
WITH created AS (
    INSERT INTO skolenza."StaffPositionAssignment"
        ("InstitutionId", "StaffMemberId", "StaffPositionMasterId", "EffectiveFrom", "IsPrimary", "IsActive", "AddedById")
    SELECT staff."InstitutionId", staff."Id", mapping.position_id,
        coalesce(staff."DateOfOnBoarding", staff."AddedDateTime")::date, true, true, staff."Id"
    FROM skolenza."StaffMember" staff
    JOIN position_cutover_map mapping ON mapping.legacy_id = staff."DesignationId"
        AND mapping.institution_id = staff."InstitutionId"
    WHERE NOT EXISTS (SELECT 1 FROM skolenza."StaffPositionAssignment" existing
        WHERE existing."StaffMemberId" = staff."Id" AND existing."IsPrimary" AND existing."IsActive")
    RETURNING *
)
SELECT * FROM created;

INSERT INTO skolenza."StaffCapabilityAssignment"
    ("InstitutionId", "StaffMemberId", "StaffCapabilityMasterId", "SourcePositionAssignmentId",
     "VerificationStatus", "IsRequired", "EffectiveFrom", "AddedById")
SELECT assignment."InstitutionId", assignment."StaffMemberId", mapping."StaffCapabilityMasterId", assignment."Id",
    1, mapping."IsMandatory", assignment."EffectiveFrom", assignment."StaffMemberId"
FROM created_position_assignments assignment
JOIN skolenza."StaffPositionCapability" mapping ON mapping."StaffPositionMasterId" = assignment."StaffPositionMasterId";

INSERT INTO skolenza."StaffDutyAssignment"
    ("InstitutionId", "StaffMemberId", "StaffDutyDefinitionMasterId", "SourcePositionAssignmentId",
     "EffectiveFrom", "IsActive", "AddedById")
SELECT assignment."InstitutionId", assignment."StaffMemberId", mapping."StaffDutyDefinitionMasterId", assignment."Id",
    assignment."EffectiveFrom", true, assignment."StaffMemberId"
FROM created_position_assignments assignment
JOIN skolenza."StaffPositionDuty" mapping ON mapping."StaffPositionMasterId" = assignment."StaffPositionMasterId";

SELECT legacy_id, institution_id, position_id FROM position_cutover_map ORDER BY institution_id, legacy_id;
SELECT count(*) AS created_primary_assignments FROM created_position_assignments;

\if :retire_legacy
-- Deliberately no CASCADE: an unexpected dependency must abort rather than disappear.
CREATE TEMP TABLE retired_designation_module_ids ON COMMIT DROP AS
SELECT "Id"
FROM skolenza."Module"
WHERE "ModuleCode" IN
(
    'DESIGNATION',
    'INSTITUTION_DESIGNATION',
    'INSTITUTION_DESIGNATIONS',
    'INSTITUTION_STAFF_DESIGNATIONS',
    'STAFF_DESIGNATIONS',
    'BULK_DESIGNATIONS'
);

DELETE FROM skolenza."InstitutionEnabledOperation"
WHERE "ModuleId" IN (SELECT "Id" FROM retired_designation_module_ids);
DELETE FROM skolenza."InstitutionEnabledModule"
WHERE "ModuleId" IN (SELECT "Id" FROM retired_designation_module_ids);
DELETE FROM skolenza."RoleModuleAndPermission"
WHERE "ModuleId" IN (SELECT "Id" FROM retired_designation_module_ids);
DELETE FROM skolenza."HostRoleModuleAndPermission"
WHERE "ModuleId" IN (SELECT "Id" FROM retired_designation_module_ids);
DELETE FROM skolenza."PlanModuleMapping"
WHERE "ModuleId" IN (SELECT "Id" FROM retired_designation_module_ids);
DELETE FROM skolenza."ModuleOperationMapping"
WHERE "ModuleId" IN (SELECT "Id" FROM retired_designation_module_ids);
DELETE FROM skolenza."Module"
WHERE "Id" IN (SELECT "Id" FROM retired_designation_module_ids)
  AND "ParentModuleId" IS NOT NULL;
DELETE FROM skolenza."Module"
WHERE "Id" IN (SELECT "Id" FROM retired_designation_module_ids);

ALTER TABLE skolenza."StaffMember" DROP COLUMN "DesignationId";
ALTER TABLE skolenza."StaffMemberManagerMapping" DROP COLUMN "DesignationId";
ALTER TABLE skolenza."PolicyApplicability" DROP COLUMN "DesignationId";
ALTER TABLE skolenza."StaffPositionMaster" DROP COLUMN "DesignationId";
ALTER TABLE skolenza."StaffPositionMaster" ADD CONSTRAINT "CK_StaffPositionMaster_PlatformScope"
    CHECK ("InstitutionId" IS NOT NULL OR "DepartmentId" IS NULL);
-- Reuse the independently approved, idempotent history preparation step.
\ir 20261017_PreparePositionHistoryColumns.sql
DROP TABLE skolenza."Designation";
CREATE OR REPLACE FUNCTION skolenza.validate_staff_employment_type_institution()
RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE institution_id bigint; type_ids integer[]; type_id integer; type_institution bigint;
BEGIN
    IF TG_TABLE_NAME = 'StaffMembersChangedEmploymentTypeHistory' THEN
        SELECT "InstitutionId" INTO institution_id FROM skolenza."StaffMember" WHERE "Id" = NEW."StaffMemberId";
        type_ids := ARRAY[NEW."OldStaffEmploymentTypeId", NEW."NewStaffEmploymentTypeId"];
    ELSE
        institution_id := NEW."InstitutionId";
        type_ids := ARRAY[NEW."StaffEmploymentTypeId"];
    END IF;
    FOREACH type_id IN ARRAY type_ids LOOP
        IF type_id IS NOT NULL THEN
            SELECT "InstitutionId" INTO type_institution FROM skolenza."StaffEmploymentType" WHERE "Id" = type_id;
            IF NOT FOUND OR type_institution IS DISTINCT FROM institution_id THEN
                RAISE EXCEPTION 'StaffEmploymentType must belong to the same institution' USING ERRCODE = '23514';
            END IF;
        END IF;
    END LOOP;
    RETURN NEW;
END $$;
\endif
\endif

\if :commit_changes
COMMIT;
\else
ROLLBACK;
\endif
