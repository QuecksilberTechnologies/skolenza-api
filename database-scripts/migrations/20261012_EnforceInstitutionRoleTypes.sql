-- Purpose: Protect the centralized Institution role-type catalogue in persistence.
-- Values: 1 Institution Administrator, 2 Staff Member, 3 Staff Manager,
--         4 External User, 5 Student, 6 Parent / Guardian.
BEGIN;

ALTER TABLE skolenza."Role"
    DROP CONSTRAINT IF EXISTS "CK_Role_RoleType";

ALTER TABLE skolenza."Role"
    ADD CONSTRAINT "CK_Role_RoleType"
    CHECK ("RoleType" IN (1, 2, 3, 4, 5, 6));

COMMENT ON COLUMN skolenza."Role"."RoleType" IS
    'Institution role persona: 1 Institution Administrator, 2 Staff Member, 3 Staff Manager, 4 External User, 5 Student, 6 Parent / Guardian.';

COMMIT;
