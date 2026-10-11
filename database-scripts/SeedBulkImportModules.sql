-- Bulk import/export operation mapping for the existing two-level Skolenza module catalogue.
-- Idempotent: safe to run after the canonical module and operation seeds.
-- Template columns are owned by BulkImportConstants in the API; this script does not duplicate them in DB.
BEGIN;

LOCK TABLE skolenza."Module", skolenza."Operation", skolenza."ModuleOperationMapping"
    IN SHARE ROW EXCLUSIVE MODE;

DO $bulk_operations$
DECLARE
    import_operation_id integer;
    export_operation_id integer;
    missing_module_codes text;
BEGIN
    SELECT string_agg(required.code, ', ' ORDER BY required.code)
    INTO missing_module_codes
    FROM (VALUES
        ('INSTITUTION_DEPARTMENTS'),
        ('INSTITUTION_STAFF_DESIGNATIONS'),
        ('INSTITUTION_ACCESS'),
        ('STAFF_EMPLOYMENT_TYPES'),
        ('STAFF_DIRECTORY')
    ) AS required(code)
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM skolenza."Module" module
        WHERE module."ModuleCode" = required.code
          AND module."ModuleScope" = 1
          AND module."IsLeafNode" = TRUE
    );

    IF missing_module_codes IS NOT NULL THEN
        RAISE EXCEPTION
            'Canonical leaf modules are missing: %. Run the canonical Skolenza module seed first.',
            missing_module_codes;
    END IF;

    SELECT "Id"
    INTO import_operation_id
    FROM skolenza."Operation"
    WHERE "OperationType" = 12
       OR LOWER(BTRIM("OperationName")) = 'import'
    ORDER BY
        CASE WHEN "OperationType" = 12 THEN 0 ELSE 1 END,
        "IsActive" DESC,
        "Id"
    LIMIT 1;

    IF import_operation_id IS NULL THEN
        INSERT INTO skolenza."Operation"
        (
            "OperationName", "Remark", "OperationType", "IsActive",
            "AddedById", "AddedDateTime", "IconImage"
        )
        VALUES
        (
            'Import',
            'Upload, preview, validate and queue an authorized bulk import.',
            12,
            TRUE,
            1,
            CURRENT_TIMESTAMP,
            'upload'
        )
        RETURNING "Id" INTO import_operation_id;
    ELSE
        UPDATE skolenza."Operation"
        SET "OperationName" = 'Import',
            "Remark" = 'Upload, preview, validate and queue an authorized bulk import.',
            "OperationType" = 12,
            "IsActive" = TRUE,
            "IconImage" = COALESCE(NULLIF(BTRIM("IconImage"), ''), 'upload'),
            "UpdatedById" = 1,
            "UpdatedDateTime" = CURRENT_TIMESTAMP
        WHERE "Id" = import_operation_id;
    END IF;

    SELECT "Id"
    INTO export_operation_id
    FROM skolenza."Operation"
    WHERE "OperationType" = 11
       OR LOWER(BTRIM("OperationName")) = 'export'
    ORDER BY
        CASE WHEN "OperationType" = 11 THEN 0 ELSE 1 END,
        "IsActive" DESC,
        "Id"
    LIMIT 1;

    IF export_operation_id IS NULL THEN
        INSERT INTO skolenza."Operation"
        (
            "OperationName", "Remark", "OperationType", "IsActive",
            "AddedById", "AddedDateTime", "IconImage"
        )
        VALUES
        (
            'Export',
            'Export authorized module data for spreadsheet use.',
            11,
            TRUE,
            1,
            CURRENT_TIMESTAMP,
            'download'
        )
        RETURNING "Id" INTO export_operation_id;
    ELSE
        UPDATE skolenza."Operation"
        SET "OperationName" = 'Export',
            "Remark" = 'Export authorized module data for spreadsheet use.',
            "OperationType" = 11,
            "IsActive" = TRUE,
            "IconImage" = COALESCE(NULLIF(BTRIM("IconImage"), ''), 'download'),
            "UpdatedById" = 1,
            "UpdatedDateTime" = CURRENT_TIMESTAMP
        WHERE "Id" = export_operation_id;
    END IF;

    INSERT INTO skolenza."ModuleOperationMapping"
    (
        "ModuleId", "OperationId", "PageURL", "IconURL",
        "IsCommonItem", "IsOperational", "Priority", "Remark",
        "IsActive", "AddedById", "AddedDateTime"
    )
    SELECT
        module."Id",
        operation_seed.operation_id,
        module."URLPath",
        operation_seed.icon_url,
        FALSE,
        TRUE,
        operation_seed.priority,
        operation_seed.remark,
        TRUE,
        1,
        CURRENT_TIMESTAMP
    FROM skolenza."Module" module
    CROSS JOIN (VALUES
        (import_operation_id, 'upload', 15, 'Bulk import through the existing permission pipeline.'),
        (export_operation_id, 'download', 20, 'Bulk export through the existing permission pipeline.')
    ) AS operation_seed(operation_id, icon_url, priority, remark)
    WHERE module."ModuleScope" = 1
      AND module."IsLeafNode" = TRUE
      AND module."ModuleCode" IN
      (
          'INSTITUTION_DEPARTMENTS',
          'INSTITUTION_STAFF_DESIGNATIONS',
          'INSTITUTION_ACCESS',
          'STAFF_EMPLOYMENT_TYPES',
          'STAFF_DIRECTORY'
      )
      AND NOT EXISTS
      (
          SELECT 1
          FROM skolenza."ModuleOperationMapping" existing
          WHERE existing."ModuleId" = module."Id"
            AND existing."OperationId" = operation_seed.operation_id
      );

    UPDATE skolenza."ModuleOperationMapping" mapping
    SET "PageURL" = module."URLPath",
        "IconURL" = CASE
            WHEN mapping."OperationId" = import_operation_id THEN 'upload'
            ELSE 'download'
        END,
        "IsCommonItem" = FALSE,
        "IsOperational" = TRUE,
        "Priority" = CASE
            WHEN mapping."OperationId" = import_operation_id THEN 15
            ELSE 20
        END,
        "Remark" = CASE
            WHEN mapping."OperationId" = import_operation_id
                THEN 'Bulk import through the existing permission pipeline.'
            ELSE 'Bulk export through the existing permission pipeline.'
        END,
        "IsActive" = TRUE,
        "UpdatedById" = 1,
        "UpdatedDateTime" = CURRENT_TIMESTAMP
    FROM skolenza."Module" module
    WHERE mapping."ModuleId" = module."Id"
      AND mapping."OperationId" IN (import_operation_id, export_operation_id)
      AND module."ModuleScope" = 1
      AND module."IsLeafNode" = TRUE
      AND module."ModuleCode" IN
      (
          'INSTITUTION_DEPARTMENTS',
          'INSTITUTION_STAFF_DESIGNATIONS',
          'INSTITUTION_ACCESS',
          'STAFF_EMPLOYMENT_TYPES',
          'STAFF_DIRECTORY'
      );
END $bulk_operations$;

COMMIT;

SELECT
    module."ModuleCode",
    operation."OperationName",
    operation."OperationType",
    mapping."IsActive"
FROM skolenza."Module" module
JOIN skolenza."ModuleOperationMapping" mapping
  ON mapping."ModuleId" = module."Id"
JOIN skolenza."Operation" operation
  ON operation."Id" = mapping."OperationId"
WHERE module."ModuleCode" IN
(
    'INSTITUTION_DEPARTMENTS',
    'INSTITUTION_STAFF_DESIGNATIONS',
    'INSTITUTION_ACCESS',
    'STAFF_EMPLOYMENT_TYPES',
    'STAFF_DIRECTORY'
)
  AND operation."OperationType" IN (11, 12)
ORDER BY module."ModuleCode", operation."OperationType";
