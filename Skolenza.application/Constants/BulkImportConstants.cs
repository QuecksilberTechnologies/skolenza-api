namespace skolenza.application.Constants;

/// <summary>Central parsing limits and column contracts for master import previews.</summary>
public static class BulkImportConstants
{
    public const string DepartmentModuleCode = "INSTITUTION_DEPARTMENTS";
    public const string RetiredWorkTitleMessage = "This work-title import has been retired. Use Staff Professional Structure > Positions.";
    public const string RoleModuleCode = "INSTITUTION_ACCESS";
    public const string HostDeviceBulkModuleCode = "HOST_DEVICE_SETUP";
    public const string HostCardBulkModuleCode = "HOST_INSTITUTION_RFID_MANAGEMENT";
    public const string HostModuleBulkModuleCode = "HOST_MODULES";
    public const string HostSubModuleBulkModuleCode = "HOST_SUBMODULES";
    public const string HostOperationBulkModuleCode = "HOST_OPERATIONS";
    public const string HostModuleOperationBulkModuleCode = "HOST_MODULE_OPERATIONS";
    public const int MaxFileBytes = 5 * 1024 * 1024;
    public const int MaxExpandedBytes = 25 * 1024 * 1024;
    public const int MaxRows = 5000;
    public const int MaxColumns = 64;
    public const int MaxCellLength = 4000;
    public const string DepartmentName = "DepartmentName";
    public const string RoleName = "RoleName";
    public const string RoleType = "RoleType";
    public const string TypeName = "TypeName"; 
    public const string StaffEmploymentTypeModuleCode = "STAFF_EMPLOYMENT_TYPES";
    public const string StaffMemberCodeModuleCode = "INSTITUTION_STAFF_CODE";
    public const string StaffMemberModuleCode = "STAFF_DIRECTORY";
    public const string PolicyTypeModuleCode = "INSTITUTION_POLICY_TYPES";
    public const string PolicyDefinitionModuleCode = "INSTITUTION_POLICY_DEFINITIONS";
    public const string PolicyAssignmentModuleCode = "INSTITUTION_POLICY_ASSIGNMENTS";
    public const string StaffMemberCodeLockPrefix = "staffMember-code-pattern:";
    public const string StaffMemberLoginLockPrefix = "staffMember-login:";
    public const string StaffMemberCode = "StaffMemberCode";
    public static readonly string[] DepartmentColumns =
    [
        DepartmentName, Description, Remark, IsActive
    ];
    public static readonly string[] RoleColumns =
    [
        RoleName, RoleType, Remark, IsActive
    ];
    public static readonly string[] StaffEmploymentTypeColumns =
    [
        TypeName, Description, Remark, IsActive
    ];
    public static readonly string[] StaffMemberColumns =
    [
        StaffMemberCode, "FirstName", "LastName", "OfficialEmail", "DateOfBirth", "DateOfOnBoarding",
        "GenderId", "CountryId", "DepartmentId", "InstitutionStaffCategoryMasterId", "StaffPositionMasterId", "StaffEmploymentTypeId", "RoleId",
        "HasPermanent", IsActive, "MiddleName", "MobileNumber", "ContactName", "ContactNumber",
        "AlternateNumber", "ContactEmail", "ContactCountryId", "StateId", "DistrictId",
        "HouseNo", "Street", "LandMark", "Address"
    ];
    public static readonly string[] StaffMemberContactColumns =
    [
        "ContactName", "ContactNumber", "AlternateNumber", "ContactEmail", "ContactCountryId",
        "StateId", "DistrictId", "HouseNo", "Street", "LandMark", "Address"
    ];
    public const string Description = "Description";
    public const string Remark = "Remark";
    public const string IsActive = "IsActive";
    public static readonly string[] PolicyTypeColumns =
    [
        "PolicyTypeCode", "PolicyName", "PolicyCategoryCode", "Description",
        "DefaultCurrencyCode", IsActive
    ];
    public static readonly string[] PolicyDefinitionColumns =
    [
        "PolicyCode", "PolicyName", "PolicyTypeCode", "EffectiveFrom", "EffectiveTo",
        "Summary", "DefaultCurrencyCode", "RulesJson", "ApplicabilityJson"
    ];
    public static readonly string[] PolicyAssignmentColumns =
    [
        "PolicyCode", "VersionNumber", "StaffMemberCode", "EffectiveFrom", "EffectiveTo", "IsMandatory"
    ];
}
