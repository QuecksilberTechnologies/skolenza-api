-- Rebind copied AxionPro function bodies to the Skolenza schema.
-- Function names and parameter contracts remain unchanged for API compatibility.
BEGIN;
CREATE OR REPLACE FUNCTION skolenza."AllStaffMemberCountData"(p_institutionid bigint)
 RETURNS TABLE("TotalCount" bigint, "ActiveCount" bigint, "InActiveCount" bigint, "OnLeaveCount" integer)
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY
    SELECT
        COUNT(*)::bigint,

        COALESCE(SUM(
            CASE 
                WHEN "IsActive" = true THEN 1 
                ELSE 0 
            END
        ),0)::bigint,

        COALESCE(SUM(
            CASE 
                WHEN "IsActive" = false THEN 1 
                ELSE 0 
            END
        ),0)::bigint,

        10
    FROM skolenza."StaffMember"
    WHERE COALESCE("IsSoftDeleted", false) = false
      AND "InstitutionId" = p_institutionid;
END;
$function$
;
CREATE OR REPLACE FUNCTION skolenza."CheckHostUserPermission"(p_hostuserid bigint, p_tokenhostroleid bigint, p_moduleid integer, p_operationid integer)
 RETURNS TABLE("ResultCode" integer, "ResultKey" character varying, "CurrentHostRoleId" bigint, "GrantedHostRoleId" bigint)
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_current_host_role_id bigint;
    v_permission_exists boolean;
BEGIN

    -- ============================================================
    -- INPUT VALIDATION
    -- ============================================================

    IF p_hostuserid IS NULL
       OR p_hostuserid <= 0
       OR p_tokenhostroleid IS NULL
       OR p_tokenhostroleid <= 0
       OR p_moduleid IS NULL
       OR p_moduleid <= 0
       OR p_operationid IS NULL
       OR p_operationid <= 0
    THEN
        RETURN QUERY
        SELECT
            -2,
            'INVALID_HOST_CONTEXT'::character varying(50),
            NULL::bigint,
            NULL::bigint;

        RETURN;
    END IF;


    -- ============================================================
    -- RESOLVE CURRENT ACTIVE HOST ROLE
    -- ============================================================

    SELECT
        hu."HostRoleId"
    INTO
        v_current_host_role_id
    FROM skolenza."HostUser" hu

    INNER JOIN skolenza."HostRole" hr
        ON hr."Id" = hu."HostRoleId"

    WHERE hu."Id" = p_hostuserid

      AND COALESCE(hu."IsActive", false) = true
      AND COALESCE(hu."IsSoftDeleted", false) = false

      AND COALESCE(hr."IsActive", false) = true
      AND COALESCE(hr."IsSoftDeleted", false) = false

    LIMIT 1;


    -- ============================================================
    -- INVALID HOST CONTEXT
    -- ============================================================

    IF v_current_host_role_id IS NULL
    THEN
        RETURN QUERY
        SELECT
            -2,
            'INVALID_HOST_CONTEXT'::character varying(50),
            NULL::bigint,
            NULL::bigint;

        RETURN;
    END IF;


    -- ============================================================
    -- CHECK WHETHER JWT ROLE IS STALE
    -- ============================================================

    IF v_current_host_role_id <> p_tokenhostroleid
    THEN
        RETURN QUERY
        SELECT
            -1,
            'AUTH_CONTEXT_CHANGED'::character varying(50),
            v_current_host_role_id,
            NULL::bigint;

        RETURN;
    END IF;


    -- ============================================================
    -- CHECK MODULE + OPERATION PERMISSION
    -- ============================================================

    SELECT EXISTS
    (
        SELECT 1
        FROM skolenza."HostRoleModuleAndPermission" hrmp

        WHERE hrmp."HostRoleId" = v_current_host_role_id

          AND hrmp."ModuleId" = p_moduleid
          AND hrmp."OperationId" = p_operationid

          AND COALESCE(hrmp."IsActive", false) = true
          AND COALESCE(hrmp."IsSoftDeleted", false) = false
    )
    INTO v_permission_exists;


    -- ============================================================
    -- PERMISSION ALLOWED
    -- ============================================================

    IF v_permission_exists
    THEN
        RETURN QUERY
        SELECT
            1,
            'ALLOWED'::character varying(50),
            v_current_host_role_id,
            v_current_host_role_id;

        RETURN;
    END IF;


    -- ============================================================
    -- PERMISSION DENIED
    -- ============================================================

    RETURN QUERY
    SELECT
        0,
        'PERMISSION_DENIED'::character varying(50),
        v_current_host_role_id,
        NULL::bigint;

END;
$function$
;
CREATE OR REPLACE FUNCTION skolenza."CheckPermission"(p_roleid character varying, p_projectchildmoduledetailid integer, p_operationid integer, p_hasaccess boolean, p_isactive boolean)
 RETURNS TABLE("HasAccess" boolean)
 LANGUAGE plpgsql
AS $function$
DECLARE
    Result boolean := false;
BEGIN

    -- Check permission for any role
    IF EXISTS (
        SELECT 1
        FROM skolenza."RoleModuleAndPermission"
        WHERE "RoleId" IN (
            SELECT unnest(string_to_array(p_roleid, ','))::int
        )
        AND "ProjectChildModuleDetailId" = p_projectchildmoduledetailid
        AND "OperationId" = p_operationid
        AND "HasAccess" = p_hasaccess
        AND "IsActive" = p_isactive
    ) THEN
        Result := true;
    END IF;

    RETURN QUERY SELECT Result;

