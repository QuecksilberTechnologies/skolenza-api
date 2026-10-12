BEGIN;

-- Align the persisted routine contract with RoleModuleOperationResponseDTO.
-- The legacy result column was still named TenantId, which makes EF Core's
-- FromSql materializer fail because the API contract requires InstitutionId.
DROP FUNCTION IF EXISTS skolenza."GetActiveRoleModuleOperations"(bigint, text);

CREATE FUNCTION skolenza."GetActiveRoleModuleOperations"(
    p_institutionid bigint,
    p_roleids text)
RETURNS TABLE(
    "RoleId" integer,
    "InstitutionId" bigint,
    "ModuleId" integer,
    "ModuleName" text,
    "URLPath" text,
    "DisplayName" text,
    "ImageIconWeb" text,
    "ImageIconMobile" text,
    "ParentModuleId" integer,
    "SubModuleName" text,
    "MainModuleId" integer,
    "MainModuleName" text,
    "OperationId" integer,
    "OperationName" text,
    "DataViewStructureId" integer,
    "DisplayOn" text)
LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY
    WITH "RoleIdTable" AS (
        SELECT value::integer AS "RoleId"
        FROM unnest(string_to_array(p_roleids, ',')) value
    )
    SELECT
        rep."RoleId",
        COALESCE(tem."InstitutionId", p_institutionid),
        m."Id",
        m."ModuleName"::text,
        COALESCE(m."URLPath", '')::text,
        m."DisplayName"::text,
        m."ImageIconWeb"::text,
        m."ImageIconMobile"::text,
        m."ParentModuleId",
        COALESCE(sm."ModuleName", '')::text,
        COALESCE(mm."Id", 0),
        COALESCE(mm."ModuleName", '')::text,
        rep."OperationId",
        op."OperationName"::text,
        mom."DataViewStructureId",
        COALESCE(dvs."DisplayOn", '')::text
    FROM skolenza."RoleModuleAndPermission" rep
    INNER JOIN "RoleIdTable" rid
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
        COALESCE(mm."Id", 0),
        COALESCE(sm."Id", 0),
        m."Id",
        rep."OperationId";
END;
$function$;

COMMIT;
