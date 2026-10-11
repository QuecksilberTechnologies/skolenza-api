-- Company: Quecksilber Technologies
-- Purpose: Every designation belongs to a department. Names are unique only
-- within that institution/department; IT/Manager and HR/Manager are both valid.
-- Does not delete, merge or invent departments for existing data.
BEGIN;

LOCK TABLE skolenza."Designation" IN SHARE ROW EXCLUSIVE MODE;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM skolenza."Designation"
        WHERE "DepartmentId" IS NULL OR "DepartmentId" <= 0
    ) THEN
        RAISE EXCEPTION 'Designation has missing/invalid DepartmentId. Resolve those records before applying this migration.';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM skolenza."Designation" designation
        LEFT JOIN skolenza."Department" department
            ON department."Id" = designation."DepartmentId"
           AND department."InstitutionId" = designation."InstitutionId"
        WHERE department."Id" IS NULL
    ) THEN
        RAISE EXCEPTION 'Designation has a missing or other-institution department. Resolve those records first.';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM skolenza."Designation"
        WHERE "IsSoftDeleted" IS NOT TRUE
        GROUP BY "InstitutionId", "DepartmentId", lower(btrim("DesignationName"))
        HAVING count(*) > 1
    ) THEN
        RAISE EXCEPTION 'Duplicate designation names exist within a institution department. Resolve duplicates before applying this migration.';
    END IF;
END;
$$;

ALTER TABLE skolenza."Designation"
    ALTER COLUMN "DepartmentId" SET NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS "UX_Designation_Institution_Department_Name_Live"
    ON skolenza."Designation" (
        "InstitutionId",
        "DepartmentId",
        lower(btrim("DesignationName"))
    )
    WHERE "IsSoftDeleted" IS NOT TRUE;

COMMIT;