EXCEPTION
    WHEN OTHERS THEN
        RETURN QUERY SELECT false;

END;
$function$
;
CREATE OR REPLACE FUNCTION skolenza."CheckInstitutionStaffMemberPermission"(p_institutionid bigint, p_staffMemberid bigint, p_tokenroleid integer, p_moduleid integer, p_operationid integer)
 RETURNS TABLE("ResultCode" integer, "ResultKey" character varying, "CurrentPrimaryRoleId" integer, "GrantedRoleId" integer)
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_primary_role_id integer;
    v_primary_role_count integer;
    v_granted_role_id integer;
BEGIN

    ----------------------------------------------------------------
    -- Validate required authorization context.
    ----------------------------------------------------------------
    IF p_institutionid <= 0
       OR p_staffMemberid <= 0
       OR p_tokenroleid <= 0
       OR p_moduleid <= 0
       OR p_operationid <= 0
    THEN
        RETURN QUERY
        SELECT
            -2,
            'INVALID_ROLE_CONTEXT'::character varying(50),
            NULL::integer,
            NULL::integer;

        RETURN;
    END IF;


    ----------------------------------------------------------------
    -- Resolve CURRENT active Primary Role from UserRole + Role.
    --
    -- This deliberately does NOT trust the RoleId from JWT.
    ----------------------------------------------------------------
    SELECT
        COUNT(*)::integer,
        MAX(ur."RoleId")
    INTO
        v_primary_role_count,
        v_primary_role_id
    FROM skolenza."UserRole" ur

    INNER JOIN skolenza."Role" r
        ON r."Id" = ur."RoleId"

    WHERE ur."StaffMemberId" = p_staffMemberid

      AND COALESCE(ur."IsPrimaryRole", false) = true
      AND COALESCE(ur."IsActive", false) = true
      AND COALESCE(ur."IsSoftDeleted", false) = false

      AND r."InstitutionId" = p_institutionid
      AND COALESCE(r."IsActive", false) = true
      AND COALESCE(r."IsSoftDeleted", false) = false;


    ----------------------------------------------------------------
    -- Exactly one valid current Primary Role must exist.
    --
    -- 0 primary roles or multiple active primary roles means the
    -- authorization context is invalid and must fail closed.
    ----------------------------------------------------------------
    IF v_primary_role_count <> 1
    THEN
        RETURN QUERY
        SELECT
            -2,
            'INVALID_ROLE_CONTEXT'::character varying(50),
            v_primary_role_id,
            NULL::integer;

        RETURN;
    END IF;


    ----------------------------------------------------------------
    -- Detect stale JWT authorization context.
    --
    -- Example:
    --
    -- JWT RoleId        = 52 Engineering Manager
    -- Current DB RoleId = 36 StaffMember
    --
    -- Old JWT must no longer be accepted as the current role context.
    ----------------------------------------------------------------
    IF v_primary_role_id <> p_tokenroleid
    THEN
        RETURN QUERY
        SELECT
            -1,
            'AUTH_CONTEXT_CHANGED'::character varying(50),
            v_primary_role_id,
            NULL::integer;

        RETURN;
    END IF;


    ----------------------------------------------------------------
    -- Check requested Module + Operation permission against ALL
    -- CURRENT active roles:
    --
    --   Primary Role
    --        +
    --   Secondary Role(s)
    --
    -- Any valid role granting the requested permission is sufficient.
    ----------------------------------------------------------------
    SELECT ur."RoleId"
    INTO v_granted_role_id
    FROM skolenza."UserRole" ur

    INNER JOIN skolenza."Role" r
        ON r."Id" = ur."RoleId"

    INNER JOIN skolenza."RoleModuleAndPermission" rmp
        ON rmp."RoleId" = ur."RoleId"

    WHERE ur."StaffMemberId" = p_staffMemberid

      --------------------------------------------------------------
      -- Current UserRole assignment.
      --------------------------------------------------------------
      AND COALESCE(ur."IsActive", false) = true
      AND COALESCE(ur."IsSoftDeleted", false) = false

      --------------------------------------------------------------
      -- Current valid Institution Role.
      --------------------------------------------------------------
      AND r."InstitutionId" = p_institutionid
      AND COALESCE(r."IsActive", false) = true
      AND COALESCE(r."IsSoftDeleted", false) = false

      --------------------------------------------------------------
      -- Requested Module + Operation.
      --------------------------------------------------------------
      AND rmp."ModuleId" = p_moduleid
      AND rmp."OperationId" = p_operationid

      --------------------------------------------------------------
      -- Current valid permission.
      --------------------------------------------------------------
      AND COALESCE(rmp."HasAccess", false) = true
      AND COALESCE(rmp."IsActive", false) = true
      AND COALESCE(rmp."IsSoftDeleted", false) = false

    --------------------------------------------------------------
    -- If both Primary and Secondary roles grant the permission,
    -- report Primary first. Authorization itself is still union-based.
    --------------------------------------------------------------
    ORDER BY
        COALESCE(ur."IsPrimaryRole", false) DESC,
        ur."RoleId"

    LIMIT 1;


    ----------------------------------------------------------------
    -- Permission found from either Primary or Secondary Role.
    ----------------------------------------------------------------
    IF v_granted_role_id IS NOT NULL
    THEN
        RETURN QUERY
        SELECT
            1,
            'ALLOWED'::character varying(50),
            v_primary_role_id,
            v_granted_role_id;

        RETURN;
    END IF;


    ----------------------------------------------------------------
    -- Authentication/role context is valid but this operation
    -- is not permitted.
    ----------------------------------------------------------------
    RETURN QUERY
    SELECT
        0,
        'PERMISSION_DENIED'::character varying(50),
        v_primary_role_id,
        NULL::integer;

