-- Adds generic command-transport support and secure HTTPS device ingress.
-- Run this script once BEFORE deploying the matching API build.
-- It is additive and backfills legacy MQTT/MQTTS configurations safely.

BEGIN;

ALTER TABLE skolenza."InstitutionDeviceConfiguration"
    ADD COLUMN IF NOT EXISTS "CommandTransport" smallint NULL;

ALTER TABLE skolenza."InstitutionDeviceConfiguration"
    ADD COLUMN IF NOT EXISTS "HttpsIngressTokenHash" character varying(64) NULL;

-- Existing rows retain their transport semantics without any manual conversion.
UPDATE skolenza."InstitutionDeviceConfiguration"
SET "CommandTransport" = "MqttTransport"
WHERE "CommandTransport" IS NULL
  AND "MqttTransport" IN (1, 2);

ALTER TABLE skolenza."InstitutionDeviceConfiguration"
    DROP CONSTRAINT IF EXISTS "CK_InstitutionDeviceConfiguration_CommandTransport";

ALTER TABLE skolenza."InstitutionDeviceConfiguration"
    ADD CONSTRAINT "CK_InstitutionDeviceConfiguration_CommandTransport"
    CHECK ("CommandTransport" IS NULL OR "CommandTransport" IN (1, 2, 3, 4, 5, 6));

ALTER TABLE skolenza."InstitutionDeviceConfiguration"
    DROP CONSTRAINT IF EXISTS "CK_InstitutionDeviceConfiguration_HttpsIngressTokenHash";

ALTER TABLE skolenza."InstitutionDeviceConfiguration"
    ADD CONSTRAINT "CK_InstitutionDeviceConfiguration_HttpsIngressTokenHash"
    CHECK ("HttpsIngressTokenHash" IS NULL OR "HttpsIngressTokenHash" ~ '^[0-9a-f]{64}$');

CREATE UNIQUE INDEX IF NOT EXISTS "UX_InstitutionDeviceConfiguration_HttpsIngressTokenHash"
    ON skolenza."InstitutionDeviceConfiguration" ("HttpsIngressTokenHash")
    WHERE "HttpsIngressTokenHash" IS NOT NULL;

COMMIT;
