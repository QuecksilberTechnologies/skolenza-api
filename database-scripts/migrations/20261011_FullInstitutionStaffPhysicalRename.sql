BEGIN;

DO $$
DECLARE
    item record;
    new_name text;
BEGIN
    FOR item IN
        SELECT tablename AS old_name
        FROM pg_tables
        WHERE schemaname = 'skolenza'
          AND tablename ~ '(Employee|Tenant)'
        ORDER BY length(tablename) DESC
    LOOP
        new_name := item.old_name;
        new_name := replace(new_name, 'EmployeesChangedTypeHistory', 'StaffMembersChangedEmploymentTypeHistory');
        new_name := replace(new_name, 'EmployeeType', 'StaffEmploymentType');
        new_name := replace(new_name, 'PayrollEmployee', 'PayrollStaffMember');
        new_name := replace(new_name, 'Employee', 'StaffMember');
        new_name := replace(new_name, 'TenantLocation', 'Campus');
        new_name := replace(new_name, 'TenantIndustry', 'InstitutionType');
        new_name := replace(new_name, 'Tenant', 'Institution');

        IF new_name <> item.old_name THEN
            EXECUTE format('ALTER TABLE skolenza.%I RENAME TO %I', item.old_name, new_name);
        END IF;
    END LOOP;
END $$;

DO $$
DECLARE
    item record;
    new_name text;
BEGIN
    FOR item IN
        SELECT table_name, column_name
        FROM information_schema.columns
        WHERE table_schema = 'skolenza'
          AND column_name ~ '(Employee|Tenant)'
        ORDER BY table_name, ordinal_position
    LOOP
        new_name := item.column_name;
        new_name := replace(new_name, 'TenantBillingSubscriptionId', 'InstitutionBillingSubscriptionId');
        new_name := replace(new_name, 'TenantSubscriptionId', 'InstitutionSubscriptionId');
        new_name := replace(new_name, 'TenantLocationId', 'CampusId');
        new_name := replace(new_name, 'PrimaryTenantLocationId', 'PrimaryCampusId');
        new_name := replace(new_name, 'TenantDeviceId', 'InstitutionDeviceId');
        new_name := replace(new_name, 'TenantIndustryId', 'InstitutionTypeId');
        new_name := replace(new_name, 'TenantCode', 'InstitutionCode');
        new_name := replace(new_name, 'TenantEmail', 'InstitutionEmail');
        new_name := replace(new_name, 'TotalEmployees', 'TotalStaffMembers');
        new_name := replace(new_name, 'Employees', 'StaffMembers');
        new_name := replace(new_name, 'EmployeeTypeId', 'StaffEmploymentTypeId');
        new_name := replace(new_name, 'EmployeeWork', 'StaffMemberWork');
        new_name := replace(new_name, 'PayrollEmployeeId', 'PayrollStaffMemberId');
        new_name := replace(new_name, 'EmployeeIdOfCompany', 'StaffMemberIdOfInstitution');
        new_name := replace(new_name, 'EmployeeId', 'StaffMemberId');
        new_name := replace(new_name, 'Employee', 'StaffMember');
        new_name := replace(new_name, 'TenantId', 'InstitutionId');
        new_name := replace(new_name, 'Tenant', 'Institution');

        IF new_name <> item.column_name THEN
            EXECUTE format(
                'ALTER TABLE skolenza.%I RENAME COLUMN %I TO %I',
                item.table_name,
                item.column_name,
                new_name);
        END IF;
    END LOOP;
END $$;

DO $$
DECLARE
    item record;
    new_name text;
BEGIN
    FOR item IN
        SELECT c.relname AS old_name, c.relkind
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = 'skolenza'
          AND c.relkind IN ('i', 'S')
          AND c.relname ~ '(Employee|employee|Tenant|tenant)'
        ORDER BY length(c.relname) DESC
    LOOP
        new_name := item.old_name;
        new_name := replace(new_name, 'EmployeeType', 'StaffEmploymentType');
        new_name := replace(new_name, 'employee_type', 'staff_employment_type');
        new_name := replace(new_name, 'Employees', 'StaffMembers');
        new_name := replace(new_name, 'employees', 'staff_members');
        new_name := replace(new_name, 'Employee', 'StaffMember');
        new_name := replace(new_name, 'employee', 'staff_member');
        new_name := replace(new_name, 'TenantLocation', 'Campus');
        new_name := replace(new_name, 'tenant_location', 'campus');
        new_name := replace(new_name, 'Tenant', 'Institution');
        new_name := replace(new_name, 'tenant', 'institution');

        IF new_name <> item.old_name
           AND NOT EXISTS (
               SELECT 1
               FROM pg_class existing
               JOIN pg_namespace existing_namespace ON existing_namespace.oid = existing.relnamespace
               WHERE existing_namespace.nspname = 'skolenza'
                 AND existing.relname = new_name)
        THEN
            IF item.relkind = 'S' THEN
                EXECUTE format('ALTER SEQUENCE skolenza.%I RENAME TO %I', item.old_name, new_name);
            ELSE
                EXECUTE format('ALTER INDEX skolenza.%I RENAME TO %I', item.old_name, new_name);
            END IF;
        END IF;
    END LOOP;
END $$;

DO $$
DECLARE
    item record;
    new_name text;
BEGIN
    FOR item IN
        SELECT conrelid::regclass AS table_name, conname AS old_name
        FROM pg_constraint constraint_record
        JOIN pg_namespace n ON n.oid = constraint_record.connamespace
        WHERE n.nspname = 'skolenza'
          AND conname ~ '(Employee|employee|Tenant|tenant)'
    LOOP
        new_name := item.old_name;
        new_name := replace(new_name, 'EmployeeType', 'StaffEmploymentType');
        new_name := replace(new_name, 'employee_type', 'staff_employment_type');
        new_name := replace(new_name, 'Employees', 'StaffMembers');
        new_name := replace(new_name, 'employees', 'staff_members');
        new_name := replace(new_name, 'Employee', 'StaffMember');
        new_name := replace(new_name, 'employee', 'staff_member');
        new_name := replace(new_name, 'Tenant', 'Institution');
        new_name := replace(new_name, 'tenant', 'institution');

        IF new_name <> item.old_name THEN
            EXECUTE format(
                'ALTER TABLE %s RENAME CONSTRAINT %I TO %I',
                item.table_name,
                item.old_name,
                new_name);
        END IF;
    END LOOP;
END $$;

COMMIT;