END;
$function$
;
CREATE OR REPLACE FUNCTION skolenza."GetActiveRoleModuleOperations"(p_institutionid bigint, p_roleids text)
 RETURNS TABLE("RoleId" integer, "InstitutionId" bigint, "ModuleId" integer, "ModuleName" text, "URLPath" text, "DisplayName" text, "ImageIconWeb" text, "ImageIconMobile" text, "ParentModuleId" integer, "SubModuleName" text, "MainModuleId" integer, "MainModuleName" text, "OperationId" integer, "OperationName" text, "DataViewStructureId" integer, "DisplayOn" text)
 LANGUAGE plpgsql
AS $function$
BEGIN

RETURN QUERY

WITH RoleIdTable AS (
    SELECT value::integer AS "RoleId"
    FROM unnest(string_to_array(p_roleids, ',')) value
)

SELECT
    rep."RoleId",
    COALESCE(tem."InstitutionId", p_institutionid),

    m."Id",
    m."ModuleName"::text,
    COALESCE(m."URLPath",'')::text,
    m."DisplayName"::text,
    m."ImageIconWeb"::text,
    m."ImageIconMobile"::text,
    m."ParentModuleId",

    COALESCE(sm."ModuleName",'')::text,
    COALESCE(mm."Id",0),
    COALESCE(mm."ModuleName",'')::text,

    rep."OperationId",
    op."OperationName"::text,

    mom."DataViewStructureId",
    COALESCE(dvs."DisplayOn",'')::text

FROM skolenza."RoleModuleAndPermission" rep

INNER JOIN RoleIdTable rid
    ON rid."RoleId" = rep."RoleId"

LEFT JOIN skolenza."InstitutionEnabledModule" tem
    ON tem."InstitutionId" = p_institutionid
    AND tem."ModuleId" = rep."ModuleId"
    AND tem."IsEnabled" = true

LEFT JOIN skolenza."Module" m
    ON m."Id" = rep."ModuleId"
    AND m."IsActive" = true

LEFT JOIN skolenza."Module" sm
    ON sm."Id" = m."ParentModuleId"
    AND sm."IsActive" = true

LEFT JOIN skolenza."Module" mm
    ON mm."Id" = sm."ParentModuleId"
    AND mm."IsActive" = true

LEFT JOIN skolenza."Operation" op
    ON op."Id" = rep."OperationId"
    AND op."IsActive" = true

LEFT JOIN skolenza."ModuleOperationMapping" mom
    ON mom."ModuleId" = rep."ModuleId"
    AND mom."OperationId" = rep."OperationId"
    AND mom."IsActive" = true

LEFT JOIN skolenza."DataViewStructure" dvs
    ON dvs."Id" = mom."DataViewStructureId"

WHERE rep."IsActive" = true
  AND (rep."IsSoftDeleted" = false OR rep."IsSoftDeleted" IS NULL)
  AND rep."HasAccess" = true

ORDER BY
    COALESCE(mm."Id",0),
    COALESCE(sm."Id",0),
    m."Id",
    rep."OperationId";

END;
$function$
;
CREATE OR REPLACE FUNCTION skolenza."GetCountryIdentityDocument"(p_countryid integer)
 RETURNS TABLE("CountryCode" text, "CountryName" text, "IdentityCategoryName" text, "IdentityCategoryDocumentId" integer, "DocumentName" text, "IsMandatory" boolean)
 LANGUAGE plpgsql
AS $function$
BEGIN

RETURN QUERY
SELECT
    c."CountryCode",
    c."CountryName",

    ic."Name" AS "IdentityCategoryName",

    icd."Id" AS "IdentityCategoryDocumentId",
    icd."DocumentName",

    cir."IsMandatory"

FROM skolenza."CountryIdentityRule" cir

JOIN skolenza."Country" c
    ON c."Id" = cir."CountryId"
   AND c."IsActive" = true

JOIN skolenza."IdentityCategoryDocument" icd
    ON icd."Id" = cir."IdentityCategoryDocumentId"
   AND icd."IsActive" = true

JOIN skolenza."IdentityCategory" ic
    ON ic."Id" = icd."IdentityCategoryId"
   AND ic."IsActive" = true

WHERE cir."CountryId" = p_countryid
  AND cir."IsActive" = true

ORDER BY ic."Name", icd."DocumentName";

END;
$function$
;
CREATE OR REPLACE FUNCTION skolenza."GetDetailsForPermission"(p_institutionid bigint)
 RETURNS TABLE("ParentModuleId" integer, "ModuleId" integer, "ModuleName" text, "IsLeafNode" boolean, "IsEnabled" boolean, "OperationId" integer, "OperationName" text, "IsOperationUsed" boolean, "Level" integer, "BreadCrum" text)
 LANGUAGE plpgsql
AS $function$
BEGIN

RETURN QUERY

WITH RECURSIVE

