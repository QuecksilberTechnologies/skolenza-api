-- Supports seamless remote replacement of a device's opaque HTTPS gateway URL.
-- The old hash remains valid while a protected setdevinfo command gives the
-- physical device its new address. The pending hash is promoted on first poll.

ALTER TABLE skolenza."InstitutionDeviceConfiguration"
    ADD COLUMN IF NOT EXISTS "PendingHttpsIngressTokenHash" character varying(64) NULL;

ALTER TABLE skolenza."InstitutionDeviceConfiguration"
    ADD COLUMN IF NOT EXISTS "PendingHttpsIngressTokenExpiresDateTime" timestamp without time zone NULL;

ALTER TABLE skolenza."InstitutionDeviceConfiguration"
    DROP CONSTRAINT IF EXISTS "CK_InstitutionDeviceConfiguration_PendingHttpsIngressTokenHash";

ALTER TABLE skolenza."InstitutionDeviceConfiguration"
    ADD CONSTRAINT "CK_InstitutionDeviceConfiguration_PendingHttpsIngressTokenHash"
    CHECK ("PendingHttpsIngressTokenHash" IS NULL OR "PendingHttpsIngressTokenHash" ~ '^[0-9a-f]{64}$');

CREATE UNIQUE INDEX IF NOT EXISTS "UX_InstitutionDeviceConfiguration_PendingHttpsIngressTokenHash"
    ON skolenza."InstitutionDeviceConfiguration" ("PendingHttpsIngressTokenHash")
    WHERE "PendingHttpsIngressTokenHash" IS NOT NULL;
