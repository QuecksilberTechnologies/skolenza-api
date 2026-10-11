# Complete table dependency catalogue

Audited database: `workforcedb_34hi_duis`. Tables: 163. Foreign keys: 278.

| Table | Rows before reset | Parent tables | Direct dependent tables |
| --- | ---: | --- | --- |
| `Asset` | 0 | AssetStatus (AssetStatusId → Id)<br>AssetType (AssetTypeId → Id) | AssetAssignment (AssetId)<br>AssetImage (AssetId) |
| `AssetAssignment` | 0 | Asset (AssetId → Id)<br>AssetRequest (RequestId → Id) | None |
| `AssetCategory` | 0 | Tenant (TenantId → Id) | AssetType (AssetCategoryId) |
| `AssetImage` | 0 | Asset (AssetId → Id) | None |
| `AssetRequest` | 0 | None (root/master) | AssetAssignment (RequestId) |
| `AssetStatus` | 0 | None (root/master) | Asset (AssetStatusId) |
| `AssetType` | 0 | AssetCategory (AssetCategoryId → Id) | Asset (AssetTypeId) |
| `AssignmentStatus` | 0 | None (root/master) | None |
| `Attendance` | 0 | None (root/master) | None |
| `AttendanceDeviceType` | 4 | None (root/master) | EmployeeAttendancePunch (AttendanceDeviceTypeId)<br>EmployeeDailyAttendance (AttendanceDeviceTypeId)<br>UserAttendanceSetting (AttendanceDeviceTypeId) |
| `AttendancePolicy` | 0 | Tenant (TenantId → Id) | EmployeeWorkArrangement (AttendancePolicyId) |
| `AttendancePolicyVersionConfiguration` | 6 | PolicyVersion (PolicyVersionId → Id)<br>Tenant (TenantId → Id) | None |
| `AttendanceRequest` | 0 | None (root/master) | None |
| `BillingAuditLog` | 0 | None (root/master) | None |
| `BillingInvoice` | 0 | BillingOrder (BillingOrderId → Id)<br>Tenant (TenantId → Id) | BillingInvoiceLine (BillingInvoiceId) |
| `BillingInvoiceLine` | 0 | BillingInvoice (BillingInvoiceId → Id) | None |
| `BillingOrder` | 0 | PaymentGateway (PaymentGatewayId → Id)<br>SubscriptionPlanPrice (SubscriptionPlanPriceId → Id)<br>Tenant (TenantId → Id)<br>TenantBillingSubscription (TenantBillingSubscriptionId → Id) | BillingInvoice (BillingOrderId)<br>PaymentAttempt (BillingOrderId)<br>PaymentTransaction (BillingOrderId) |
| `BillingRefund` | 0 | PaymentTransaction (PaymentTransactionId → Id) | None |
| `BillingSubscriptionChange` | 0 | SubscriptionPlanPrice (FromSubscriptionPlanPriceId → Id)<br>SubscriptionPlanPrice (ToSubscriptionPlanPriceId → Id)<br>TenantBillingSubscription (TenantBillingSubscriptionId → Id) | None |
| `BillingTaxRule` | 1 | None (root/master) | None |
| `BulkImportJob` | 174 | None (root/master) | None |
| `Category` | 0 | Category (ParentID → Id) | Category (ParentID)<br>EmployeeCategorySkill (CategoryId) |
| `Client` | 10 | ClientType (ClientTypeId → Id) | None |
| `ClientType` | 12 | None (root/master) | Client (ClientTypeId)<br>Tender (ClientId) |
| `ComplianceRule` | 0 | ComplianceTypeMaster (ComplianceTypeId → Id)<br>Country (CountryId → Id)<br>State (StateId → Id)<br>Tenant (TenantId → Id) | None |
| `ComplianceTypeMaster` | 33 | None (root/master) | ComplianceRule (ComplianceTypeId)<br>SalaryComponentMaster (ComplianceTypeId) |
| `Country` | 4 | None (root/master) | ComplianceRule (CountryId)<br>CountryIdentityRule (CountryId)<br>CountryStatutoryRule (CountryId)<br>Employee (CountryId)<br>EmployeeTaxProfile (CountryId)<br>PolicyApplicability (CountryId)<br>SalaryComponentMaster (CountryId)<br>State (CountryId)<br>StatutoryType (CountryId)<br>TaxRegimeMaster (CountryId)<br>TaxRule (CountryId)<br>TaxSlab (CountryId)<br>TaxSystemMaster (CountryId)<br>TenantLocation (CountryId) |
| `CountryIdentityRule` | 0 | Country (CountryId → Id)<br>IdentityCategoryDocument (IdentityCategoryDocumentId → Id) | None |
| `CountryStatutoryRule` | 0 | Country (CountryId → Id)<br>StatutoryType (StatutoryTypeId → Id) | None |
| `DataViewStructure` | 2 | None (root/master) | ModuleOperationMapping (DataViewStructureId) |
| `DayCombination` | 0 | Tenant (TenantId → Id) | None |
| `DefaultEmailConfig` | 2 | None (root/master) | None |
| `Department` | 21 | Tenant (TenantId → Id) | Designation (DepartmentId)<br>EmployeeManagerMapping (DepartmentId)<br>Policy (OwnerDepartmentId)<br>PolicyApplicability (DepartmentId) |
| `Designation` | 10 | Department (DepartmentId → Id)<br>Tenant (TenantId → Id) | Employee (DesignationId)<br>EmployeeManagerMapping (DesignationId)<br>PolicyApplicability (DesignationId) |
| `DeviceCommand` | 0 | Tenant (TenantId → Id)<br>TenantDevice (TenantDeviceId → Id)<br>TenantLocation (TenantLocationId → Id) | DeviceCommandResponse (DeviceCommandId) |
| `DeviceCommandResponse` | 0 | DeviceCommand (DeviceCommandId → Id)<br>Tenant (TenantId → Id)<br>TenantDevice (TenantDeviceId → Id) | None |
| `DeviceCredential` | 0 | TenantDevice (TenantDeviceId → Id) | None |
| `DeviceInitialProvisioning` | 0 | DeviceMaster (DeviceMasterId → Id) | None |
| `DeviceMaster` | 3 | None (root/master) | DeviceInitialProvisioning (DeviceMasterId)<br>TenantDevice (DeviceMasterId) |
| `DeviceMessageLog` | 0 | Tenant (TenantId → Id)<br>TenantDevice (TenantDeviceId → Id) | None |
| `District` | 12458 | State (StateId → Id) | Locality (DistrictId)<br>PolicyApplicability (DistrictId)<br>TenantLocation (DistrictId) |
| `EmailQueue` | 0 | EmailTemplate (TemplateId → Id) | None |
| `EmailTemplate` | 5 | None (root/master) | EmailQueue (TemplateId) |
| `Employee` | 15 | Country (CountryId → Id)<br>Designation (DesignationId → Id)<br>EmployeeType (EmployeeTypeId → Id)<br>Gender (GenderId → Id)<br>Tenant (TenantId → Id) | EmployeeAttendancePunch (EmployeeId)<br>EmployeeBankDetail (EmployeeId)<br>EmployeeCategorySkill (EmployeeId)<br>EmployeeContact (EmployeeId)<br>EmployeeDailyAttendance (EmployeeId)<br>EmployeeDependent (EmployeeId)<br>EmployeeDeviceEnrollment (EmployeeId)<br>EmployeeExperience (EmployeeId)<br>EmployeeIdentity (EmployeeId)<br>EmployeeImage (EmployeeId)<br>EmployeeLeavePolicyMapping (EmployeeId)<br>EmployeeLocationAssignment (EmployeeId)<br>EmployeeManagerMapping (EmployeeId)<br>EmployeeManagerMapping (ManagerId)<br>EmployeePersonalDetail (EmployeeId)<br>EmployeePolicyEnrollment (EmployeeId)<br>EmployeeSalary (EmployeeId)<br>EmployeeTaxProfile (EmployeeId)<br>EmployeeWorkArrangement (EmployeeId)<br>EmployeeWorkModeOverrideRequest (EmployeeId)<br>EmployeesChangedTypeHistory (EmployeeId)<br>LeaveRequest (EmployeeId)<br>LoginCredential (EmployeeId)<br>PayrollEmployee (EmployeeId)<br>PolicyAcknowledgement (EmployeeId)<br>PolicyApplicability (EmployeeId)<br>PolicyAssignment (EmployeeId)<br>PolicyException (EmployeeId)<br>ThreadMessage (AddedById)<br>Ticket (ApprovedByUserId)<br>Ticket (AssignedToUserId)<br>Ticket (RecommendedByUserId)<br>Ticket (RequestedByUserId)<br>Ticket (RequestedForUserId)<br>TicketAttachment (UploadedByUserId)<br>TicketHistory (DoneByUserId)<br>UserAttendanceSetting (EmployeeId)<br>UserRole (EmployeeId) |
| `EmployeeAttendancePunch` | 0 | AttendanceDeviceType (AttendanceDeviceTypeId → Id)<br>Employee (EmployeeId → Id)<br>EmployeeWorkArrangement (EmployeeWorkArrangementId → Id)<br>PolicyVersion (PolicyVersionId → Id)<br>Tenant (TenantId → Id)<br>TenantLocation (TenantLocationId → Id) | None |
| `EmployeeBankDetail` | 1 | Employee (EmployeeId → Id) | None |
| `EmployeeCategorySkill` | 0 | Category (CategoryId → Id)<br>Employee (EmployeeId → Id) | None |
| `EmployeeCodePattern` | 3 | Tenant (TenantId → Id) | None |
| `EmployeeContact` | 7 | Employee (EmployeeId → Id) | None |
| `EmployeeDailyAttendance` | 0 | AttendanceDeviceType (AttendanceDeviceTypeId → Id)<br>Employee (EmployeeId → Id) | None |
| `EmployeeDependent` | 0 | Employee (EmployeeId → Id) | EmployeePolicyDependentMapping (DependentId) |
| `EmployeeDeviceEnrollment` | 0 | Employee (EmployeeId → Id)<br>Tenant (TenantId → Id) | None |
| `EmployeeEducation` | 0 | None (root/master) | None |
| `EmployeeExperience` | 1 | Employee (EmployeeId → Id) | EmployeeExperienceDocument (EmployeeExperienceId) |
| `EmployeeExperienceDocument` | 0 | EmployeeExperience (EmployeeExperienceId → Id) | None |
| `EmployeeIdentity` | 0 | Employee (EmployeeId → Id)<br>IdentityCategoryDocument (IdentityCategoryDocumentId → Id) | None |
| `EmployeeImage` | 15 | Employee (EmployeeId → Id) | None |
| `EmployeeLeaveBalance` | 0 | EmployeeLeavePolicyMapping (EmployeeLeavePolicyMappingId → Id)<br>Tenant (TenantId → Id) | None |
| `EmployeeLeavePolicyMapping` | 0 | Employee (EmployeeId → Id)<br>Tenant (TenantId → Id) | EmployeeLeaveBalance (EmployeeLeavePolicyMappingId) |
| `EmployeeLocationAssignment` | 11 | Employee (EmployeeId → Id)<br>Tenant (TenantId → Id)<br>TenantLocation (TenantLocationId → Id) | None |
| `EmployeeManagerMapping` | 0 | Department (DepartmentId → Id)<br>Designation (DesignationId → Id)<br>Employee (EmployeeId → Id)<br>Employee (ManagerId → Id)<br>ReportingType (ReportingTypeId → Id)<br>Tenant (TenantId → Id) | None |
| `EmployeePersonalDetail` | 0 | Employee (EmployeeId → Id) | None |
| `EmployeePolicyDependentMapping` | 0 | EmployeeDependent (DependentId → Id)<br>EmployeePolicyEnrollment (EmployeePolicyEnrollmentId → Id) | None |
| `EmployeePolicyEnrollment` | 0 | Employee (EmployeeId → Id) | EmployeePolicyDependentMapping (EmployeePolicyEnrollmentId) |
| `EmployeeSalary` | 0 | Employee (EmployeeId → Id)<br>SalaryStructure (SalaryStructureId → Id) | None |
| `EmployeeStatutoryAccount` | 0 | StatutoryType (StatutoryTypeId → Id) | None |
| `EmployeeTaxProfile` | 0 | Country (CountryId → Id)<br>Employee (EmployeeId → Id)<br>State (StateId → Id)<br>TaxRegimeMaster (RegimeId → Id) | None |
| `EmployeeType` | 15 | Tenant (TenantId → Id) | Employee (EmployeeTypeId)<br>EmployeeTypeBasicMenu (EmployeeTypeId)<br>EmployeesChangedTypeHistory (NewEmployeeTypeId)<br>EmployeesChangedTypeHistory (OldEmployeeTypeId)<br>PolicyApplicability (EmployeeTypeId) |
| `EmployeeTypeBasicMenu` | 0 | EmployeeType (EmployeeTypeId → Id) | None |
| `EmployeeWorkArrangement` | 6 | AttendancePolicy (AttendancePolicyId → Id)<br>Employee (EmployeeId → Id)<br>PolicyVersion (PolicyVersionId → Id)<br>Tenant (TenantId → Id)<br>TenantLocation (PrimaryTenantLocationId → Id) | EmployeeAttendancePunch (EmployeeWorkArrangementId)<br>EmployeeWorkModeOverrideRequest (EmployeeWorkArrangementId)<br>EmployeeWorkPattern (EmployeeWorkArrangementId) |
| `EmployeeWorkDocument` | 0 | EmployeeWorkHistory (EmployeeWorkHistoryId → Id)<br>WorkDocumentType (WorkDocumentTypeId → Id) | None |
| `EmployeeWorkHistory` | 0 | EmployeeWorkProfile (EmployeeWorkProfileId → Id) | EmployeeWorkDocument (EmployeeWorkHistoryId) |
| `EmployeeWorkModeOverrideRequest` | 4 | Employee (EmployeeId → Id)<br>EmployeeWorkArrangement (EmployeeWorkArrangementId → Id)<br>Tenant (TenantId → Id)<br>TenantLocation (TenantLocationId → Id) | None |
| `EmployeeWorkPattern` | 17 | EmployeeWorkArrangement (EmployeeWorkArrangementId → Id)<br>Tenant (TenantId → Id)<br>TenantLocation (TenantLocationId → Id) | None |
| `EmployeeWorkProfile` | 0 | None (root/master) | EmployeeWorkHistory (EmployeeWorkProfileId) |
| `EmployeesChangedTypeHistory` | 0 | Employee (EmployeeId → Id)<br>EmployeeType (NewEmployeeTypeId → Id)<br>EmployeeType (OldEmployeeTypeId → Id) | None |
| `ForgotPasswordOTPDetail` | 0 | None (root/master) | None |
| `Gender` | 3 | None (root/master) | Employee (GenderId)<br>PolicyApplicability (GenderId)<br>Tenant (GenderId) |
| `Holiday` | 35 | Tenant (TenantId → Id)<br>TenantLocation (TenantLocationId → Id) | None |
| `HostBillingConfiguration` | 1 | PaymentGateway (PaymentGatewayId → Id) | None |
| `HostRole` | 1 | None (root/master) | HostRoleModuleAndPermission (HostRoleId)<br>HostUser (HostRoleId) |
| `HostRoleModuleAndPermission` | 110 | HostRole (HostRoleId → Id)<br>Module (ModuleId → Id)<br>Operation (OperationId → Id) | None |
| `HostUser` | 2 | HostRole (HostRoleId → Id) | RefreshToken (HostUserId) |
| `IdentityCategory` | 3 | None (root/master) | IdentityCategoryDocument (IdentityCategoryId) |
| `IdentityCategoryDocument` | 4 | IdentityCategory (IdentityCategoryId → Id) | CountryIdentityRule (IdentityCategoryDocumentId)<br>EmployeeIdentity (IdentityCategoryDocumentId) |
| `LeaveRequest` | 0 | Employee (EmployeeId → Id)<br>LeaveType (LeaveTypeId → Id)<br>Tenant (TenantId → Id) | None |
| `LeaveType` | 0 | Tenant (TenantId → Id) | LeaveRequest (LeaveTypeId) |
| `License` | 1 | None (root/master) | None |
| `Locality` | 231016 | District (DistrictId → Id)<br>LocalityType (LocalityTypeId → Id)<br>State (StateId → Id) | PolicyApplicability (LocalityId)<br>TenantLocation (CityId) |
| `LocalityType` | 4 | None (root/master) | Locality (LocalityTypeId) |
| `LoginCredential` | 15 | Employee (EmployeeId → Id) | RefreshToken (LoginCredentialId) |
| `Module` | 90 | Module (ParentModuleId → Id) | HostRoleModuleAndPermission (ModuleId)<br>Module (ParentModuleId)<br>ModuleOperationMapping (ModuleId)<br>PlanModuleMapping (ModuleId)<br>RoleModuleAndPermission (ModuleId)<br>TenantEnabledModule (ModuleId)<br>TenantEnabledOperation (ModuleId) |
| `ModuleOperationMapping` | 331 | DataViewStructure (DataViewStructureId → Id)<br>Module (ModuleId → Id)<br>Operation (OperationId → Id)<br>PageTypeEnum (PageTypeId → Id) | None |
| `Operation` | 27 | None (root/master) | HostRoleModuleAndPermission (OperationId)<br>ModuleOperationMapping (OperationId)<br>RoleModuleAndPermission (OperationId)<br>TenantEnabledOperation (OperationId) |
| `PageTypeEnum` | 5 | None (root/master) | ModuleOperationMapping (PageTypeId) |
| `PaymentAttempt` | 0 | BillingOrder (BillingOrderId → Id) | None |
| `PaymentGateway` | 1 | None (root/master) | BillingOrder (PaymentGatewayId)<br>HostBillingConfiguration (PaymentGatewayId)<br>PaymentWebhookEvent (PaymentGatewayId)<br>TenantBillingSubscription (PaymentGatewayId) |
| `PaymentTransaction` | 0 | BillingOrder (BillingOrderId → Id) | BillingRefund (PaymentTransactionId) |
| `PaymentWebhookEvent` | 0 | PaymentGateway (PaymentGatewayId → Id) | None |
| `PayrollEmployee` | 0 | Employee (EmployeeId → Id)<br>PayrollRun (PayrollRunId → Id) | PayrollEmployeeDetail (PayrollEmployeeId) |
| `PayrollEmployeeDetail` | 0 | PayrollEmployee (PayrollEmployeeId → Id)<br>SalaryComponentMaster (ComponentId → Id) | None |
| `PayrollRun` | 0 | Tenant (TenantId → Id) | PayrollEmployee (PayrollRunId) |
| `PlanModuleMapping` | 136 | Module (ModuleId → Id)<br>SubscriptionPlan (SubscriptionPlanId → Id) | None |
| `Policy` | 13 | Department (OwnerDepartmentId → Id)<br>PolicyType (PolicyTypeId → Id)<br>Tenant (TenantId → Id) | PolicyChangeAudit (PolicyId)<br>PolicyVersion (PolicyId) |
| `PolicyAcknowledgement` | 3 | Employee (EmployeeId → Id)<br>PolicyVersion (PolicyVersionId → Id)<br>Tenant (TenantId → Id) | None |
| `PolicyApplicability` | 9 | Country (CountryId → Id)<br>Department (DepartmentId → Id)<br>Designation (DesignationId → Id)<br>District (DistrictId → Id)<br>Employee (EmployeeId → Id)<br>EmployeeType (EmployeeTypeId → Id)<br>Gender (GenderId → Id)<br>Locality (LocalityId → Id)<br>PolicyVersion (PolicyVersionId → Id)<br>State (StateId → Id)<br>Tenant (TenantId → Id)<br>TenantLocation (TenantLocationId → Id) | PolicyAssignment (SourceApplicabilityId) |
| `PolicyApprovalHistory` | 2 | PolicyApprovalStage (PolicyApprovalStageId → Id)<br>PolicyVersion (PolicyVersionId → Id)<br>Tenant (TenantId → Id) | None |
| `PolicyApprovalStage` | 2 | PolicyCategory (PolicyCategoryId → Id)<br>Role (ApproverRoleId → Id)<br>Tenant (TenantId → Id) | PolicyApprovalHistory (PolicyApprovalStageId) |
| `PolicyAssignment` | 3 | Employee (EmployeeId → Id)<br>PolicyApplicability (SourceApplicabilityId → Id)<br>PolicyVersion (PolicyVersionId → Id)<br>Tenant (TenantId → Id) | None |
| `PolicyCategory` | 12 | None (root/master) | PolicyApprovalStage (PolicyCategoryId)<br>PolicyType (PolicyCategoryId) |
| `PolicyChangeAudit` | 52 | Policy (PolicyId → Id)<br>PolicyVersion (PolicyVersionId → Id)<br>Tenant (TenantId → Id) | None |
| `PolicyDocument` | 0 | PolicyDocumentType (PolicyDocumentTypeId → Id)<br>PolicyVersion (PolicyVersionId → Id)<br>Tenant (TenantId → Id) | None |
| `PolicyDocumentType` | 5 | None (root/master) | PolicyDocument (PolicyDocumentTypeId) |
| `PolicyException` | 1 | Employee (EmployeeId → Id)<br>PolicyStatus (ApprovalStatusId → Id)<br>PolicyVersion (PolicyVersionId → Id)<br>Tenant (TenantId → Id) | None |
| `PolicyRule` | 19 | PolicyRuleType (PolicyRuleTypeId → Id)<br>PolicyVersion (PolicyVersionId → Id)<br>Tenant (TenantId → Id) | None |
| `PolicyRuleType` | 13 | None (root/master) | PolicyRule (PolicyRuleTypeId) |
| `PolicyStatus` | 7 | None (root/master) | PolicyException (ApprovalStatusId)<br>PolicyVersion (PolicyStatusId) |
| `PolicyType` | 13 | PolicyCategory (PolicyCategoryId → Id)<br>Tenant (TenantId → Id) | Policy (PolicyTypeId) |
| `PolicyVersion` | 16 | Policy (PolicyId → Id)<br>PolicyStatus (PolicyStatusId → Id)<br>Tenant (TenantId → Id) | AttendancePolicyVersionConfiguration (PolicyVersionId)<br>EmployeeAttendancePunch (PolicyVersionId)<br>EmployeeWorkArrangement (PolicyVersionId)<br>PolicyAcknowledgement (PolicyVersionId)<br>PolicyApplicability (PolicyVersionId)<br>PolicyApprovalHistory (PolicyVersionId)<br>PolicyAssignment (PolicyVersionId)<br>PolicyChangeAudit (PolicyVersionId)<br>PolicyDocument (PolicyVersionId)<br>PolicyException (PolicyVersionId)<br>PolicyRule (PolicyVersionId) |
| `RefreshToken` | 546 | HostUser (HostUserId → Id)<br>LoginCredential (LoginCredentialId → Id) | None |
| `ReportingType` | 0 | None (root/master) | EmployeeManagerMapping (ReportingTypeId) |
| `RequestType` | 0 | Tenant (TenantId → Id) | None |
| `Role` | 66 | None (root/master) | PolicyApprovalStage (ApproverRoleId)<br>RoleModuleAndPermission (RoleId)<br>Ticket (AssignedToRoleId)<br>TicketType (ApprovalRoleId)<br>TicketType (ResponsibleRoleId)<br>UserRole (RoleId) |
| `RoleModuleAndPermission` | 645 | Module (ModuleId → Id)<br>Operation (OperationId → Id)<br>Role (RoleId → Id) | None |
| `SalaryComponentMaster` | 0 | ComplianceTypeMaster (ComplianceTypeId → Id)<br>Country (CountryId → Id)<br>SalaryComponentMaster (DependsOnComponentId → Id)<br>State (StateId → Id)<br>Tenant (TenantId → Id) | PayrollEmployeeDetail (ComponentId)<br>SalaryComponentMaster (DependsOnComponentId)<br>SalaryStructureDetail (ComponentId)<br>SalaryStructureDetail (DependsOnComponentId) |
| `SalaryStructure` | 0 | Tenant (TenantId → Id) | EmployeeSalary (SalaryStructureId)<br>SalaryStructureDetail (SalaryStructureId) |
| `SalaryStructureDetail` | 0 | SalaryComponentMaster (ComponentId → Id)<br>SalaryComponentMaster (DependsOnComponentId → Id)<br>SalaryStructure (SalaryStructureId → Id) | None |
| `State` | 150 | Country (CountryId → Id) | ComplianceRule (StateId)<br>District (StateId)<br>EmployeeTaxProfile (StateId)<br>Locality (StateId)<br>PolicyApplicability (StateId)<br>SalaryComponentMaster (StateId)<br>TaxSlab (StateId) |
| `StatutoryType` | 0 | Country (CountryId → Id) | CountryStatutoryRule (StatutoryTypeId)<br>EmployeeStatutoryAccount (StatutoryTypeId) |
| `SubscriptionPlan` | 15 | None (root/master) | PlanModuleMapping (SubscriptionPlanId)<br>SubscriptionPlanPrice (SubscriptionPlanId)<br>TenantSubscription (SubscriptionPlanId) |
| `SubscriptionPlanPrice` | 0 | SubscriptionPlan (SubscriptionPlanId → Id) | BillingOrder (SubscriptionPlanPriceId)<br>BillingSubscriptionChange (FromSubscriptionPlanPriceId)<br>BillingSubscriptionChange (ToSubscriptionPlanPriceId)<br>TenantBillingSubscription (SubscriptionPlanPriceId) |
| `TaxRegimeMaster` | 0 | Country (CountryId → Id)<br>TaxSystemMaster (TaxSystemId → Id) | EmployeeTaxProfile (RegimeId)<br>TaxSlab (RegimeId) |
| `TaxRule` | 0 | Country (CountryId → Id)<br>Tenant (TenantId → Id) | None |
| `TaxSlab` | 0 | Country (CountryId → Id)<br>State (StateId → Id)<br>TaxRegimeMaster (RegimeId → Id)<br>Tenant (TenantId → Id) | None |
| `TaxSystemMaster` | 0 | Country (CountryId → Id) | TaxRegimeMaster (TaxSystemId) |
| `Tenant` | 3 | Gender (GenderId → Id)<br>TenantIndustry (TenantIndustryId → Id) | AssetCategory (TenantId)<br>AttendancePolicy (TenantId)<br>AttendancePolicyVersionConfiguration (TenantId)<br>BillingInvoice (TenantId)<br>BillingOrder (TenantId)<br>ComplianceRule (TenantId)<br>DayCombination (TenantId)<br>Department (TenantId)<br>Designation (TenantId)<br>DeviceCommand (TenantId)<br>DeviceCommandResponse (TenantId)<br>DeviceMessageLog (TenantId)<br>Employee (TenantId)<br>EmployeeAttendancePunch (TenantId)<br>EmployeeCodePattern (TenantId)<br>EmployeeDeviceEnrollment (TenantId)<br>EmployeeLeaveBalance (TenantId)<br>EmployeeLeavePolicyMapping (TenantId)<br>EmployeeLocationAssignment (TenantId)<br>EmployeeManagerMapping (TenantId)<br>EmployeeType (TenantId)<br>EmployeeWorkArrangement (TenantId)<br>EmployeeWorkModeOverrideRequest (TenantId)<br>EmployeeWorkPattern (TenantId)<br>Holiday (TenantId)<br>LeaveRequest (TenantId)<br>LeaveType (TenantId)<br>PayrollRun (TenantId)<br>Policy (TenantId)<br>PolicyAcknowledgement (TenantId)<br>PolicyApplicability (TenantId)<br>PolicyApprovalHistory (TenantId)<br>PolicyApprovalStage (TenantId)<br>PolicyAssignment (TenantId)<br>PolicyChangeAudit (TenantId)<br>PolicyDocument (TenantId)<br>PolicyException (TenantId)<br>PolicyRule (TenantId)<br>PolicyType (TenantId)<br>PolicyVersion (TenantId)<br>RequestType (TenantId)<br>SalaryComponentMaster (TenantId)<br>SalaryStructure (TenantId)<br>TaxRule (TenantId)<br>TaxSlab (TenantId)<br>TenantBillingProfile (TenantId)<br>TenantBillingSubscription (TenantId)<br>TenantCardMaster (TenantId)<br>TenantDevice (TenantId)<br>TenantEmailConfig (TenantId)<br>TenantEmailTemplate (TenantId)<br>TenantEnabledModule (TenantId)<br>TenantEnabledOperation (TenantId)<br>TenantLocation (TenantId)<br>TenantProfile (TenantId)<br>TenantSubscription (TenantId)<br>Ticket (TenantId)<br>TicketClassification (TenantId)<br>TicketHeader (TenantId)<br>TicketType (TenantId) |
| `TenantBillingProfile` | 0 | Tenant (TenantId → Id) | None |
| `TenantBillingSubscription` | 0 | PaymentGateway (PaymentGatewayId → Id)<br>SubscriptionPlanPrice (SubscriptionPlanPriceId → Id)<br>Tenant (TenantId → Id)<br>TenantSubscription (TenantSubscriptionId → Id) | BillingOrder (TenantBillingSubscriptionId)<br>BillingSubscriptionChange (TenantBillingSubscriptionId) |
| `TenantCardMaster` | 2 | Tenant (TenantId → Id) | None |
| `TenantDevice` | 3 | DeviceMaster (DeviceMasterId → Id)<br>Tenant (TenantId → Id)<br>TenantLocation (TenantId → Id)<br>TenantLocation (TenantId → TenantId)<br>TenantLocation (TenantLocationId → Id)<br>TenantLocation (TenantLocationId → TenantId) | DeviceCommand (TenantDeviceId)<br>DeviceCommandResponse (TenantDeviceId)<br>DeviceCredential (TenantDeviceId)<br>DeviceMessageLog (TenantDeviceId)<br>TenantDeviceConfiguration (TenantDeviceId) |
| `TenantDeviceConfiguration` | 0 | TenantDevice (TenantDeviceId → Id) | None |
| `TenantEmailConfig` | 1 | Tenant (TenantId → Id) | None |
| `TenantEmailTemplate` | 10 | Tenant (TenantId → Id) | None |
| `TenantEnabledModule` | 101 | Module (ModuleId → Id)<br>Tenant (TenantId → Id) | None |
| `TenantEnabledOperation` | 428 | Module (ModuleId → Id)<br>Operation (OperationId → Id)<br>Tenant (TenantId → Id) | None |
| `TenantEncryptionKeys` | 7 | None (root/master) | None |
| `TenantIndustry` | 5 | None (root/master) | Tenant (TenantIndustryId) |
| `TenantLocation` | 6 | Country (CountryId → Id)<br>District (DistrictId → Id)<br>Locality (CityId → Id)<br>Tenant (TenantId → Id) | DeviceCommand (TenantLocationId)<br>EmployeeAttendancePunch (TenantLocationId)<br>EmployeeLocationAssignment (TenantLocationId)<br>EmployeeWorkArrangement (PrimaryTenantLocationId)<br>EmployeeWorkModeOverrideRequest (TenantLocationId)<br>EmployeeWorkPattern (TenantLocationId)<br>Holiday (TenantLocationId)<br>PolicyApplicability (TenantLocationId)<br>TenantDevice (TenantId)<br>TenantDevice (TenantLocationId) |
| `TenantProfile` | 3 | Tenant (TenantId → Id) | None |
| `TenantSubscription` | 3 | SubscriptionPlan (SubscriptionPlanId → Id)<br>Tenant (TenantId → Id) | TenantBillingSubscription (TenantSubscriptionId) |
| `Tender` | 0 | ClientType (ClientId → Id)<br>TenderStatus (TenderStatusId → Id) | None |
| `TenderStatus` | 5 | None (root/master) | Tender (TenderStatusId) |
| `ThreadMessage` | 0 | Employee (AddedById → Id)<br>TicketThread (ThreadId → Id) | None |
| `Ticket` | 0 | Employee (ApprovedByUserId → Id)<br>Employee (AssignedToUserId → Id)<br>Employee (RecommendedByUserId → Id)<br>Employee (RequestedByUserId → Id)<br>Employee (RequestedForUserId → Id)<br>Role (AssignedToRoleId → Id)<br>Tenant (TenantId → Id)<br>TicketClassification (TicketClassificationId → Id)<br>TicketHeader (TicketHeaderId → Id)<br>TicketType (TicketTypeId → Id) | TicketHistory (TicketId) |
| `TicketAttachment` | 0 | Employee (UploadedByUserId → Id) | None |
| `TicketClassification` | 0 | Tenant (TenantId → Id) | Ticket (TicketClassificationId)<br>TicketHeader (TicketClassificationId) |
| `TicketHeader` | 0 | Tenant (TenantId → Id)<br>TicketClassification (TicketClassificationId → Id) | Ticket (TicketHeaderId)<br>TicketType (TicketHeaderId) |
| `TicketHistory` | 0 | Employee (DoneByUserId → Id)<br>Ticket (TicketId → Id) | None |
| `TicketThread` | 0 | None (root/master) | ThreadMessage (ThreadId) |
| `TicketType` | 0 | Role (ApprovalRoleId → Id)<br>Role (ResponsibleRoleId → Id)<br>Tenant (TenantId → Id)<br>TicketHeader (TicketHeaderId → Id) | Ticket (TicketTypeId) |
| `UserAttendanceSetting` | 0 | AttendanceDeviceType (AttendanceDeviceTypeId → Id)<br>Employee (EmployeeId → Id) | None |
| `UserRole` | 17 | Employee (EmployeeId → Id)<br>Role (RoleId → Id) | None |
| `WorkDocumentType` | 0 | None (root/master) | EmployeeWorkDocument (WorkDocumentTypeId) |