-- Step 1
"LeafModules" AS (
    SELECT DISTINCT "ModuleId"
    FROM skolenza."InstitutionEnabledOperation"
    WHERE "InstitutionId" = p_institutionid
),

-- Step 2
"AllModules" AS (
    SELECT DISTINCT "ModuleId"
    FROM skolenza."InstitutionEnabledModule"
    WHERE "InstitutionId" = p_institutionid

    UNION

    SELECT "ModuleId"
    FROM "LeafModules"
),

-- Step 3 Recursive Hierarchy
"ModuleHierarchy" AS (

    SELECT
        m."Id" AS "ModuleId",
        m."ParentModuleId",
        0 AS "Level",
        CAST(m."ModuleName" AS TEXT) AS "BreadCrum"
    FROM skolenza."Module" m
    INNER JOIN "AllModules" am ON m."Id" = am."ModuleId"
    WHERE m."ParentModuleId" IS NULL

    UNION ALL

    SELECT
        child."Id",
        child."ParentModuleId",
        parent."Level" + 1,
        parent."BreadCrum" || ' > ' || child."ModuleName"
    FROM skolenza."Module" child
    INNER JOIN "ModuleHierarchy" parent
        ON child."ParentModuleId" = parent."ModuleId"
)

SELECT
    mh."ParentModuleId",
    mh."ModuleId",
    m."ModuleName",
    m."IsLeafNode",
    tem."IsEnabled",
    teo."OperationId",
    o."OperationName",
    teo."IsOperationUsed",
    mh."Level",
    mh."BreadCrum"

FROM "ModuleHierarchy" mh

LEFT JOIN skolenza."Module" m
    ON mh."ModuleId" = m."Id"

LEFT JOIN skolenza."InstitutionEnabledModule" tem
    ON mh."ModuleId" = tem."ModuleId"
    AND tem."InstitutionId" = p_institutionid

LEFT JOIN skolenza."InstitutionEnabledOperation" teo
    ON mh."ModuleId" = teo."ModuleId"
    AND teo."InstitutionId" = p_institutionid

LEFT JOIN skolenza."Operation" o
    ON teo."OperationId" = o."Id"

ORDER BY mh."BreadCrum", teo."OperationId";

END;
$function$
;
CREATE OR REPLACE FUNCTION skolenza."GetStaffMemberCodePatternByInstitution"(p_institutionid bigint)
 RETURNS TABLE("Id" bigint, "InstitutionId" bigint, "Prefix" character varying, "IncludeYear" boolean, "IncludeMonth" boolean, "IncludeDepartment" boolean, "Separator" character varying, "RunningNumberLength" integer, "LastUsedNumber" integer, "IsActive" boolean, "AddedById" bigint, "AddedDateTime" timestamp with time zone, "UpdatedById" bigint, "UpdatedDateTime" timestamp with time zone)
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY
    SELECT
        ECP."Id",
        ECP."InstitutionId",
        ECP."Prefix",
        ECP."IncludeYear",
        ECP."IncludeMonth",
        ECP."IncludeDepartment",
        ECP."Separator",
        ECP."RunningNumberLength",
        ECP."LastUsedNumber",
        ECP."IsActive",
        ECP."AddedById",
        ECP."AddedDateTime",
        ECP."UpdatedById",
        ECP."UpdatedDateTime"
    FROM skolenza."StaffMemberCodePattern" AS ECP
    WHERE ECP."InstitutionId" = p_institutionid
      AND ECP."IsActive" = TRUE
    ORDER BY ECP."Id" DESC
    LIMIT 1;
END;
$function$
;
CREATE OR REPLACE FUNCTION skolenza."GetStaffMemberIdentityByCountryRule"(p_staffMemberid bigint, p_countryid integer, p_isactive boolean)
 RETURNS TABLE("Id" bigint, "StaffMemberId" bigint, "CountryCode" text, "CountryName" text, "IdentityCategoryName" text, "IdentityCategoryDocumentId" integer, "DocumentCode" text, "DocumentName" text, "Description" text, "IsMandatory" boolean, "StaffMemberIdentityId" bigint, "IdentityValue" text, "DocumentFileName" text, "DocumentFilePath" text, "IsVerified" boolean, "IsEditAllowed" boolean, "HasIdentityUploaded" boolean, "EffectiveFrom" date, "EffectiveTo" date, "IsActive" boolean)
 LANGUAGE plpgsql
AS $function$
BEGIN

RETURN QUERY
SELECT
    ei."Id",
    ei."StaffMemberId",

    c."CountryCode",
    c."CountryName",

    ic."Name" AS "IdentityCategoryName",

    icd."Id"   AS "IdentityCategoryDocumentId",
    icd."Code" AS "DocumentCode",
    icd."DocumentName",
    icd."Description",

    cir."IsMandatory",

    ei."Id" AS "StaffMemberIdentityId",
    ei."IdentityValue",

    ei."DocumentFileName",
    ei."DocumentFilePath",

    ei."IsVerified",
    ei."IsEditAllowed",
    ei."HasIdentityUploaded",
    ei."EffectiveFrom",
    ei."EffectiveTo",
    ei."IsActive"

FROM skolenza."CountryIdentityRule" cir

JOIN skolenza."Country" c
    ON c."Id" = cir."CountryId"
   AND c."IsActive" = true

JOIN skolenza."IdentityCategoryDocument" icd
    ON icd."Id" = cir."IdentityCategoryDocumentId"
   AND icd."IsActive" = true

