-- Approved local cutover preparation, 2026-10-17.
-- Run only with the Position-only API, after a verified backup. No rows or text are rewritten.
-- This atomic, repeatable step is separate from destructive legacy-master retirement.
DO $$
DECLARE
    mapping record;
    old_exists boolean;
    new_exists boolean;
BEGIN
    FOR mapping IN
        SELECT * FROM (VALUES
            ('StaffMemberExperience', 'Designation', 'PreviousPositionTitle'),
            ('StaffMemberExperience', 'ColleagueDesignation', 'ColleaguePositionTitle'),
            ('StaffMemberWorkHistory', 'Designation', 'PreviousPositionTitle')
        ) AS columns_to_rename(table_name, old_name, new_name)
    LOOP
        SELECT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'skolenza' AND table_name = mapping.table_name
                AND column_name = mapping.old_name
        ) INTO old_exists;
        SELECT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'skolenza' AND table_name = mapping.table_name
                AND column_name = mapping.new_name
        ) INTO new_exists;

        IF old_exists AND NOT new_exists THEN
            EXECUTE format('ALTER TABLE skolenza.%I RENAME COLUMN %I TO %I',
                mapping.table_name, mapping.old_name, mapping.new_name);
        ELSIF old_exists OR NOT new_exists THEN
            RAISE EXCEPTION 'Ambiguous or missing history column pair on %: % / %',
                mapping.table_name, mapping.old_name, mapping.new_name;
        END IF;
    END LOOP;
END $$;
