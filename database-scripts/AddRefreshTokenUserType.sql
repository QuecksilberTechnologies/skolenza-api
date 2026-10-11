-- ============================================================================
-- Author      : Deepesh Gupta
-- Company     : Quecksilber Technologies
-- Role        : CEO
-- Purpose     : Adds explicit Institution and Host ownership foreign keys to the common refresh-token table.
-- ============================================================================

-- Remove the legacy foreign key that made LoginId institution-only. Ownership is now
-- represented by one of the two explicit owner foreign keys below.
ALTER TABLE skolenza."RefreshToken"
    DROP CONSTRAINT IF EXISTS "FK__RefreshTo__Login__7132C993";

-- UserType values are defined by LoginUserType in the application:
-- InstitutionStaffMember = 1 and Host = 2.
ALTER TABLE skolenza."RefreshToken"
    ADD COLUMN IF NOT EXISTS "UserType" SMALLINT NOT NULL DEFAULT 1;

ALTER TABLE skolenza."RefreshToken"
    ADD COLUMN IF NOT EXISTS "LoginCredentialId" BIGINT NULL;

ALTER TABLE skolenza."RefreshToken"
    ADD COLUMN IF NOT EXISTS "HostUserId" BIGINT NULL;

CREATE INDEX IF NOT EXISTS "IX_RefreshToken_LoginId_UserType"
    ON skolenza."RefreshToken" ("LoginId", "UserType");

CREATE INDEX IF NOT EXISTS "IX_RefreshToken_LoginCredentialId"
    ON skolenza."RefreshToken" ("LoginCredentialId");

CREATE INDEX IF NOT EXISTS "IX_RefreshToken_HostUserId"
    ON skolenza."RefreshToken" ("HostUserId");

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'FK_RefreshToken_LoginCredential'
          AND connamespace = 'skolenza'::regnamespace) THEN
        ALTER TABLE skolenza."RefreshToken"
            ADD CONSTRAINT "FK_RefreshToken_LoginCredential"
            FOREIGN KEY ("LoginCredentialId")
            REFERENCES skolenza."LoginCredential" ("Id")
            ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'FK_RefreshToken_HostUser'
          AND connamespace = 'skolenza'::regnamespace) THEN
        ALTER TABLE skolenza."RefreshToken"
            ADD CONSTRAINT "FK_RefreshToken_HostUser"
            FOREIGN KEY ("HostUserId")
            REFERENCES skolenza."HostUser" ("Id")
            ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'CK_RefreshToken_ExactlyOneOwner'
          AND connamespace = 'skolenza'::regnamespace) THEN
        ALTER TABLE skolenza."RefreshToken"
            ADD CONSTRAINT "CK_RefreshToken_ExactlyOneOwner"
            CHECK (
                ("UserType" = 1 AND "LoginCredentialId" IS NOT NULL AND "HostUserId" IS NULL)
                OR
                ("UserType" = 2 AND "HostUserId" IS NOT NULL AND "LoginCredentialId" IS NULL))
            NOT VALID;
    END IF;
END $$;

-- Existing rows created before explicit owner foreign keys require data remediation
-- before validating CK_RefreshToken_ExactlyOneOwner. New writes are constrained.