JOIN skolenza."IdentityCategory" ic
    ON ic."Id" = icd."IdentityCategoryId"
   AND ic."IsActive" = true

LEFT JOIN skolenza."StaffMemberIdentity" ei
    ON ei."IdentityCategoryDocumentId" = icd."Id"
   AND ei."StaffMemberId" = p_staffMemberid
   AND ei."IsActive" = p_isactive
   AND ei."IsSoftDeleted" = false
   AND (ei."EffectiveTo" IS NULL
        OR ei."EffectiveTo" >= CURRENT_DATE)

WHERE cir."CountryId" = p_countryid
  AND cir."IsActive" = true

ORDER BY ic."Name", icd."DocumentName";

END;
$function$
;
CREATE OR REPLACE FUNCTION skolenza."GetSubscribedModuleByInstitutionId"(p_institutionid bigint)
 RETURNS TABLE("ModuleId" integer, "ModuleName" text, "DisplayName" text)
 LANGUAGE plpgsql
AS $function$
BEGIN

RETURN QUERY
SELECT 
    m."Id" AS "ModuleId",
    m."ModuleName",
    m."DisplayName"
FROM skolenza."InstitutionEnabledModule" tem
INNER JOIN skolenza."Module" m 
    ON tem."ModuleId" = m."Id"
WHERE tem."InstitutionId" = p_institutionid
  AND tem."IsEnabled" = true
  AND m."ParentModuleId" IS NULL;

END;
$function$
;
CREATE OR REPLACE FUNCTION skolenza."GetInstitutionModuleHierarchyWithOps_MinRoot"(p_institutionid bigint)
 RETURNS TABLE("ParentModuleId" bigint, "ModuleId" bigint, "ModuleName" text, "IsLeafNode" boolean, "OperationId" integer, "Operations" text, "IsOperationUsed" boolean, "Level" integer, "FullPath" text)
 LANGUAGE plpgsql
AS $function$
BEGIN

RETURN QUERY

WITH RECURSIVE

-- 1) Smallest root
"Root" AS (
    SELECT tem."ModuleId"
    FROM skolenza."InstitutionEnabledModule" tem
    WHERE tem."InstitutionId" = p_institutionid
      AND tem."ParentModuleId" IS NULL
    ORDER BY tem."ModuleId" ASC
    LIMIT 1
),

-- 2) Recursive hierarchy
"ModuleHierarchy" AS (

    -- Base (root)
    SELECT
        tem."ParentModuleId",
        tem."ModuleId",
        m."ModuleName",
        tem."IsLeafNode",
        CAST(m."ModuleName" AS TEXT) AS "FullPath",
        0 AS "Level"
    FROM skolenza."InstitutionEnabledModule" tem
    JOIN "Root" r ON r."ModuleId" = tem."ModuleId"
    JOIN skolenza."Module" m ON m."Id" = tem."ModuleId"
    WHERE tem."InstitutionId" = p_institutionid

    UNION ALL

    -- Children
    SELECT
        child."ParentModuleId",
        child."ModuleId",
        m."ModuleName",
        child."IsLeafNode",
        h."FullPath" || ' > ' || m."ModuleName",
        h."Level" + 1
    FROM skolenza."InstitutionEnabledModule" child
    JOIN skolenza."Module" m ON m."Id" = child."ModuleId"
    JOIN "ModuleHierarchy" h ON h."ModuleId" = child."ParentModuleId"
    WHERE child."InstitutionId" = p_institutionid
)

-- 3) Leaf modules operations
SELECT
    mh."ParentModuleId",
    mh."ModuleId",
    mh."ModuleName",
    mh."IsLeafNode",
    teo."OperationId",
    op."OperationName" AS "Operations",
    teo."IsOperationUsed",
    mh."Level",
    CASE
        WHEN mh."IsLeafNode" = true AND teo."OperationId" IS NOT NULL
            THEN mh."FullPath" || '/' || op."OperationName"
        ELSE mh."FullPath"
    END AS "FullPath"

FROM "ModuleHierarchy" mh

LEFT JOIN skolenza."InstitutionEnabledOperation" teo
    ON teo."InstitutionId" = p_institutionid
   AND teo."ModuleId" = mh."ModuleId"

LEFT JOIN skolenza."Operation" op
    ON op."Id" = teo."OperationId"

ORDER BY mh."FullPath", op."OperationName";

END;
$function$
;
CREATE OR REPLACE FUNCTION skolenza."InsertOrUpdateRefreshToken"(p_loginid text, p_token text, p_expirydate timestamp without time zone, p_createdbyip text)
 RETURNS TABLE("Status" integer, "ErrorMessage" text)
 LANGUAGE plpgsql
AS $function$
BEGIN

    BEGIN
        -- Check if RefreshToken exists
        IF EXISTS (
            SELECT 1
            FROM skolenza."RefreshToken"
            WHERE "LoginId" = p_loginid
        ) THEN

            -- Update existing token
            UPDATE skolenza."RefreshToken"
            SET
                "Token" = p_token,
                "ExpiryDate" = p_expirydate,
                "CreatedAt" = CURRENT_TIMESTAMP,
                "CreatedByIp" = p_createdbyip,
                "IsRevoked" = false
            WHERE "LoginId" = p_loginid;

        ELSE

            -- Insert new token
            INSERT INTO skolenza."RefreshToken"
            (
                "LoginId",
                "Token",
                "ExpiryDate",
                "CreatedAt",
                "CreatedByIp",
                "IsRevoked"
            )
            VALUES
            (
                "LoginId",
                "Token",
                "ExpiryDate",
                CURRENT_TIMESTAMP,
                "CreatedByIp",
                false
            );

        END IF;

        -- success
        RETURN QUERY SELECT 1, NULL::TEXT;

    EXCEPTION WHEN OTHERS THEN

        -- log error
        INSERT INTO skolenza."ErrorLogs"
        (
            "ErrorMessage",
            "ErrorDateTime",
            "ProcedureName"
        )
        VALUES
        (
            SQLERRM,
            CURRENT_TIMESTAMP,
            'Skolenza.InsertOrUpdateRefreshToken'
        );

        RETURN QUERY SELECT 0, SQLERRM;

    END;

