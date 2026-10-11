-- Institution-owned StaffEmploymentTypes. Apply after AddDurableMasterBulkImport.sql.
-- Legacy null-institution rows remain as historical/bootstrap templates, never institution API options.
BEGIN;
LOCK TABLE skolenza."StaffEmploymentType", skolenza."StaffMember",
    skolenza."StaffMembersChangedTypeHistory", skolenza."StaffEmploymentTypeBasicMenu"
    IN SHARE ROW EXCLUSIVE MODE;

SELECT setval(
    pg_get_serial_sequence('skolenza."StaffEmploymentType"','Id'),
    GREATEST(
        COALESCE((SELECT max("Id") FROM skolenza."StaffEmploymentType"), 0),
        COALESCE(pg_sequence_last_value(pg_get_serial_sequence('skolenza."StaffEmploymentType"','Id')::regclass), 0),
        1),
    true);
SELECT setval(
    pg_get_serial_sequence('skolenza."StaffEmploymentTypeBasicMenu"','Id'),
    GREATEST(
        COALESCE((SELECT max("Id") FROM skolenza."StaffEmploymentTypeBasicMenu"), 0),
        COALESCE(pg_sequence_last_value(pg_get_serial_sequence('skolenza."StaffEmploymentTypeBasicMenu"','Id')::regclass), 0),
        1),
    true);

ALTER TABLE skolenza."StaffEmploymentType"
    ADD COLUMN IF NOT EXISTS "InstitutionId" bigint REFERENCES skolenza."Institution"("Id");
CREATE UNIQUE INDEX IF NOT EXISTS "UX_StaffEmploymentType_Institution_Name_Live"
    ON skolenza."StaffEmploymentType" ("InstitutionId", lower(btrim("TypeName")))
    WHERE "InstitutionId" IS NOT NULL AND "IsSoftDeleted" IS NOT TRUE;

CREATE TEMP TABLE bulk_type_migration
(
    "InstitutionId" bigint,
    "OldId" integer,
    "NewId" integer,
    PRIMARY KEY ("InstitutionId", "OldId")
) ON COMMIT DROP;

INSERT INTO bulk_type_migration ("InstitutionId", "OldId")
SELECT DISTINCT usage."InstitutionId", usage."TypeId"
FROM
(
    SELECT "InstitutionId", "StaffEmploymentTypeId" AS "TypeId"
    FROM skolenza."StaffMember"
    UNION
    SELECT staffMember."InstitutionId", history."OldStaffEmploymentTypeId"
    FROM skolenza."StaffMembersChangedTypeHistory" history
    JOIN skolenza."StaffMember" staffMember ON staffMember."Id" = history."StaffMemberId"
    UNION
    SELECT staffMember."InstitutionId", history."NewStaffEmploymentTypeId"
    FROM skolenza."StaffMembersChangedTypeHistory" history
    JOIN skolenza."StaffMember" staffMember ON staffMember."Id" = history."StaffMemberId"
) usage
JOIN skolenza."StaffEmploymentType" source
    ON source."Id" = usage."TypeId" AND source."InstitutionId" IS NULL
WHERE usage."InstitutionId" IS NOT NULL;

DO $migration$
DECLARE
    mapping record;
    new_id integer;
BEGIN
    FOR mapping IN
        SELECT * FROM bulk_type_migration ORDER BY "InstitutionId", "OldId"
    LOOP
        INSERT INTO skolenza."StaffEmploymentType"
        (
            "InstitutionId", "TypeName", "Description", "Remark", "IsActive",
            "AddedById", "AddedDateTime", "UpdatedById", "UpdatedDateTime",
            "IsSoftDeleted", "SoftDeletedById", "SoftDeletedDateTime"
        )
        SELECT
            mapping."InstitutionId", "TypeName", "Description", "Remark", "IsActive",
            "AddedById", "AddedDateTime", "UpdatedById", "UpdatedDateTime",
            "IsSoftDeleted", "SoftDeletedById", "SoftDeletedDateTime"
        FROM skolenza."StaffEmploymentType"
        WHERE "Id" = mapping."OldId"
        RETURNING "Id" INTO new_id;

        UPDATE bulk_type_migration
        SET "NewId" = new_id
        WHERE "InstitutionId" = mapping."InstitutionId" AND "OldId" = mapping."OldId";

        INSERT INTO skolenza."StaffEmploymentTypeBasicMenu"
        (
            "BasicMenuId", "StaffEmploymentTypeId", "ForPlatform", "IsMenuDisplayInUI",
            "IsDisplayable", "IsActive", "HasAccess", "AddedById", "AddedDateTime",
            "UpdatedById", "UpdatedDateTime"
        )
        SELECT
            "BasicMenuId", new_id, "ForPlatform", "IsMenuDisplayInUI",
            "IsDisplayable", "IsActive", "HasAccess", "AddedById", "AddedDateTime",
            "UpdatedById", "UpdatedDateTime"
        FROM skolenza."StaffEmploymentTypeBasicMenu"
        WHERE "StaffEmploymentTypeId" = mapping."OldId";
    END LOOP;
