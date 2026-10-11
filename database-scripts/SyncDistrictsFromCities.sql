/*
  Populate skolenza."District" from the existing skolenza."City" catalog.

  Why this keeps the source City Id:
  The current campus UI requests /location/district/option but persists
  the selected value as Campus.CityId.  Keeping District.Id = City.Id
  preserves the existing client contract and guarantees that every selected
  district value is also a valid City foreign key.

  Scope:
  - Inserts one District for every City that does not already have the same Id.
  - Does not alter or delete City rows.
  - Refuses to run if District already contains data, so it cannot merge an
    unrelated district catalog by accident.

  Usage:
  1. Run as-is to see the preview result only.
  2. Change ONLY ExecuteInsert from FALSE to TRUE, then run the entire file.
*/

BEGIN;
SET LOCAL lock_timeout = '15s';
SET LOCAL statement_timeout = '120s';

CREATE TEMP TABLE district_sync_config
(
    "ExecuteInsert" boolean NOT NULL
) ON COMMIT DROP;

-- CHANGE ONLY THIS VALUE FOR THE ACTUAL INSERT.
INSERT INTO district_sync_config ("ExecuteInsert") VALUES (FALSE);

LOCK TABLE skolenza."City", skolenza."District" IN SHARE ROW EXCLUSIVE MODE;

DO $$
DECLARE
    city_count bigint;
    district_count bigint;
BEGIN
    SELECT count(*) INTO city_count FROM skolenza."City";
    SELECT count(*) INTO district_count FROM skolenza."District";

    IF city_count = 0 THEN
        RAISE EXCEPTION 'City is empty. Seed City before creating District rows.';
    END IF;

    IF district_count <> 0 THEN
        RAISE EXCEPTION
            'District already has % row(s). This script intentionally refuses to merge or overwrite existing district data.',
            district_count;
    END IF;
END;
$$;

CREATE TEMP TABLE district_sync_preview
(
    "CityCount" bigint NOT NULL,
    "DistrictCountBefore" bigint NOT NULL,
    "RowsToInsert" bigint NOT NULL
) ON COMMIT DROP;

INSERT INTO district_sync_preview ("CityCount", "DistrictCountBefore", "RowsToInsert")
SELECT
    (SELECT count(*) FROM skolenza."City"),
    (SELECT count(*) FROM skolenza."District"),
    (SELECT count(*) FROM skolenza."City" city
     LEFT JOIN skolenza."District" district ON district."Id" = city."Id"
     WHERE district."Id" IS NULL);

-- Preview output. With ExecuteInsert = FALSE this is the only data change-free result.
SELECT "CityCount", "DistrictCountBefore", "RowsToInsert"
FROM district_sync_preview;

INSERT INTO skolenza."District"
(
    "Id",
    "StateId",
    "DistrictName",
    "IsActive",
    "Remark"
)
SELECT
    city."Id",
    city."StateId",
    city."CityName",
    COALESCE(city."IsActive", TRUE),
    'Seeded one-to-one from City catalog'
FROM skolenza."City" city
CROSS JOIN district_sync_config config
WHERE config."ExecuteInsert" = TRUE
ORDER BY city."Id";

DO $$
DECLARE
    should_insert boolean;
    city_count bigint;
    district_count bigint;
    missing_count bigint;
BEGIN
    SELECT "ExecuteInsert" INTO should_insert FROM district_sync_config;

    IF should_insert THEN
        SELECT count(*) INTO city_count FROM skolenza."City";
        SELECT count(*) INTO district_count FROM skolenza."District";
        SELECT count(*) INTO missing_count
        FROM skolenza."City" city
        LEFT JOIN skolenza."District" district ON district."Id" = city."Id"
        WHERE district."Id" IS NULL;

        IF district_count <> city_count OR missing_count <> 0 THEN
            RAISE EXCEPTION
                'District sync verification failed. CityCount=%, DistrictCount=%, MissingDistricts=%',
                city_count, district_count, missing_count;
        END IF;

        PERFORM setval(
            pg_get_serial_sequence('skolenza."District"', 'Id'),
            (SELECT max("Id") FROM skolenza."District"),
            TRUE);

        RAISE NOTICE '% City rows copied to District; City.Id and District.Id are aligned.', city_count;
    ELSE
        RAISE NOTICE 'Preview only. Change ExecuteInsert to TRUE to copy City rows into District.';
    END IF;
END;
$$;

COMMIT;
