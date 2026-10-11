BEGIN;

CREATE TEMP TABLE tenant_function_definitions ON COMMIT DROP AS
SELECT p.oid, pg_get_functiondef(p.oid) AS definition
FROM pg_proc p
JOIN pg_namespace n ON n.oid = p.pronamespace
WHERE n.nspname = 'skolenza'
  AND p.prokind IN ('f', 'p');

ALTER TABLE IF EXISTS skolenza."InstitutionBillingProfile" RENAME TO "TenantBillingProfile";
ALTER TABLE IF EXISTS skolenza."InstitutionBillingSubscription" RENAME TO "TenantBillingSubscription";
ALTER TABLE IF EXISTS skolenza."InstitutionCardMaster" RENAME TO "TenantCardMaster";
ALTER TABLE IF EXISTS skolenza."InstitutionDeviceConfiguration" RENAME TO "TenantDeviceConfiguration";
ALTER TABLE IF EXISTS skolenza."InstitutionDevice" RENAME TO "TenantDevice";
ALTER TABLE IF EXISTS skolenza."InstitutionEmailConfig" RENAME TO "TenantEmailConfig";
ALTER TABLE IF EXISTS skolenza."InstitutionEmailTemplate" RENAME TO "TenantEmailTemplate";
ALTER TABLE IF EXISTS skolenza."InstitutionStaffSectionDefault" RENAME TO "TenantEmployeeSectionDefault";
ALTER TABLE IF EXISTS skolenza."InstitutionEnabledModule" RENAME TO "TenantEnabledModule";
ALTER TABLE IF EXISTS skolenza."InstitutionEnabledOperation" RENAME TO "TenantEnabledOperation";
ALTER TABLE IF EXISTS skolenza."InstitutionEncryptionKeys" RENAME TO "TenantEncryptionKeys";
ALTER TABLE IF EXISTS skolenza."InstitutionType" RENAME TO "TenantIndustry";
ALTER TABLE IF EXISTS skolenza."Campus" RENAME TO "TenantLocation";
ALTER TABLE IF EXISTS skolenza."InstitutionProfile" RENAME TO "TenantProfile";
ALTER TABLE IF EXISTS skolenza."InstitutionSubscription" RENAME TO "TenantSubscription";
ALTER TABLE IF EXISTS skolenza."Institution" RENAME TO "Tenant";

ALTER SEQUENCE IF EXISTS skolenza."InstitutionBillingProfile_Id_seq" RENAME TO "TenantBillingProfile_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."InstitutionBillingSubscription_Id_seq" RENAME TO "TenantBillingSubscription_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."InstitutionCardMaster_Id_seq" RENAME TO "TenantCardMaster_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."InstitutionDeviceConfiguration_Id_seq" RENAME TO "TenantDeviceConfiguration_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."InstitutionDevice_Id_seq" RENAME TO "TenantDevice_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."InstitutionEmailConfig_Id_seq" RENAME TO "TenantEmailConfig_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."InstitutionEmailTemplate_Id_seq" RENAME TO "TenantEmailTemplate_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."InstitutionEnabledModule_Id_seq" RENAME TO "TenantEnabledModule_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."InstitutionEnabledOperation_Id_seq" RENAME TO "TenantEnabledOperation_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."InstitutionEncryptionKeys_Id_seq" RENAME TO "TenantEncryptionKeys_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."InstitutionType_Id_seq" RENAME TO "TenantIndustry_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."Campus_Id_seq" RENAME TO "TenantLocation_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."InstitutionProfile_Id_seq" RENAME TO "TenantProfile_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."InstitutionSubscription_Id_seq" RENAME TO "TenantSubscription_Id_seq";
ALTER SEQUENCE IF EXISTS skolenza."Institution_Id_seq" RENAME TO "Tenant_Id_seq";

DO $rollback$
DECLARE
    item record;
    updated_definition text;
BEGIN
    FOR item IN SELECT definition FROM tenant_function_definitions LOOP
        updated_definition := item.definition;
        updated_definition := replace(updated_definition, '"InstitutionBillingProfile"', '"TenantBillingProfile"');
        updated_definition := replace(updated_definition, '"InstitutionBillingSubscription"', '"TenantBillingSubscription"');
        updated_definition := replace(updated_definition, '"InstitutionCardMaster"', '"TenantCardMaster"');
        updated_definition := replace(updated_definition, '"InstitutionDeviceConfiguration"', '"TenantDeviceConfiguration"');
        updated_definition := replace(updated_definition, '"InstitutionDevice"', '"TenantDevice"');
        updated_definition := replace(updated_definition, '"InstitutionEmailConfig"', '"TenantEmailConfig"');
        updated_definition := replace(updated_definition, '"InstitutionEmailTemplate"', '"TenantEmailTemplate"');
        updated_definition := replace(updated_definition, '"InstitutionStaffSectionDefault"', '"TenantEmployeeSectionDefault"');
        updated_definition := replace(updated_definition, '"InstitutionEnabledModule"', '"TenantEnabledModule"');
        updated_definition := replace(updated_definition, '"InstitutionEnabledOperation"', '"TenantEnabledOperation"');
        updated_definition := replace(updated_definition, '"InstitutionEncryptionKeys"', '"TenantEncryptionKeys"');
        updated_definition := replace(updated_definition, '"InstitutionType"', '"TenantIndustry"');
        updated_definition := replace(updated_definition, '"Campus"', '"TenantLocation"');
        updated_definition := replace(updated_definition, '"InstitutionProfile"', '"TenantProfile"');
        updated_definition := replace(updated_definition, '"InstitutionSubscription"', '"TenantSubscription"');
        updated_definition := replace(updated_definition, '"Institution"', '"Tenant"');

        IF updated_definition IS DISTINCT FROM item.definition THEN
            EXECUTE updated_definition;
        END IF;
    END LOOP;
END
$rollback$;

COMMIT;