END;
$function$
;
CREATE OR REPLACE FUNCTION skolenza."SyncNewOperationsForExistingModules"()
 RETURNS TABLE("OperationsInserted" integer, "StatusMessage" text)
 LANGUAGE plpgsql
AS $function$
DECLARE
    OperationsInserted INT := 0;
BEGIN

    -- Insert missing operations
    INSERT INTO skolenza."InstitutionEnabledOperation"
    (
        "InstitutionId",
        "ModuleId",
        "OperationId",
        "IsEnabled",
        "AddedById",
        "AddedDateTime"
    )
    SELECT
        TEM."InstitutionId",
        MOM."ModuleId",
        MOM."OperationId",
        false,
        TEM."InstitutionId",
        CURRENT_TIMESTAMP
    FROM skolenza."InstitutionEnabledModule" TEM
    INNER JOIN skolenza."ModuleOperationMapping" MOM
        ON TEM."ModuleId" = MOM."ModuleId"
       AND MOM."IsActive" = true
    INNER JOIN skolenza."Operation" OP
        ON OP."Id" = MOM."OperationId"
       AND OP."IsActive" = true
    WHERE NOT EXISTS (
        SELECT 1
        FROM skolenza."InstitutionEnabledOperation" TEO
        WHERE TEO."InstitutionId" = TEM."InstitutionId"
          AND TEO."ModuleId" = MOM."ModuleId"
          AND TEO."OperationId" = MOM."OperationId"
    );

    -- get number of rows inserted
    GET DIAGNOSTICS OperationsInserted = ROW_COUNT;

    RETURN QUERY
    SELECT
        OperationsInserted,
        CASE
            WHEN OperationsInserted = 0
                THEN 'No new operations inserted (already up-to-date)'
            ELSE 'New operations added for existing modules across institutions successfully'
        END;

END;
$function$
;
CREATE OR REPLACE FUNCTION skolenza."SyncInstitutionModulesAndOperations"()
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE
    InstitutionId BIGINT;
    PlanId INT;
    ModulesInserted INT;
    OperationsInserted INT;
BEGIN

FOR InstitutionId IN
    SELECT TS."InstitutionId"
    FROM skolenza."InstitutionSubscription" TS
    WHERE TS."IsActive" = true
      AND (TS."SubscriptionEndDate" IS NULL 
           OR TS."SubscriptionEndDate"::date >= CURRENT_DATE)
LOOP

    SELECT TS."SubscriptionPlanId"
    INTO PlanId
    FROM skolenza."InstitutionSubscription" TS
    WHERE TS."InstitutionId" = InstitutionId
      AND TS."IsActive" = true
    LIMIT 1;

    IF PlanId IS NOT NULL THEN

        ModulesInserted := 0;
        OperationsInserted := 0;

        CREATE TEMP TABLE tmp_new_modules(
            "ModuleId" BIGINT
        ) ON COMMIT DROP;

        -- Step 1 Insert missing modules and capture inserted ids
        WITH ins AS (
            INSERT INTO skolenza."InstitutionEnabledModule"
            (
                "InstitutionId",
                "ModuleId",
                "ParentModuleId",
                "IsLeafNode",
                "IsEnabled",
                "AddedById",
                "AddedDateTime"
            )
            SELECT
                InstitutionId,
                PMM."ModuleId",
                M."ParentModuleId",
                M."IsLeafNode",
                false,
                InstitutionId,
                CURRENT_TIMESTAMP
            FROM skolenza."PlanModuleMapping" PMM
            INNER JOIN skolenza."Module" M
                ON M."Id" = PMM."ModuleId"
            WHERE PMM."SubscriptionPlanId" = PlanId
              AND PMM."IsActive" = true
              AND NOT EXISTS (
                    SELECT 1
                    FROM skolenza."InstitutionEnabledModule" TEM
                    WHERE TEM."InstitutionId" = InstitutionId
                      AND TEM."ModuleId" = PMM."ModuleId"
              )
            RETURNING "ModuleId"
        )
        INSERT INTO tmp_new_modules("ModuleId")
        SELECT "ModuleId" FROM ins;

        -- ModulesInserted will reflect how many rows were inserted into tmp_new_modules
        GET DIAGNOSTICS ModulesInserted = ROW_COUNT;

        -- Step 3 Sync operations
        INSERT INTO skolenza."InstitutionEnabledOperation"
        (
            "InstitutionId",
            "ModuleId",
            "OperationId",
            "IsEnabled",
            "AddedById",
            "AddedDateTime"
        )
        SELECT
            InstitutionId,
            MOM."ModuleId",
            MOM."OperationId",
            false,
            InstitutionId,
            CURRENT_TIMESTAMP
        FROM tmp_new_modules NM
        INNER JOIN skolenza."ModuleOperationMapping" MOM
            ON MOM."ModuleId" = NM."ModuleId"
           AND MOM."IsActive" = true
        INNER JOIN skolenza."Operation" OP
            ON OP."Id" = MOM."OperationId"
           AND OP."IsActive" = true
        ON CONFLICT DO NOTHING;

        GET DIAGNOSTICS OperationsInserted = ROW_COUNT;

        RAISE NOTICE 'InstitutionId: %, Modules: %, Operations: %',
                     InstitutionId, ModulesInserted, OperationsInserted;

    END IF;

