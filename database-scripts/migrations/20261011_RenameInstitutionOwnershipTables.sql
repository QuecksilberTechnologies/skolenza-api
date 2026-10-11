BEGIN;

CREATE TEMP TABLE institution_function_definitions ON COMMIT DROP AS
SELECT p.oid, pg_get_functiondef(p.oid) AS definition
FROM pg_proc p
JOIN pg_namespace n ON n.oid = p.pronamespace
WHERE n.nspname = 'skolenza'
  AND p.prokind IN ('f', 'p');

ALTER TABLE IF EXISTS skolenza."TenantBillingProfile" RENAME TO "InstitutionBillingProfile";
ALTER TABLE IF EXISTS skolenza."TenantBillingSubscription" RENAME TO "InstitutionBillingSubscription";
ALTER TABLE IF EXISTS skolenza."TenantCardMaster" RENAME TO "InstitutionCardMaster";
ALTER TABLE IF EXISTS skolenza."TenantDeviceConfiguration" RENAME TO "InstitutionDeviceConfiguration";
ALTER TABLE IF EXISTS skolenza."TenantDevice" RENAME TO "InstitutionDevice";
ALTER TABLE IF EXISTS skolenza."TenantEmailConfig" RENAME TO "InstitutionEmailConfig";
ALTER TABLE IF EXISTS skolenza."TenantEmailTemplate" RENAME TO "InstitutionEmailTemplate";
ALTER TABLE IF EXISTS skolenza."TenantEmployeeSectionDefault" RENAME TO "InstitutionStaffSectionDefault";
ALTER TABLE IF EXISTS skolenza."TenantEnabledModule" RENAME TO "InstitutionEnabledModule";
ALTER TABLE IF EXISTS skolenza."TenantEnabledOperation" RENAME TO "InstitutionEnabledOperation";
ALTER TABLE IF EXISTS skolenza."TenantEncryptionKeys" RENAME TO "InstitutionEncryptionKeys";
ALTER TABLE IF EXISTS skolenza."TenantIndustry" RENAME TO "InstitutionType";
ALTER TABLE IF EXISTS skolenza."TenantLocation" RENAME TO "Campus";
ALTER TABLE IF EXISTS skolenza."TenantProfile" RENAME TO "InstitutionProfile";
ALTER TABLE IF EXISTS skolenza."TenantSubscription" RENAME TO "InstitutionSubscription";
ALTER TABLE IF EXISTS skolenza."Tenant" RENAME TO "Institution";

ALTER SEQUENCE IF EXISTS skolenza."TenantBillingProfile_Id_seq" RENAME TO "InstitutionBillingProfile_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."TenantBillingSubscription_Id_seq" RENAME TO "InstitutionBillingSubscription_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."TenantCardMaster_Id_seq" RENAME TO "InstitutionCardMaster_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."TenantDeviceConfiguration_Id_seq" RENAME TO "InstitutionDeviceConfiguration_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."TenantDevice_Id_seq" RENAME TO "InstitutionDevice_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."TenantEmailConfig_Id_seq" RENAME TO "InstitutionEmailConfig_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."TenantEmailTemplate_Id_seq" RENAME TO "InstitutionEmailTemplate_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."TenantEnabledModule_Id_seq" RENAME TO "InstitutionEnabledModule_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."TenantEnabledOperation_Id_seq" RENAME TO "InstitutionEnabledOperation_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."TenantEncryptionKeys_Id_seq" RENAME TO "InstitutionEncryptionKeys_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."TenantIndustry_Id_seq" RENAME TO "InstitutionType_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."TenantLocation_Id_seq" RENAME TO "Campus_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."TenantProfile_Id_seq" RENAME TO "InstitutionProfile_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."TenantSubscription_Id_seq" RENAME TO "InstitutionSubscription_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."Tenant_Id_seq" RENAME TO "Institution_Id_seq";

DO $migration$
DECLARE
    item record;
    updated_definition text;
BEGIN
    FOR item IN SELECT definition FROM institution_function_definitions LOOP
        updated_definition := item.definition;
        updated_definition := replace(updated_definition, '"TenantBillingProfile"', '"InstitutionBillingProfile"');
        updated_definition := replace(updated_definition, '"TenantBillingSubscription"', '"InstitutionBillingSubscription"');
        updated_definition := replace(updated_definition, '"TenantCardMaster"', '"InstitutionCardMaster"');
        updated_definition := replace(updated_definition, '"TenantDeviceConfiguration"', '"InstitutionDeviceConfiguration"');
        updated_definition := replace(updated_definition, '"TenantDevice"', '"InstitutionDevice"');
        updated_definition := replace(updated_definition, '"TenantEmailConfig"', '"InstitutionEmailConfig"');
        updated_definition := replace(updated_definition, '"TenantEmailTemplate"', '"InstitutionEmailTemplate"');
        updated_definition := replace(updated_definition, '"TenantEmployeeSectionDefault"', '"InstitutionStaffSectionDefault"');
        updated_definition := replace(updated_definition, '"TenantEnabledModule"', '"InstitutionEnabledModule"');
        updated_definition := replace(updated_definition, '"TenantEnabledOperation"', '"InstitutionEnabledOperation"');
        updated_definition := replace(updated_definition, '"TenantEncryptionKeys"', '"InstitutionEncryptionKeys"');
        updated_definition := replace(updated_definition, '"TenantIndustry"', '"InstitutionType"');
        updated_definition := replace(updated_definition, '"TenantLocation"', '"Campus"');
        updated_definition := replace(updated_definition, '"TenantProfile"', '"InstitutionProfile"');
        updated_definition := replace(updated_definition, '"TenantSubscription"', '"InstitutionSubscription"');
        updated_definition := replace(updated_definition, '"Tenant"', '"Institution"');

        IF updated_definition IS DISTINCT FROM item.definition THEN
            EXECUTE updated_definition;
        END IF;
    END LOOP;
END
$migration$;

COMMIT;
