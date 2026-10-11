/*
  Adds stable codes to State, District, and Locality and moves postal ownership
  away from District. Existing rows receive deterministic internal codes. A
  country seed may later replace them with source-issued codes without changing
  primary or foreign keys.
*/

BEGIN;
SET LOCAL lock_timeout = '15s';
SET LOCAL statement_timeout = '180s';

ALTER TABLE skolenza."State"
    ADD COLUMN IF NOT EXISTS "StateCode" character varying(50);

UPDATE skolenza."State" state
SET "StateCode" = concat(COALESCE(country."CountryCode", 'XX'), '-S', state."Id")
FROM skolenza."Country" country
WHERE state."CountryId" = country."Id"
  AND NULLIF(btrim(state."StateCode"), '') IS NULL;

ALTER TABLE skolenza."State"
    ALTER COLUMN "StateCode" SET NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS "UX_State_CountryId_StateCode"
    ON skolenza."State" ("CountryId", "StateCode");

ALTER TABLE skolenza."District"
    ADD COLUMN IF NOT EXISTS "DistrictCode" character varying(100);

UPDATE skolenza."District" district
SET "DistrictCode" = concat(state."StateCode", '-D', district."Id")
FROM skolenza."State" state
WHERE district."StateId" = state."Id"
  AND NULLIF(btrim(district."DistrictCode"), '') IS NULL;

ALTER TABLE skolenza."District"
    ALTER COLUMN "DistrictCode" SET NOT NULL,
    DROP COLUMN IF EXISTS "PinCode";

CREATE UNIQUE INDEX IF NOT EXISTS "UX_District_StateId_DistrictCode"
    ON skolenza."District" ("StateId", "DistrictCode");

ALTER TABLE skolenza."Locality"
    ADD COLUMN IF NOT EXISTS "LocalityCode" character varying(100),
    ADD COLUMN IF NOT EXISTS "PostalCode" character varying(20);

UPDATE skolenza."Locality" locality
SET "LocalityCode" = concat(district."DistrictCode", '-L', locality."Id")
FROM skolenza."District" district
WHERE locality."DistrictId" = district."Id"
  AND NULLIF(btrim(locality."LocalityCode"), '') IS NULL;

ALTER TABLE skolenza."Locality"
    ALTER COLUMN "LocalityCode" SET NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS "UX_Locality_DistrictId_LocalityCode"
    ON skolenza."Locality" ("DistrictId", "LocalityCode");

COMMIT;

SELECT
    (SELECT count(*) FROM skolenza."State" WHERE NULLIF(btrim("StateCode"), '') IS NULL) AS "MissingStateCodes",
    (SELECT count(*) FROM skolenza."District" WHERE NULLIF(btrim("DistrictCode"), '') IS NULL) AS "MissingDistrictCodes",
    (SELECT count(*) FROM skolenza."Locality" WHERE NULLIF(btrim("LocalityCode"), '') IS NULL) AS "MissingLocalityCodes";
