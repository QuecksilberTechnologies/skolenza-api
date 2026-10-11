-- Preserves existing holiday rows while renaming the table and database objects.

BEGIN;

DO $migration$
BEGIN
    IF to_regclass('skolenza."Holiday"') IS NOT NULL
       AND to_regclass('skolenza."OrganizationHolidayCalendar"') IS NOT NULL THEN
        RAISE EXCEPTION 'Both skolenza.Holiday and skolenza.OrganizationHolidayCalendar exist; resolve this ambiguity before migration.';
    END IF;

    IF to_regclass('skolenza."Holiday"') IS NULL THEN
        IF to_regclass('skolenza."OrganizationHolidayCalendar"') IS NULL THEN
            RAISE EXCEPTION 'Neither skolenza.Holiday nor skolenza.OrganizationHolidayCalendar exists.';
        END IF;

        ALTER TABLE skolenza."OrganizationHolidayCalendar" RENAME TO "Holiday";
    END IF;
END
$migration$;

ALTER TABLE skolenza."Holiday"
    ADD COLUMN IF NOT EXISTS "Icon" character varying(100);

DO $constraints$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'PK__Organiza__3214EC077FBA239C' AND conrelid = 'skolenza."Holiday"'::regclass) THEN
        ALTER TABLE skolenza."Holiday" RENAME CONSTRAINT "PK__Organiza__3214EC077FBA239C" TO "PK_Holiday";
    END IF;

    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_OrganizationHolidayCalendar_Institution' AND conrelid = 'skolenza."Holiday"'::regclass) THEN
        ALTER TABLE skolenza."Holiday" RENAME CONSTRAINT "FK_OrganizationHolidayCalendar_Institution" TO "FK_Holiday_Institution";
    END IF;

    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_OrganizationHolidayCalendar_Campus' AND conrelid = 'skolenza."Holiday"'::regclass) THEN
        ALTER TABLE skolenza."Holiday" RENAME CONSTRAINT "FK_OrganizationHolidayCalendar_Campus" TO "FK_Holiday_Campus";
    END IF;

    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'OrganizationHolidayCalendar_Id_not_null' AND conrelid = 'skolenza."Holiday"'::regclass) THEN
        ALTER TABLE skolenza."Holiday" RENAME CONSTRAINT "OrganizationHolidayCalendar_Id_not_null" TO "Holiday_Id_not_null";
    END IF;

    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'OrganizationHolidayCalendar_HolidayName_not_null' AND conrelid = 'skolenza."Holiday"'::regclass) THEN
        ALTER TABLE skolenza."Holiday" RENAME CONSTRAINT "OrganizationHolidayCalendar_HolidayName_not_null" TO "Holiday_HolidayName_not_null";
    END IF;

    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'OrganizationHolidayCalendar_HolidayDate_not_null' AND conrelid = 'skolenza."Holiday"'::regclass) THEN
        ALTER TABLE skolenza."Holiday" RENAME CONSTRAINT "OrganizationHolidayCalendar_HolidayDate_not_null" TO "Holiday_HolidayDate_not_null";
    END IF;

    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'OrganizationHolidayCalendar_IsOptional_not_null' AND conrelid = 'skolenza."Holiday"'::regclass) THEN
        ALTER TABLE skolenza."Holiday" RENAME CONSTRAINT "OrganizationHolidayCalendar_IsOptional_not_null" TO "Holiday_IsOptional_not_null";
    END IF;

    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'OrganizationHolidayCalendar_CampusId_not_null' AND conrelid = 'skolenza."Holiday"'::regclass) THEN
        ALTER TABLE skolenza."Holiday" RENAME CONSTRAINT "OrganizationHolidayCalendar_CampusId_not_null" TO "Holiday_CampusId_not_null";
    END IF;
END
$constraints$;

DO $sequence$
BEGIN
    IF to_regclass('skolenza."OrganizationHolidayCalendar_Id_seq"') IS NOT NULL
       AND to_regclass('skolenza."Holiday_Id_seq"') IS NULL THEN
        ALTER SEQUENCE skolenza."OrganizationHolidayCalendar_Id_seq" RENAME TO "Holiday_Id_seq";
    END IF;
END
$sequence$;

DO $indexes$
BEGIN
    IF to_regclass('skolenza."IX_OrganizationHolidayCalendar_Location_Date"') IS NOT NULL
       AND to_regclass('skolenza."IX_Holiday_Location_Date"') IS NULL THEN
        ALTER INDEX skolenza."IX_OrganizationHolidayCalendar_Location_Date" RENAME TO "IX_Holiday_Location_Date";
    END IF;

    IF to_regclass('skolenza."UX_OrganizationHolidayCalendar_Institution_Location_Date_NotDeleted"') IS NOT NULL
       AND to_regclass('skolenza."UX_Holiday_Institution_Location_Date_NotDeleted"') IS NULL THEN
        ALTER INDEX skolenza."UX_OrganizationHolidayCalendar_Institution_Location_Date_NotDeleted"
            RENAME TO "UX_Holiday_Institution_Location_Date_NotDeleted";
    END IF;
END
$indexes$;

COMMIT;
