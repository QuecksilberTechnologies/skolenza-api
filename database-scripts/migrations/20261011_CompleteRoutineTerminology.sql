BEGIN;

DO $$
BEGIN
    IF to_regprocedure('skolenza."ValidateActiveUserCrendentialOnly"(text)') IS NOT NULL
       AND to_regprocedure('skolenza."ValidateActiveUserCredentialOnly"(text)') IS NULL THEN
        ALTER FUNCTION skolenza."ValidateActiveUserCrendentialOnly"(text)
            RENAME TO "ValidateActiveUserCredentialOnly";
    END IF;
END
$$;

DO $$
DECLARE
    routine_oid oid;
    routine_definition text;
BEGIN
    FOR routine_oid IN
        SELECT p.oid
        FROM pg_proc p
        INNER JOIN pg_namespace n ON n.oid = p.pronamespace
        WHERE n.nspname = 'skolenza'
          AND p.prokind = 'f'
          AND p.proname IN
          (
              'SyncInstitutionModulesAndOperations',
              'SyncNewOperationsForExistingModules',
              'ValidateActiveUserCredentialOnly',
              'prevent_staff_employment_type_owner_change',
              'validate_staff_employment_type_institution'
          )
    LOOP
        routine_definition := pg_get_functiondef(routine_oid);
        routine_definition := replace(routine_definition, 'All active tenants synced successfully.', 'All active institutions synced successfully.');
        routine_definition := replace(routine_definition, 'across tenants successfully', 'across institutions successfully');
        routine_definition := replace(routine_definition, 'v_employeeid', 'v_staffmemberid');
        routine_definition := replace(routine_definition, 'tenant_id', 'institution_id');
        routine_definition := replace(routine_definition, 'type_tenant', 'type_institution');
        routine_definition := replace(routine_definition, 'between tenants', 'between institutions');
        routine_definition := replace(routine_definition, 'same tenant', 'same institution');
        EXECUTE routine_definition;
    END LOOP;
END
$$;

COMMIT;
