-- Completes database-level conflict protection for staffMember work-mode overrides.
-- Application validation returns the detailed institution-facing error; this constraint closes
-- concurrent-write races. Safe to rerun. Existing conflicting rows are reported, not modified.

BEGIN;
SELECT pg_advisory_xact_lock(7421092601);
CREATE EXTENSION IF NOT EXISTS btree_gist;

DO $validation$
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM skolenza."StaffMemberWorkModeOverrideRequest" first_override
        JOIN skolenza."StaffMemberWorkModeOverrideRequest" second_override
          ON second_override."InstitutionId" = first_override."InstitutionId"
         AND second_override."StaffMemberId" = first_override."StaffMemberId"
         AND second_override."Id" > first_override."Id"
         AND daterange(second_override."FromDate", second_override."ToDate", '[]')
             && daterange(first_override."FromDate", first_override."ToDate", '[]')
        WHERE first_override."IsActive" = TRUE
          AND first_override."IsSoftDeleted" = FALSE
          AND first_override."ApprovalStatus" IN (1, 2)
          AND second_override."IsActive" = TRUE
          AND second_override."IsSoftDeleted" = FALSE
          AND second_override."ApprovalStatus" IN (1, 2)
    ) THEN
        RAISE EXCEPTION 'Active Pending/Approved staffMember work-mode overrides overlap. Resolve the listed business records before applying this constraint.';
    END IF;
END
$validation$;

DO $constraint$
BEGIN
    IF NOT EXISTS
    (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'EX_StaffMemberWorkModeOverride_StaffMember_Window'
          AND conrelid = 'skolenza."StaffMemberWorkModeOverrideRequest"'::regclass
    ) THEN
        ALTER TABLE skolenza."StaffMemberWorkModeOverrideRequest"
        ADD CONSTRAINT "EX_StaffMemberWorkModeOverride_StaffMember_Window"
        EXCLUDE USING gist
        (
            "InstitutionId" WITH =,
            "StaffMemberId" WITH =,
            daterange("FromDate", "ToDate", '[]') WITH &&
        )
        WHERE ("IsActive" = TRUE AND "IsSoftDeleted" = FALSE AND "ApprovalStatus" IN (1, 2));
    END IF;
END
$constraint$;

COMMIT;
