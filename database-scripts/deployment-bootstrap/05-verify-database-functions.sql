-- Read-only verification for copied database functions and Host authorization.
DO $$
DECLARE
    remaining integer;
    result_code integer;
BEGIN
    SELECT count(*) INTO remaining
    FROM pg_proc p
    JOIN pg_namespace n ON n.oid = p.pronamespace
    WHERE n.nspname = 'skolenza'
      AND p.prokind = 'f'
      AND pg_get_functiondef(p.oid) ILIKE '%axionpro.%';

    IF remaining <> 0 THEN
        RAISE EXCEPTION '% Skolenza function(s) still reference the AxionPro schema.', remaining;
    END IF;

    SELECT "ResultCode" INTO result_code
    FROM skolenza."CheckHostUserPermission"(1, 1, 33, 4);
    IF result_code <> 1 THEN RAISE EXCEPTION 'Expected ALLOWED, got %.', result_code; END IF;

    SELECT "ResultCode" INTO result_code
    FROM skolenza."CheckHostUserPermission"(1, 999, 33, 4);
    IF result_code <> -1 THEN RAISE EXCEPTION 'Expected AUTH_CONTEXT_CHANGED, got %.', result_code; END IF;

    SELECT "ResultCode" INTO result_code
    FROM skolenza."CheckHostUserPermission"(1, 1, 33, 999);
    IF result_code <> 0 THEN RAISE EXCEPTION 'Expected PERMISSION_DENIED, got %.', result_code; END IF;

    SELECT "ResultCode" INTO result_code
    FROM skolenza."CheckHostUserPermission"(0, 1, 33, 4);
    IF result_code <> -2 THEN RAISE EXCEPTION 'Expected INVALID_HOST_CONTEXT, got %.', result_code; END IF;
END $$;

SELECT 'PASS' AS "DatabaseFunctionVerification";
