-- Removes a closed graph of legacy tables with no API, handler, repository,
-- DbSet runtime, view, trigger, or PostgreSQL routine usage.

BEGIN;

DO $cleanup$
DECLARE
    candidates text[] := ARRAY[
        'ApprovalWorkflow', 'WorkflowStep',
        'AssetHistory', 'AssetTicketTypeDetail',
        'AttendanceHistory', 'AttendanceLogs',
        'CandidateHistory',
        'DemoRequest', 'DemoRequestBiometricDetail',
        'EmailsLog',
        'InterviewFeedback', 'InterviewPanel', 'InterviewPanelMember',
        'InterviewSchedule', 'InterviewSdule',
        'LeaveTransactionLog',
        'TenderProject', 'TenderService', 'TenderServiceHistory',
        'TenderServiceProvider', 'TenderServiceSpecification',
        'TenderServiceType'
    ];
    table_name text;
    row_count bigint;
    dependency_count integer;
    drop_list text := '';
BEGIN
    FOREACH table_name IN ARRAY candidates
    LOOP
        IF to_regclass(format('skolenza.%I', table_name)) IS NULL THEN
            CONTINUE;
        END IF;

        SELECT count(*) INTO dependency_count
        FROM pg_constraint fk
        JOIN pg_class child ON child.oid = fk.conrelid
        JOIN pg_class parent ON parent.oid = fk.confrelid
        JOIN pg_namespace parent_schema ON parent_schema.oid = parent.relnamespace
        WHERE fk.contype = 'f'
          AND parent_schema.nspname = 'skolenza'
          AND parent.relname = table_name
          AND child.relname <> ALL (candidates);

        IF dependency_count > 0 THEN
            RAISE EXCEPTION
                'Cleanup stopped: skolenza.% has % retained-table dependencies.',
                table_name,
                dependency_count;
        END IF;

        SELECT count(*) INTO dependency_count
        FROM pg_trigger trigger_info
        WHERE trigger_info.tgrelid = to_regclass(format('skolenza.%I', table_name))
          AND NOT trigger_info.tgisinternal;

        IF dependency_count > 0 THEN
            RAISE EXCEPTION
                'Cleanup stopped: skolenza.% has % user triggers.',
                table_name,
                dependency_count;
        END IF;

        SELECT count(*) INTO dependency_count
        FROM pg_views view_info
        WHERE view_info.schemaname = 'skolenza'
          AND position(table_name in view_info.definition) > 0;

        IF dependency_count > 0 THEN
            RAISE EXCEPTION
                'Cleanup stopped: skolenza.% is referenced by % views.',
                table_name,
                dependency_count;
        END IF;

        SELECT count(*) INTO dependency_count
        FROM pg_proc routine
        JOIN pg_namespace routine_schema ON routine_schema.oid = routine.pronamespace
        WHERE routine_schema.nspname = 'skolenza'
          AND routine.prokind IN ('f', 'p')
          AND position(table_name in pg_get_functiondef(routine.oid)) > 0;

        IF dependency_count > 0 THEN
            RAISE EXCEPTION
                'Cleanup stopped: skolenza.% is referenced by % routines.',
                table_name,
                dependency_count;
        END IF;

        EXECUTE format('SELECT count(*) FROM skolenza.%I', table_name)
        INTO row_count;
        RAISE NOTICE 'Retiring skolenza.% (% rows; retained in full DB backup).',
            table_name,
            row_count;

        drop_list := drop_list
            || CASE WHEN drop_list = '' THEN '' ELSE ', ' END
            || format('skolenza.%I', table_name);
    END LOOP;

    IF drop_list <> '' THEN
        EXECUTE 'DROP TABLE ' || drop_list || ' RESTRICT';
    END IF;
END
$cleanup$;

COMMIT;