END $migration$;

UPDATE skolenza."StaffMember" staffMember
SET "StaffEmploymentTypeId" = mapping."NewId"
FROM bulk_type_migration mapping
WHERE staffMember."InstitutionId" = mapping."InstitutionId"
  AND staffMember."StaffEmploymentTypeId" = mapping."OldId";

UPDATE skolenza."StaffMembersChangedTypeHistory" history
SET "OldStaffEmploymentTypeId" = mapping."NewId"
FROM bulk_type_migration mapping, skolenza."StaffMember" staffMember
WHERE staffMember."Id" = history."StaffMemberId"
  AND staffMember."InstitutionId" = mapping."InstitutionId"
  AND history."OldStaffEmploymentTypeId" = mapping."OldId";

UPDATE skolenza."StaffMembersChangedTypeHistory" history
SET "NewStaffEmploymentTypeId" = mapping."NewId"
FROM bulk_type_migration mapping, skolenza."StaffMember" staffMember
WHERE staffMember."Id" = history."StaffMemberId"
  AND staffMember."InstitutionId" = mapping."InstitutionId"
  AND history."NewStaffEmploymentTypeId" = mapping."OldId";

CREATE OR REPLACE FUNCTION skolenza.prevent_staffMember_type_owner_change()
RETURNS trigger LANGUAGE plpgsql AS $body$
BEGIN
    IF OLD."InstitutionId" IS DISTINCT FROM NEW."InstitutionId" THEN
        RAISE EXCEPTION 'StaffEmploymentType ownership cannot be moved between institutions' USING ERRCODE = '23514';
    END IF;
    RETURN NEW;
END $body$;

DROP TRIGGER IF EXISTS "TR_StaffEmploymentType_OwnerImmutable" ON skolenza."StaffEmploymentType";
CREATE TRIGGER "TR_StaffEmploymentType_OwnerImmutable"
BEFORE UPDATE OF "InstitutionId" ON skolenza."StaffEmploymentType"
FOR EACH ROW EXECUTE FUNCTION skolenza.prevent_staffMember_type_owner_change();

CREATE OR REPLACE FUNCTION skolenza.validate_staffMember_type_institution()
RETURNS trigger LANGUAGE plpgsql AS $body$
DECLARE
    institution_id bigint;
    type_ids integer[];
    type_id integer;
    type_institution bigint;
BEGIN
    IF TG_TABLE_NAME = 'StaffMembersChangedTypeHistory' THEN
        SELECT "InstitutionId" INTO institution_id
        FROM skolenza."StaffMember"
        WHERE "Id" = NEW."StaffMemberId";
        type_ids := ARRAY[NEW."OldStaffEmploymentTypeId", NEW."NewStaffEmploymentTypeId"];
    ELSE
        institution_id := NEW."InstitutionId";
        type_ids := ARRAY[NEW."StaffEmploymentTypeId"];
    END IF;

    FOREACH type_id IN ARRAY type_ids
    LOOP
        IF type_id IS NOT NULL THEN
            SELECT "InstitutionId" INTO type_institution
            FROM skolenza."StaffEmploymentType"
            WHERE "Id" = type_id;
            IF NOT FOUND OR type_institution IS DISTINCT FROM institution_id THEN
                RAISE EXCEPTION 'StaffEmploymentType must belong to the same institution' USING ERRCODE = '23514';
            END IF;
        END IF;
    END LOOP;
    RETURN NEW;
END $body$;

DO $triggers$
DECLARE
    table_name text;
BEGIN
    FOREACH table_name IN ARRAY ARRAY['StaffMember', 'StaffMembersChangedTypeHistory']
    LOOP
        EXECUTE format('DROP TRIGGER IF EXISTS "TR_StaffEmploymentType_Institution" ON skolenza.%I', table_name);
        EXECUTE format(
            'CREATE TRIGGER "TR_StaffEmploymentType_Institution" BEFORE INSERT OR UPDATE ON skolenza.%I '
            'FOR EACH ROW EXECUTE FUNCTION skolenza.validate_staffMember_type_institution()',
            table_name);
    END LOOP;
END $triggers$;

ALTER TABLE skolenza."BulkImportJob" DROP CONSTRAINT IF EXISTS "BulkImportJob_Master_check";
ALTER TABLE skolenza."BulkImportJob"
    ADD CONSTRAINT "BulkImportJob_Master_check" CHECK ("Master" BETWEEN 1 AND 5);
COMMIT;
