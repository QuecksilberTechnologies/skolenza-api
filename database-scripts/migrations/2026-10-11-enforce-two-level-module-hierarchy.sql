BEGIN;

LOCK TABLE skolenza."Module" IN SHARE ROW EXCLUSIVE MODE;

-- PageName is an immutable public identifier during normal application work.
-- This controlled identity merge must transfer it from the duplicate leaf to
-- the canonical Institution child before the duplicate row can be removed.
ALTER TABLE skolenza."Module" DISABLE TRIGGER "TR_Module_PageNameImmutable";

CREATE TEMP TABLE module_merge
(
    old_code varchar(50) PRIMARY KEY,
    canonical_code varchar(50) NOT NULL UNIQUE
) ON COMMIT DROP;

INSERT INTO module_merge (old_code, canonical_code) VALUES
('DEPARTMENT', 'INSTITUTION_DEPARTMENTS'),
('STAFF_DESIGNATIONS', 'INSTITUTION_STAFF_DESIGNATIONS'),
('ACCESS_ROLES', 'INSTITUTION_ACCESS');

DO $$
DECLARE
    missing_codes text;
BEGIN
    SELECT string_agg(required.code, ', ' ORDER BY required.code)
    INTO missing_codes
    FROM (
        SELECT old_code AS code FROM module_merge
        UNION ALL
        SELECT canonical_code FROM module_merge
    ) required
    LEFT JOIN skolenza."Module" module
        ON upper(btrim(module."ModuleCode")) = required.code
    WHERE module."Id" IS NULL;

    IF missing_codes IS NOT NULL THEN
        RAISE EXCEPTION 'Two-level module migration is missing required codes: %', missing_codes;
    END IF;
END $$;

CREATE TEMP TABLE module_merge_ids ON COMMIT DROP AS
SELECT old_module."Id" AS old_id,
       canonical_module."Id" AS canonical_id,
       merge.old_code,
       merge.canonical_code
FROM module_merge merge
JOIN skolenza."Module" old_module
    ON upper(btrim(old_module."ModuleCode")) = merge.old_code
JOIN skolenza."Module" canonical_module
    ON upper(btrim(canonical_module."ModuleCode")) = merge.canonical_code;

CREATE TEMP TABLE module_merge_source ON COMMIT DROP AS
SELECT ids.*,
       source."ModuleName" AS source_module_name,
       source."DisplayName" AS source_display_name,
       source."URLPath" AS source_url_path,
       source."PageName" AS source_page_name,
       source."ImageIconWeb" AS source_image_icon_web,
       source."ImageIconMobile" AS source_image_icon_mobile
FROM module_merge_ids ids
JOIN skolenza."Module" source ON source."Id" = ids.old_id;

DO $$
DECLARE
    duplicate_reference text;
BEGIN
    SELECT format('%s:%s', relation_name, canonical_code)
    INTO duplicate_reference
    FROM (
        SELECT 'ModuleOperationMapping' AS relation_name, ids.canonical_code
        FROM module_merge_ids ids
        JOIN skolenza."ModuleOperationMapping" old_row ON old_row."ModuleId" = ids.old_id
        JOIN skolenza."ModuleOperationMapping" new_row
          ON new_row."ModuleId" = ids.canonical_id
         AND new_row."OperationId" = old_row."OperationId"
        UNION ALL
        SELECT 'PlanModuleMapping', ids.canonical_code
        FROM module_merge_ids ids
        JOIN skolenza."PlanModuleMapping" old_row ON old_row."ModuleId" = ids.old_id
        JOIN skolenza."PlanModuleMapping" new_row
          ON new_row."ModuleId" = ids.canonical_id
         AND new_row."SubscriptionPlanId" = old_row."SubscriptionPlanId"
    ) collision
    LIMIT 1;

    IF duplicate_reference IS NOT NULL THEN
        RAISE EXCEPTION 'Module merge would create a duplicate reference: %', duplicate_reference;
    END IF;
END $$;

UPDATE skolenza."ModuleOperationMapping" relation
SET "ModuleId" = ids.canonical_id
FROM module_merge_ids ids
WHERE relation."ModuleId" = ids.old_id;

UPDATE skolenza."PlanModuleMapping" relation
SET "ModuleId" = ids.canonical_id
FROM module_merge_ids ids
WHERE relation."ModuleId" = ids.old_id;