END LOOP;

RAISE NOTICE 'All active institutions synced successfully.';

END;
$function$
;
CREATE OR REPLACE FUNCTION skolenza."InstitutionModuleOperationConfigurationData"(p_institutionid bigint)
 RETURNS TABLE("ParentModuleId" integer, "ModuleId" integer, "ModuleName" text, "IsLeafNode" boolean, "IsEnabled" boolean, "OperationId" integer, "OperationName" text, "IsOperationUsed" boolean, "Level" integer, "BreadCrum" text)
 LANGUAGE plpgsql
AS $function$
BEGIN

RETURN QUERY

WITH RECURSIVE

-- Step 1
"LeafModules" AS (
    SELECT DISTINCT "ModuleId"
    FROM skolenza."InstitutionEnabledOperation"
    WHERE "InstitutionId" = p_institutionid
),

-- Step 2
"AllModules" AS (
    SELECT DISTINCT "ModuleId"
    FROM skolenza."InstitutionEnabledModule"
    WHERE "InstitutionId" = p_institutionid

    UNION

    SELECT "ModuleId"
    FROM "LeafModules"
),

-- Step 3 Recursive hierarchy
"ModuleHierarchy" AS (

    SELECT
        m."Id" AS "ModuleId",
        m."ParentModuleId",
        0 AS "Level",
        CAST(m."ModuleName" AS TEXT) AS "BreadCrum"
    FROM skolenza."Module" m
    INNER JOIN "AllModules" am ON m."Id" = am."ModuleId"
    WHERE m."ParentModuleId" IS NULL

    UNION ALL

    SELECT
        child."Id",
        child."ParentModuleId",
        parent."Level" + 1,
        parent."BreadCrum" || ' > ' || child."ModuleName"
    FROM skolenza."Module" child
    INNER JOIN "ModuleHierarchy" parent
        ON child."ParentModuleId" = parent."ModuleId"
)

SELECT
    mh."ParentModuleId",
    mh."ModuleId",
    m."ModuleName",
    m."IsLeafNode",
    tem."IsEnabled",
    teo."OperationId",
    o."OperationName",
    teo."IsOperationUsed",
    mh."Level",
    mh."BreadCrum"

FROM "ModuleHierarchy" mh

LEFT JOIN skolenza."Module" m
    ON mh."ModuleId" = m."Id"

LEFT JOIN skolenza."InstitutionEnabledModule" tem
    ON mh."ModuleId" = tem."ModuleId"
   AND tem."InstitutionId" = p_institutionid

LEFT JOIN skolenza."InstitutionEnabledOperation" teo
    ON mh."ModuleId" = teo."ModuleId"
   AND teo."InstitutionId" = p_institutionid

LEFT JOIN skolenza."Operation" o
    ON teo."OperationId" = o."Id"

ORDER BY mh."BreadCrum", teo."OperationId";

END;
$function$
;
CREATE OR REPLACE FUNCTION skolenza."UpdateLoginCredential"(p_loginid text, p_latitude text, p_longitude text, p_logindevice integer, p_ipaddresslocal text, p_ipaddresspublic text, p_macaddress text)
 RETURNS TABLE("Status" integer, "ErrorMessage" text)
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_rowsaffected INT;
    v_errmsg TEXT;
BEGIN

    UPDATE skolenza."LoginCredential"
    SET
        "Latitude" = p_latitude,
        "Longitude" = p_longitude,
        "LoginDevice" = p_logindevice,
        "IpAddressLocal" = p_ipaddresslocal,
        "IpAddressPublic" = p_ipaddresspublic,
        "MacAddress" = p_macaddress
    WHERE "LoginId" = p_loginid;

    GET DIAGNOSTICS v_rowsaffected = ROW_COUNT;

    IF v_rowsaffected > 0 THEN
        RETURN QUERY SELECT 1, NULL;
    ELSE
        RETURN QUERY SELECT 0, 'No matching record found for the provided LoginId.';
    END IF;

EXCEPTION
WHEN OTHERS THEN
    v_errmsg := SQLERRM;

    INSERT INTO skolenza."ErrorLog"
    (
        "ErrorMessage",
        "ErrorDateTime",
        "LoginId"
    )
    VALUES
    (
        v_errmsg,
        NOW(),
        p_loginid
    );

    RETURN QUERY SELECT -1, v_errmsg;
