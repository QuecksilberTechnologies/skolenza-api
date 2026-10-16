-- Historical prerequisite note: older databases applied
-- EnforceDesignationDepartmentScope.sql before this additive migration.
-- Position-only databases do not require or rerun that retired prerequisite.
BEGIN;

CREATE TABLE IF NOT EXISTS skolenza."BulkImportJob"
(
    "Id" uuid PRIMARY KEY,
    "InstitutionId" bigint NOT NULL,
    "ActorId" bigint NOT NULL,
    "RoleId" integer NOT NULL,
    "ModuleId" integer NOT NULL,
    "OperationId" integer NOT NULL,
    "Master" integer NOT NULL CHECK ("Master" BETWEEN 1 AND 3),
    "Status" integer NOT NULL CHECK ("Status" BETWEEN 1 AND 7),
    "PreviewJson" jsonb NOT NULL,
    "InputHash" varchar(64) NOT NULL,
    "NextRow" integer NOT NULL DEFAULT 0 CHECK ("NextRow" >= 0),
    "CreatedAtUtc" timestamptz NOT NULL,
    "UpdatedAtUtc" timestamptz NOT NULL,
    "ScheduledAtUtc" timestamptz NULL,
    "Error" text NULL
);

CREATE INDEX IF NOT EXISTS "IX_BulkImportJob_Due"
    ON skolenza."BulkImportJob" ("Status", "ScheduledAtUtc", "UpdatedAtUtc");
CREATE INDEX IF NOT EXISTS "IX_BulkImportJob_Owner"
    ON skolenza."BulkImportJob" ("InstitutionId", "ActorId", "Master", "CreatedAtUtc");

-- Existing duplicate data must be resolved explicitly; fail rather than merge.
CREATE UNIQUE INDEX IF NOT EXISTS "UX_Department_Institution_Name_Live"
    ON skolenza."Department" ("InstitutionId", lower(btrim("DepartmentName")))
    WHERE "IsSoftDeleted" IS NOT TRUE;
CREATE UNIQUE INDEX IF NOT EXISTS "UX_Role_Institution_Name_Live"
    ON skolenza."Role" ("InstitutionId", lower(btrim("RoleName")))
    WHERE "IsSoftDeleted" IS NOT TRUE;

COMMIT;