UPDATE skolenza."RoleModuleAndPermission" relation
SET "ModuleId" = ids.canonical_id
FROM module_merge_ids ids
WHERE relation."ModuleId" = ids.old_id;

UPDATE skolenza."HostRoleModuleAndPermission" relation
SET "ModuleId" = ids.canonical_id
FROM module_merge_ids ids
WHERE relation."ModuleId" = ids.old_id;

UPDATE skolenza."InstitutionEnabledModule" relation
SET "ModuleId" = ids.canonical_id
FROM module_merge_ids ids
WHERE relation."ModuleId" = ids.old_id;

UPDATE skolenza."InstitutionEnabledOperation" relation
SET "ModuleId" = ids.canonical_id
FROM module_merge_ids ids
WHERE relation."ModuleId" = ids.old_id;

UPDATE skolenza."Module" source
SET "PageName" = NULL
FROM module_merge_ids ids
WHERE source."Id" = ids.old_id;

UPDATE skolenza."Module" canonical
SET "ModuleName" = source.source_module_name,
    "DisplayName" = source.source_display_name,
    "URLPath" = source.source_url_path,
    "PageName" = source.source_page_name,
    "IsLeafNode" = true,
    "ImageIconWeb" = source.source_image_icon_web,
    "ImageIconMobile" = source.source_image_icon_mobile,
    "UpdatedById" = 1,
    "UpdatedDateTime" = CURRENT_TIMESTAMP
FROM module_merge_source source
WHERE canonical."Id" = source.canonical_id;

DELETE FROM skolenza."Module" module
USING module_merge_ids ids
WHERE module."Id" = ids.old_id;

ALTER TABLE skolenza."Module" ENABLE TRIGGER "TR_Module_PageNameImmutable";

-- AxionPro's catalogue contract is Parent header -> Child option. Any current
-- grandchild therefore belongs directly to its root header.
UPDATE skolenza."Module" grandchild
SET "ParentModuleId" = parent."ParentModuleId",
    "UpdatedById" = 1,
    "UpdatedDateTime" = CURRENT_TIMESTAMP
FROM skolenza."Module" parent
WHERE grandchild."ParentModuleId" = parent."Id"
  AND parent."ParentModuleId" IS NOT NULL;

UPDATE skolenza."Module" module
SET "IsLeafNode" = NOT EXISTS
    (SELECT 1 FROM skolenza."Module" child WHERE child."ParentModuleId" = module."Id"),
    "UpdatedById" = 1,
    "UpdatedDateTime" = CURRENT_TIMESTAMP;

CREATE OR REPLACE FUNCTION skolenza."ValidateModuleTwoLevelHierarchy"()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    parent_parent_id integer;
BEGIN
    IF NEW."ParentModuleId" IS NULL THEN
        RETURN NEW;
    END IF;

    SELECT parent."ParentModuleId"
    INTO parent_parent_id
    FROM skolenza."Module" parent
    WHERE parent."Id" = NEW."ParentModuleId";

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Parent module % does not exist.', NEW."ParentModuleId";
    END IF;

    IF parent_parent_id IS NOT NULL THEN
        RAISE EXCEPTION 'Module hierarchy supports only Parent headers and Child options.';
    END IF;

    IF NEW."Id" IS NOT NULL AND EXISTS
       (SELECT 1 FROM skolenza."Module" child WHERE child."ParentModuleId" = NEW."Id") THEN
        RAISE EXCEPTION 'A Parent header with Child options cannot itself become a Child option.';
    END IF;

    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS "TR_Module_TwoLevelHierarchy" ON skolenza."Module";
CREATE TRIGGER "TR_Module_TwoLevelHierarchy"
BEFORE INSERT OR UPDATE OF "ParentModuleId" ON skolenza."Module"
FOR EACH ROW EXECUTE FUNCTION skolenza."ValidateModuleTwoLevelHierarchy"();

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM skolenza."Module" child
        JOIN skolenza."Module" parent ON parent."Id" = child."ParentModuleId"
        WHERE parent."ParentModuleId" IS NOT NULL
    ) THEN
        RAISE EXCEPTION 'Three-level Module rows remain after migration.';
    END IF;
END $$;

COMMIT;
