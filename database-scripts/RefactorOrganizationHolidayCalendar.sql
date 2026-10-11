-- Refactors holidays to use a Campus FK and a local business date.

BEGIN;

DO $migration$
DECLARE
    existing_rows bigint;
BEGIN
    IF to_regclass('skolenza."OrganizationHolidayCalendar"') IS NULL THEN
        RAISE EXCEPTION 'skolenza.OrganizationHolidayCalendar does not exist.';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'skolenza'
          AND table_name = 'OrganizationHolidayCalendar'
          AND column_name = 'CampusId'
    ) THEN
        SELECT count(*)
        INTO existing_rows
        FROM skolenza."OrganizationHolidayCalendar";

        IF existing_rows > 0 THEN
            RAISE EXCEPTION
                'Migration stopped: % holiday rows require an explicit Campus mapping.',
                existing_rows;
        END IF;

        ALTER TABLE skolenza."OrganizationHolidayCalendar"
            ADD COLUMN "CampusId" bigint NOT NULL;
    END IF;

    SELECT count(*)
    INTO existing_rows
    FROM skolenza."OrganizationHolidayCalendar"
    WHERE "CampusId" IS NULL;

    IF existing_rows > 0 THEN
        RAISE EXCEPTION
            'Migration stopped: % holiday rows require an explicit Campus mapping.',
            existing_rows;
    END IF;

    ALTER TABLE skolenza."OrganizationHolidayCalendar"
        ALTER COLUMN "CampusId" SET NOT NULL;
END
$migration$;

DO $holiday_date$
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'skolenza'
          AND table_name = 'OrganizationHolidayCalendar'
          AND column_name = 'HolidayDate'
          AND data_type <> 'date'
    ) THEN
        ALTER TABLE skolenza."OrganizationHolidayCalendar"
            ALTER COLUMN "HolidayDate" TYPE date
            USING ("HolidayDate" AT TIME ZONE 'UTC')::date;
    END IF;
END
$holiday_date$;

ALTER TABLE skolenza."OrganizationHolidayCalendar"
    DROP COLUMN IF EXISTS "CountryCode",
    DROP COLUMN IF EXISTS "StateCode",
    DROP COLUMN IF EXISTS "HolidayYear",
    DROP COLUMN IF EXISTS "Remark";

DO $constraint$
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'FK_OrganizationHolidayCalendar_Campus'
          AND conrelid = 'skolenza."OrganizationHolidayCalendar"'::regclass
    ) THEN
        ALTER TABLE skolenza."OrganizationHolidayCalendar"
            ADD CONSTRAINT "FK_OrganizationHolidayCalendar_Campus"
            FOREIGN KEY ("CampusId")
            REFERENCES skolenza."Campus" ("Id")
            ON DELETE RESTRICT;
    END IF;
END
$constraint$;

CREATE INDEX IF NOT EXISTS "IX_OrganizationHolidayCalendar_Location_Date"
    ON skolenza."OrganizationHolidayCalendar" ("CampusId", "HolidayDate");

COMMIT;