END;
$function$
;
CREATE OR REPLACE FUNCTION skolenza."UpdateInstitutionEnabledOperationFromModuleOperation"(p_institutionid bigint)
 RETURNS integer
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_rowcount INTEGER := 0;
BEGIN

    -- Same validation
    IF p_institutionid IS NULL THEN
        RETURN -1;
    END IF;

    -- Same insert logic
    INSERT INTO skolenza."InstitutionEnabledOperation"
    (
        "InstitutionId",
        "ModuleId",
        "OperationId",
        "IsEnabled",
        "AddedById",
        "AddedDateTime"
    )
    SELECT
        p_institutionid,
        mom."ModuleId",
        mom."OperationId",
        0,
        p_institutionid,
        NOW()
    FROM skolenza."ModuleOperationMapping" mom
    WHERE mom."IsActive" = TRUE
      AND EXISTS
      (
          SELECT 1
          FROM skolenza."InstitutionEnabledOperation" teo
          WHERE teo."InstitutionId" = p_institutionid
            AND teo."ModuleId" = mom."ModuleId"
      )
      AND NOT EXISTS
      (
          SELECT 1
          FROM skolenza."InstitutionEnabledOperation" teo2
          WHERE teo2."InstitutionId" = p_institutionid
            AND teo2."ModuleId" = mom."ModuleId"
            AND teo2."OperationId" = mom."OperationId"
      );

    -- Same as @@ROWCOUNT
    GET DIAGNOSTICS v_rowcount = ROW_COUNT;

    RETURN v_rowcount;

EXCEPTION
WHEN OTHERS THEN
    RETURN -1;
END;
$function$
;
CREATE OR REPLACE FUNCTION skolenza."ValidateActiveUserCredentialOnly"(p_loginid text)
 RETURNS bigint
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_staffMemberid BIGINT;
BEGIN

    SELECT "StaffMemberId"
    INTO v_staffMemberid
    FROM skolenza."LoginCredential"
    WHERE "LoginId" = p_loginid
      AND "IsActive" = TRUE
      AND "IsSoftDeleted" = FALSE
    LIMIT 1;

    IF v_staffMemberid IS NOT NULL THEN
        RETURN v_staffMemberid;   -- ✅ return StaffMemberId
    ELSE
        RETURN 0;
    END IF;

EXCEPTION
WHEN OTHERS THEN

    INSERT INTO skolenza."ErrorLog"
    (
        "ErrorMessage",
        "ErrorDateTime"
    )
    VALUES
    (
        SQLERRM,
        CURRENT_TIMESTAMP
    );

    RETURN -1;

END;
$function$
;
CREATE OR REPLACE FUNCTION skolenza."ValidateActiveUserLoginOnly"(p_loginid text)
 RETURNS integer
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_StaffMemberId INT;
BEGIN

    BEGIN

        -- Check active login
        SELECT "StaffMemberId"
        INTO v_StaffMemberId
        FROM skolenza."LoginCredential"
        WHERE "LoginId" = p_LoginId
          AND "IsActive" = true
          AND "IsSoftDeleted" = false
        LIMIT 1;

        IF v_StaffMemberId IS NOT NULL THEN
            RETURN v_StaffMemberId;   -- Return StaffMemberId
        ELSE
            RETURN 0;              -- User not found
        END IF;

    EXCEPTION
        WHEN OTHERS THEN

            INSERT INTO skolenza."ErrorLog"
            (
                "ErrorMessage",
                "ErrorDateTime"
            )
            VALUES
            (
                SQLERRM,
                CURRENT_TIMESTAMP
            );

            RETURN -1;   -- Error case

    END;

END;
$function$
;
CREATE OR REPLACE FUNCTION skolenza.validate_staffMember_type_institution()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$
DECLARE institution_id bigint; type_ids integer[]; type_id integer; type_institution bigint;
BEGIN
    IF TG_TABLE_NAME='StaffMembersChangedTypeHistory' THEN
        SELECT "InstitutionId" INTO institution_id FROM skolenza."StaffMember" WHERE "Id"=NEW."StaffMemberId";
        type_ids := ARRAY[NEW."OldStaffEmploymentTypeId",NEW."NewStaffEmploymentTypeId"];
    ELSIF TG_TABLE_NAME IN ('AccoumndationAllowancePolicyByDesignation','MealAllowancePolicyByDesignation','TravelAllowancePolicyByDesignation') THEN
        SELECT "InstitutionId" INTO institution_id FROM skolenza."Designation" WHERE "Id"=NEW."DesignationId";
        type_ids := ARRAY[NEW."StaffEmploymentTypeId"];
    ELSE
        institution_id := NEW."InstitutionId";
        type_ids := ARRAY[NEW."StaffEmploymentTypeId"];
    END IF;
    FOREACH type_id IN ARRAY type_ids LOOP
        IF type_id IS NOT NULL THEN
            SELECT "InstitutionId" INTO type_institution FROM skolenza."StaffEmploymentType" WHERE "Id"=type_id;
            IF NOT FOUND OR type_institution IS DISTINCT FROM institution_id THEN
                RAISE EXCEPTION 'StaffEmploymentType must belong to the same institution' USING ERRCODE='23514';
            END IF;
        END IF;
    END LOOP;
    RETURN NEW;
END $function$
;

DO $$
DECLARE remaining integer;
BEGIN
    SELECT count(*) INTO remaining
    FROM pg_proc p
    JOIN pg_namespace n ON n.oid = p.pronamespace
    WHERE n.nspname = 'skolenza'
      AND p.prokind = 'f'
      AND pg_get_functiondef(p.oid) ILIKE '%axionpro.%';
    IF remaining <> 0 THEN
        RAISE EXCEPTION 'Schema rebinding incomplete: % function(s) still reference AxionPro.', remaining;
    END IF;
END $$;
COMMIT;