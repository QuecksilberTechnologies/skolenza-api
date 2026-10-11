BEGIN;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM skolenza."Holiday"
        WHERE "IsSoftDeleted" IS DISTINCT FROM TRUE
        GROUP BY "InstitutionId", "CampusId", "HolidayDate"
        HAVING count(*) > 1
    ) THEN
        RAISE EXCEPTION 'Resolve duplicate non-deleted institution/location/date holidays before adding the unique index';
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS
    "UX_Holiday_Institution_Location_Date_NotDeleted"
ON skolenza."Holiday"
    ("InstitutionId", "CampusId", "HolidayDate")
WHERE "IsSoftDeleted" IS DISTINCT FROM TRUE;

COMMIT;
