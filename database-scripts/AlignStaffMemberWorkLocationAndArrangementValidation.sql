BEGIN;

-- GiST exclusion constraints make the API's overlap rule concurrency-safe.
CREATE EXTENSION IF NOT EXISTS btree_gist;

-- Date-window conflicts are validated before writes. These former unique indexes
-- prevented valid non-overlapping history and future schedules from coexisting.
DROP INDEX IF EXISTS skolenza."UX_StaffMemberLocationAssignment_StaffMember_Location";
DROP INDEX IF EXISTS skolenza."UX_StaffMemberLocationAssignment_Primary";
DROP INDEX IF EXISTS skolenza."UX_StaffMemberWorkArrangement_Current";

CREATE INDEX IF NOT EXISTS "IX_StaffMemberLocationAssignment_StaffMember_Location_EffectiveFrom"
    ON skolenza."StaffMemberLocationAssignment" ("InstitutionId", "StaffMemberId", "CampusId", "EffectiveFrom");

CREATE INDEX IF NOT EXISTS "IX_StaffMemberLocationAssignment_Primary_EffectiveFrom"
    ON skolenza."StaffMemberLocationAssignment" ("InstitutionId", "StaffMemberId", "IsPrimary", "EffectiveFrom");

CREATE INDEX IF NOT EXISTS "IX_StaffMemberWorkArrangement_StaffMember_EffectiveFrom"
    ON skolenza."StaffMemberWorkArrangement" ("InstitutionId", "StaffMemberId", "EffectiveFrom");

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'EX_StaffMemberLocationAssignment_StaffMember_Location_Window') THEN
        ALTER TABLE skolenza."StaffMemberLocationAssignment"
            ADD CONSTRAINT "EX_StaffMemberLocationAssignment_StaffMember_Location_Window"
            EXCLUDE USING gist
            ("InstitutionId" WITH =, "StaffMemberId" WITH =, "CampusId" WITH =,
             daterange("EffectiveFrom", COALESCE("EffectiveTo", 'infinity'::date), '[]') WITH &&)
            WHERE ("IsActive" = true AND "IsSoftDeleted" = false);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'EX_StaffMemberLocationAssignment_Primary_Window') THEN
        ALTER TABLE skolenza."StaffMemberLocationAssignment"
            ADD CONSTRAINT "EX_StaffMemberLocationAssignment_Primary_Window"
            EXCLUDE USING gist
            ("InstitutionId" WITH =, "StaffMemberId" WITH =,
             daterange("EffectiveFrom", COALESCE("EffectiveTo", 'infinity'::date), '[]') WITH &&)
            WHERE ("IsPrimary" = true AND "IsActive" = true AND "IsSoftDeleted" = false);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'EX_StaffMemberWorkArrangement_StaffMember_Window') THEN
        ALTER TABLE skolenza."StaffMemberWorkArrangement"
            ADD CONSTRAINT "EX_StaffMemberWorkArrangement_StaffMember_Window"
            EXCLUDE USING gist
            ("InstitutionId" WITH =, "StaffMemberId" WITH =,
             daterange("EffectiveFrom", COALESCE("EffectiveTo", 'infinity'::date), '[]') WITH &&)
            WHERE ("IsActive" = true AND "IsSoftDeleted" = false);
    END IF;
END $$;

COMMIT;
