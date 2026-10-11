-- Removes confirmed empty legacy tables that have no API/repository usage.
-- The entire transaction aborts if a candidate contains data or is still referenced.

BEGIN;

DO $cleanup$
DECLARE
    table_name text;
    row_exists boolean;
BEGIN
    FOREACH table_name IN ARRAY ARRAY[
        'DistrictMaster',
        'HolidayMaster',
        'NoImagePath',
        'demo',
        'dummy1'
    ]
    LOOP
        IF to_regclass(format('skolenza.%I', table_name)) IS NULL THEN
            CONTINUE;
        END IF;

        EXECUTE format(
            'SELECT EXISTS (SELECT 1 FROM skolenza.%I LIMIT 1)',
            table_name)
        INTO row_exists;

        IF row_exists THEN
            RAISE EXCEPTION
                'Cleanup stopped: skolenza.% contains data.',
                table_name;
        END IF;

        EXECUTE format('DROP TABLE skolenza.%I RESTRICT', table_name);
        RAISE NOTICE 'Dropped empty legacy table skolenza.%', table_name;
    END LOOP;
END
$cleanup$;

COMMIT;

SELECT to_regclass('skolenza."DistrictMaster"') AS district_master,
       to_regclass('skolenza."HolidayMaster"') AS holiday_master,
       to_regclass('skolenza."NoImagePath"') AS no_image_path,
       to_regclass('skolenza.demo') AS demo,
       to_regclass('skolenza.dummy1') AS dummy1;
