-- Applies to existing and future staffMembers through institution-level lookup.
BEGIN;
CREATE TABLE IF NOT EXISTS skolenza."InstitutionStaffSectionDefault" (
    "InstitutionId" bigint NOT NULL REFERENCES skolenza."Institution"("Id"),
    "ModuleCode" varchar(64) NOT NULL,
    "IsEditAllowed" boolean NOT NULL DEFAULT false,
    "UpdatedById" bigint NOT NULL,
    "UpdatedDateTime" timestamp with time zone NOT NULL DEFAULT now(),
    PRIMARY KEY ("InstitutionId", "ModuleCode"),
    CHECK ("ModuleCode" IN ('EMP_WORK_LOCATIONS', 'EMP_DEVICES',
        'EMP_WORK_ARRANGEMENT', 'EMP_WORK_PATTERN', 'EMP_OVERRIDES'))
);
COMMIT;
