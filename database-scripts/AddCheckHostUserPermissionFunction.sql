-- ================================================================
-- Author  : Deepesh Gupta
-- Company : Quecksilber Technologies
-- Role    : CEO
-- Purpose : Provides current-state Host user module-operation authorization with stale-role detection.
-- ================================================================

CREATE OR REPLACE FUNCTION skolenza."CheckHostUserPermission"(
    p_hostuserid bigint,
    p_tokenhostroleid bigint,
    p_moduleid integer,
    p_operationid integer)
RETURNS TABLE(
    "ResultCode" integer,
    "ResultKey" text,
    "CurrentHostRoleId" bigint,
    "GrantedHostRoleId" bigint)
LANGUAGE plpgsql
STABLE
AS $$
DECLARE
    v_currenthostroleid bigint;
BEGIN
    -- Reject malformed authorization inputs before reading permission state.
    IF p_hostuserid <= 0 OR
       p_tokenhostroleid <= 0 OR
       p_moduleid <= 0 OR
       p_operationid <= 0 THEN
        RETURN QUERY SELECT -2, 'INVALID_HOST_CONTEXT', NULL::bigint, NULL::bigint;
        RETURN;
    END IF;

    -- The current HostUser and HostRole state is authoritative; JWT role is only a login-time snapshot.
    SELECT host_user."HostRoleId"
    INTO v_currenthostroleid
    FROM skolenza."HostUser" AS host_user
    INNER JOIN skolenza."HostRole" AS host_role
        ON host_role."Id" = host_user."HostRoleId"
    WHERE host_user."Id" = p_hostuserid
      AND host_user."IsActive" = TRUE
      AND host_user."IsSoftDeleted" = FALSE
      AND host_role."IsActive" = TRUE
      AND host_role."IsSoftDeleted" = FALSE;

    IF v_currenthostroleid IS NULL THEN
        RETURN QUERY SELECT -2, 'INVALID_HOST_CONTEXT', NULL::bigint, NULL::bigint;
        RETURN;
    END IF;

    IF v_currenthostroleid <> p_tokenhostroleid THEN
        RETURN QUERY SELECT -1, 'AUTH_CONTEXT_CHANGED', v_currenthostroleid, NULL::bigint;
        RETURN;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM skolenza."HostRoleModuleAndPermission" AS permission
        WHERE permission."HostRoleId" = v_currenthostroleid
          AND permission."ModuleId" = p_moduleid
          AND permission."OperationId" = p_operationid
          AND permission."IsActive" = TRUE
          AND permission."IsSoftDeleted" = FALSE) THEN
        RETURN QUERY SELECT 1, 'ALLOWED', v_currenthostroleid, v_currenthostroleid;
        RETURN;
    END IF;

    RETURN QUERY SELECT 0, 'PERMISSION_DENIED', v_currenthostroleid, NULL::bigint;
END;
$$;
