BEGIN;

CREATE TEMP TABLE routine_parameter_rename_work
(
    oid_value oid,
    routine_name text,
    identity_arguments text,
    definition text
) ON COMMIT DROP;

INSERT INTO routine_parameter_rename_work
SELECT p.oid,
       p.proname,
       pg_get_function_identity_arguments(p.oid),
       replace(
           replace(pg_get_functiondef(p.oid), 'p_tenantid', 'p_institutionid'),
           'p_employeeid',
           'p_staffmemberid')
FROM pg_proc p
JOIN pg_namespace n ON n.oid = p.pronamespace
WHERE n.nspname = 'skolenza'
  AND p.prokind = 'f'
  AND pg_get_function_identity_arguments(p.oid) ~* '(tenant|employee)';

DO $$
DECLARE
    item record;
BEGIN
    FOR item IN SELECT * FROM routine_parameter_rename_work ORDER BY oid_value
    LOOP
        EXECUTE format(
            'DROP FUNCTION skolenza.%I(%s)',
            item.routine_name,
            item.identity_arguments);
        EXECUTE item.definition;
    END LOOP;
END $$;

COMMIT;
