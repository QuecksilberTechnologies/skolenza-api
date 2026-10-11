\set ON_ERROR_STOP on
BEGIN;

CREATE TEMP TABLE holiday_source (
    "TenantLocationId" bigint NOT NULL,
    "HolidayName" varchar(100) NOT NULL,
    "HolidayDate" date NOT NULL,
    "IsOptional" boolean NOT NULL,
    "Description" varchar(255),
    "Icon" varchar(100)
) ON COMMIT DROP;

\copy holiday_source ("TenantLocationId", "HolidayName", "HolidayDate", "IsOptional", "Description", "Icon") FROM 'docs/testing/holiday-calendar/live-2026-jabalpur/holidays-2026.csv' WITH (FORMAT csv, HEADER true, ENCODING 'UTF8')

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM skolenza."Campus"
        WHERE "Id" = 5 AND "TenantId" = 8
          AND "IsActive" = true AND "IsSoftDeleted" = false
    ) THEN
        RAISE EXCEPTION 'Tenant 8 active location 5 was not found';
    END IF;
    IF (SELECT count(*) FROM holiday_source) <> 31
       OR (SELECT count(DISTINCT extract(month FROM "HolidayDate")) FROM holiday_source) <> 12
       OR (SELECT count(*) FROM holiday_source WHERE "IsOptional") <> 2
       OR EXISTS (SELECT 1 FROM holiday_source WHERE "TenantLocationId" <> 5 OR extract(year FROM "HolidayDate") <> 2026)
    THEN
        RAISE EXCEPTION 'Holiday source failed count, month, optional or scope validation';
    END IF;
    IF EXISTS (
        SELECT 1 FROM holiday_source
        GROUP BY "TenantLocationId", "HolidayDate", lower("HolidayName")
        HAVING count(*) > 1
    ) THEN
        RAISE EXCEPTION 'Holiday source contains duplicates';
    END IF;
END $$;

INSERT INTO skolenza."Holiday"
    ("TenantId", "TenantLocationId", "HolidayName", "HolidayDate", "IsOptional",
     "Description", "Icon", "IsActive", "IsSoftDeleted", "AddedDateTime")
SELECT 8, src."TenantLocationId", src."HolidayName", src."HolidayDate",
       src."IsOptional", src."Description", src."Icon", true, false, now()
FROM holiday_source AS src
WHERE NOT EXISTS (
    SELECT 1 FROM skolenza."Holiday" AS existing
    WHERE existing."TenantId" = 8
      AND existing."TenantLocationId" = src."TenantLocationId"
      AND existing."HolidayDate" = src."HolidayDate"
      AND existing."IsSoftDeleted" IS DISTINCT FROM TRUE
);

COMMIT;
