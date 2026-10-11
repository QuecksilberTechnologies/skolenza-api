BEGIN;

DO $$
DECLARE
    item record;
    new_name text;
    definition text;
    definition_header text;
    definition_body text;
BEGIN
    FOR item IN
        SELECT p.oid,
               p.proname,
               pg_get_function_identity_arguments(p.oid) AS identity_arguments
        FROM pg_proc p
        JOIN pg_namespace n ON n.oid = p.pronamespace
        WHERE n.nspname = 'skolenza'
          AND p.prokind IN ('f', 'p')
          AND (
              p.proname ~* '(employee|tenant)'
              OR pg_get_functiondef(p.oid) ~* '(employee|tenant)')
        ORDER BY p.oid
    LOOP
        new_name := item.proname;
        new_name := replace(new_name, 'EmployeeType', 'StaffEmploymentType');
        new_name := replace(new_name, 'employee_type', 'staff_employment_type');
        new_name := replace(new_name, 'Employee', 'StaffMember');
        new_name := replace(new_name, 'employee', 'staff_member');
        new_name := replace(new_name, 'TenantLocation', 'Campus');
        new_name := replace(new_name, 'tenant_location', 'campus');
        new_name := replace(new_name, 'Tenant', 'Institution');
        new_name := replace(new_name, 'tenant', 'institution');

        IF new_name <> item.proname THEN
            EXECUTE format(
                'ALTER FUNCTION skolenza.%I(%s) RENAME TO %I',
                item.proname,
                item.identity_arguments,
                new_name);
        END IF;

        definition := pg_get_functiondef(item.oid);
        definition_header := split_part(definition, 'AS $function$', 1);
        definition_body := split_part(definition, 'AS $function$', 2);
        definition_body := replace(definition_body, 'EmployeesChangedTypeHistory', 'StaffMembersChangedEmploymentTypeHistory');
        definition_body := replace(definition_body, 'EmployeeType', 'StaffEmploymentType');
        definition_body := replace(definition_body, 'Employees', 'StaffMembers');
        definition_body := replace(definition_body, 'Employee', 'StaffMember');
        definition_body := replace(definition_body, 'TenantLocation', 'Campus');
        definition_body := replace(definition_body, 'TenantIndustry', 'InstitutionType');
        definition_body := replace(definition_body, 'Tenant', 'Institution');
        definition := definition_header || 'AS $function$' || definition_body;
        EXECUTE definition;
    END LOOP;
END $$;

COMMIT;
