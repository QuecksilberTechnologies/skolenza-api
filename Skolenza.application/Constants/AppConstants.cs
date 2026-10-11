// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines shared application constants and standardized error values.
// ================================================================
using System;
using skolenza.domain.Entity;

namespace skolenza.application.Constants
{
    public static class AppConstants
    {
        /// <summary>Shared input limits used by request validation.</summary>
        public static class ValidationLimits
        {
            public const int WorkModeOverrideTextMaxLength = 500;
        }

        #region Institution Profile Options

        public sealed record InstitutionOptionDefinition(
            int Id,
            string Code,
            string Name,
            string Description);

        /// <summary>Authoritative ownership/management categories stored in InstitutionProfile.BusinessType.</summary>
        public static IReadOnlyList<InstitutionOptionDefinition> InstitutionCategories { get; } =
        [
            new(1, "GOVERNMENT", "Government", "Owned and managed by a government authority."),
            new(2, "GOVERNMENT_AIDED", "Government Aided", "Privately managed with government aid."),
            new(3, "PRIVATE", "Private", "Privately owned and managed institution."),
            new(4, "AUTONOMOUS", "Autonomous", "Institution operating with autonomous academic or administrative status."),
            new(5, "TRUST_OR_SOCIETY", "Trust / Society", "Institution managed by a registered trust or society."),
            new(6, "PUBLIC_PRIVATE_PARTNERSHIP", "Public-Private Partnership", "Institution operated through a public-private partnership."),
            new(99, "OTHER", "Other", "Another recognized ownership or management category.")
        ];

        /// <summary>
        /// Education-specific choices stored in Campus.LocationType. Numeric values reuse the
        /// established enum so existing database, attendance and API contracts remain compatible.
        /// </summary>
        public static IReadOnlyList<InstitutionOptionDefinition> InstitutionCampusTypes { get; } =
        [
            new((int)CampusType.HeadOffice, "MAIN_CAMPUS", "Main Campus", "Primary campus and administrative headquarters."),
            new((int)CampusType.Branch, "BRANCH_CAMPUS", "Branch Campus", "Additional campus operating under the Institution."),
            new((int)CampusType.Campus, "ACADEMIC_CAMPUS", "Academic Campus", "Campus primarily used for teaching and learning."),
            new((int)CampusType.Office, "ADMINISTRATIVE_CAMPUS", "Administrative Campus", "Campus primarily used for administration."),
            new((int)CampusType.RemoteOffice, "VIRTUAL_OR_REMOTE_CAMPUS", "Virtual / Remote Campus", "Remote or online delivery campus.")
        ];

        public static bool IsSupportedInstitutionCategory(string? value) =>
            !string.IsNullOrWhiteSpace(value) &&
            InstitutionCategories.Any(option =>
                string.Equals(option.Name, value.Trim(), StringComparison.OrdinalIgnoreCase));

        public static bool IsSupportedInstitutionCampusType(short value) =>
            InstitutionCampusTypes.Any(option => option.Id == value);

        #endregion

        #region Staff Employment Type Options

        public sealed record StaffEmploymentTypeDefinition(
            short Id,
            string Code,
            string Name,
            string Description);

        /// <summary>
        /// Authoritative employment arrangements for staff members. These values describe
        /// how a staff member is engaged and remain separate from StaffCategory values such
        /// as Teaching, Non-Teaching, Technical, Administrative, and Support.
        /// </summary>
        public static IReadOnlyList<StaffEmploymentTypeDefinition> StaffEmploymentTypes { get; } =
        [
            new(1, "PERMANENT", "Permanent", "Staff member appointed on a permanent basis."),
            new(2, "CONTRACTUAL", "Contractual", "Staff member engaged for a fixed contractual term."),
            new(3, "GUEST", "Guest", "Guest faculty or visiting staff member.")
        ];

        public static bool IsSupportedStaffEmploymentType(short value) =>
            StaffEmploymentTypes.Any(option => option.Id == value);

        #endregion

        #region Staff Category Options

        public static class StaffCategories
        {
            public const short Teaching = 1;
            public const short NonTeaching = 2;
            public const short Technical = 3;
            public const short Administrative = 4;
            public const short Support = 5;
        }

        #endregion

        #region Error Codes

        /// <summary>
        /// Defines stable application error codes emitted by the exception middleware.
        /// </summary>
        public static class ErrorCodes
        {
            public const string Validation = "VALIDATION_ERROR";
            public const string Unauthorized = "UNAUTHORIZED";
            public const string Forbidden = "FORBIDDEN";
            public const string NotFound = "NOT_FOUND";
            public const string Conflict = "CONFLICT";
            public const string RoleHasDependencies = "ROLE_HAS_DEPENDENCIES";
            public const string DepartmentHasStaffMemberDependencies = "DEPARTMENT_HAS_STAFF_MEMBER_DEPENDENCIES";
            public const string DesignationHasStaffMemberDependencies = "DESIGNATION_HAS_STAFF_MEMBER_DEPENDENCIES";
            public const string InternalServerError = "INTERNAL_SERVER_ERROR";
            public const string EmailDeliveryFailed = "EMAIL_DELIVERY_FAILED";
            public const string SmtpIpNotAuthorized = "SMTP_IP_NOT_AUTHORIZED";
            public const string SmtpAuthenticationFailed = "SMTP_AUTHENTICATION_FAILED";
            public const string SmtpProviderRejected = "SMTP_PROVIDER_REJECTED";
            public const string SmtpConfigurationUnavailable = "SMTP_CONFIGURATION_UNAVAILABLE";
            public const string EmailTemplateUnavailable = "EMAIL_TEMPLATE_UNAVAILABLE";
            public const string SmtpConnectionFailed = "SMTP_CONNECTION_FAILED";
            public const string InstitutionRegistrationFailed = "INSTITUTION_REGISTRATION_FAILED";
        }

        #endregion

        #region Response Display Values

        /// <summary>
        /// Defines safe, professional placeholders for fields deliberately withheld by response-visibility rules.
        /// </summary>
        public static class ResponseValues
        {
            /// <summary>
            /// Controls the representation of withheld StaffMember text fields across all
            /// role-based response projections. Set this one value to <see langword="true"/>
            /// to return <c>Restricted</c>; leave it <see langword="false"/> to return null.
            /// </summary>
            public const bool UseRestrictedStaffMemberFieldPlaceholder = false;

            public const string Restricted = "Restricted";

            /// <summary>
            /// Gets the configured value for any StaffMember text field withheld by privacy rules.
            /// </summary>
            public static string? WithheldStaffMemberField =>
                UseRestrictedStaffMemberFieldPlaceholder ? Restricted : null;
        }

        #endregion

        #region Error Messages

        /// <summary>
        /// Defines reusable public messages for standardized application errors.
        /// </summary>
        public static class ErrorMessages
        {
            public const string InvalidRequest = "The request is invalid.";
            public const string InvalidIdentifier = "A valid identifier is required.";
            public const string Unauthorized = "The request is not authenticated.";
            public const string PermissionDenied = "You do not have permission to perform this action.";
            public const string ResourceNotFound = "The requested resource was not found.";
            public const string ResourceConflict = "The request conflicts with the current resource state.";
            public const string InternalServerError = "Something went wrong. Please try again.";
            public const string PasswordResetEmailNotSent = "The password reset email could not be sent. Please try again.";
            public const string SmtpIpNotAuthorized = "The email provider rejected the API server IP address. Authorize the server IP in the email provider and try again.";
            public const string SmtpAuthenticationFailed = "The email provider rejected the SMTP credentials. Verify the configured SMTP username and key.";
            public const string SmtpProviderRejected = "The email provider rejected the delivery request. Verify the SMTP sender and provider configuration.";
            public const string SmtpConfigurationUnavailable = "The active default SMTP configuration is incomplete or unavailable. Verify the Host email configuration.";
            public const string EmailTemplateUnavailable = "The required verification email template is missing or inactive.";
            public const string SmtpConnectionFailed = "The API server could not establish a valid connection with the SMTP provider. Verify the SMTP host, port, encryption, and network access.";
            public const string EmailDeliveryFailed = "The email could not be delivered because of an unexpected mail-service error. Review the API mail-service log and try again.";
            public const string RequiredDataMissing = "Required data is missing.";
            /// <summary>
            /// Indicates that the requested subscription plan is unavailable or already soft deleted.
            /// </summary>
            public const string SubscriptionPlanNotFound = "The requested subscription plan was not found.";

            /// <summary>
            /// Indicates that one or more submitted Modules cannot be assigned to a subscription plan.
            /// </summary>
            public const string InvalidSubscriptionPlanModule = "One or more selected modules are not eligible for the subscription plan.";

            /// <summary>
            /// Indicates that the requested Parent Module is unavailable in the requested scope.
            /// </summary>
            public const string ParentModuleNotFound = "The requested Parent Module was not found.";

            /// <summary>
            /// Indicates that verification cannot be resent for an already verified Institution.
            /// </summary>
            public const string InstitutionAlreadyVerified = "Institution is already verified.";

            /// <summary>
            /// Indicates that a Institution cannot receive onboarding verification because its active onboarding login is unavailable.
            /// </summary>
            public const string InstitutionOnboardingCredentialNotFound = "The Institution onboarding login was not found. Ensure the active onboarding login uses the Institution email address.";

            /// <summary>
            /// Indicates that a Institution onboarding login does not resolve to its corresponding staffMember record.
            /// </summary>
            public const string InstitutionOnboardingStaffMemberNotFound = "The Institution onboarding staffMember record was not found.";

            public const string CampusNotFound = "The requested institution location was not found.";
            public const string CampusInUse = "Institution location is currently in use. Remove or deactivate the dependent configuration before changing this location.";
            public const string AttendancePolicyNotFound = "The requested attendance policy was not found.";
            public const string AttendancePolicyInUse = "Attendance policy is currently assigned to an staffMember work arrangement. Unassign or deactivate the work arrangement first.";
            public const string StaffMemberLocationAssignmentNotFound = "The requested staffMember location assignment was not found.";
            public const string StaffMemberDeviceEnrollmentNotFound = "The requested staffMember device enrollment was not found.";
            public const string StaffMemberWorkArrangementNotFound = "The requested staffMember work arrangement was not found.";
            public const string StaffMemberWorkArrangementInUse = "StaffMember work arrangement has dependent configuration that must be removed or deactivated first.";
            public const string StaffMemberWorkPatternNotFound = "The requested staffMember work pattern was not found.";
            public const string StaffMemberWorkModeOverrideNotFound = "The requested work mode override request was not found.";
            public const string WorkPatternLocationRequired = "A working Office, Field, or Client Site pattern requires a compatible location.";
            public const string WorkPatternDayOffLocationNotAllowed = "A non-working day cannot have a work location.";
            public const string WorkPatternLocationAssignmentRequired = "The pattern location must have an active, attendance-allowed staffMember location assignment covering the work arrangement period.";
            public const string WorkPatternPolicyMismatch = "The work pattern mode and location are not allowed by the work arrangement's Attendance policy.";
            public const string WorkPatternModeMustMatchArrangement = "The weekday work mode must match the selected non-hybrid work arrangement.";
            public const string FlexibleHybridPatternNotAllowed = "Flexible hybrid arrangements use day quotas and cannot have fixed weekday patterns.";
            public const string WorkModeOverrideArrangementStaffMemberMismatch = "The selected work arrangement belongs to a different staffMember.";
            public const string WorkModeOverrideArrangementDateMismatch = "The override date range must be fully inside the selected work arrangement period.";
            public const string WorkModeOverrideLocationRequired = "Office, Field, and Client Site overrides require a compatible location.";
            public const string WorkModeOverrideLocationNotAllowed = "A Work From Home override cannot have a physical location.";
            public const string WorkModeOverrideLocationAssignmentRequired = "The override location must have an active, attendance-allowed staffMember location assignment covering the complete override period.";
            public const string WorkModeOverridePolicyMismatch = "The requested override mode and location are not allowed by the selected work arrangement's Attendance policy.";
            public const string WorkModeOverrideDateOverlap = "The requested override from {0} to {1} overlaps override #{2} from {3} to {4}. Pending and approved overrides for one staffMember cannot share a calendar date.";
            public const string WorkModeOverrideDecisionFinal = "Only a pending override can be approved or rejected.";
            public const string WorkModeOverrideRejectedRemarkRequired = "A rejection remark is required when rejecting an override.";
            public const string WorkModeOverrideReasonRequired = "A reason is required and cannot exceed 500 characters.";
            public const string WorkModeOverrideInactiveApproval = "Activate the override before approving it.";
            public const string InvalidInstitutionConfigurationReference = "One or more selected configuration references are invalid for this institution.";
            public const string DuplicateCampusCode = "A live institution location already uses this location code.";
            public const string DuplicateAttendancePolicyName = "A live attendance policy already uses this policy name.";
            public const string DuplicateStaffMemberLocationAssignment = "The staffMember already has this active location assignment.";
            public const string StaffMemberAlreadyHasPrimaryLocation = "The staffMember already has an active primary location assignment.";
            public const string StaffMemberLocationAssignmentInUse = "This location assignment is the primary location of an active work arrangement. Update the work arrangement first.";
            public const string StaffMemberWorkArrangementDateOverlap = "The requested work arrangement from {0} to {1} overlaps an existing active work arrangement from {2} to {3}. Change one period so they do not share any calendar date. Start and end dates are inclusive.";
            public const string StaffMemberWorkArrangementOpenEndedDateOverlap = "The requested work arrangement from {0} to {1} overlaps an existing active work arrangement that starts on {2} and has no end date. End the existing arrangement before the requested start date, or move the requested arrangement after the new end date. Start and end dates are inclusive.";
            public const string WorkArrangementPrimaryLocationAssignmentRequired = "The selected primary location must have an active, primary, attendance-allowed staffMember location assignment covering the full arrangement period.";
            public const string WorkArrangementStaffMemberNotFound = "The selected staffMember does not exist for this institution or has been deleted.";
            public const string WorkArrangementStaffMemberInactive = "The selected staffMember is inactive. Activate the staffMember before creating or activating a work arrangement.";
            public const string WorkArrangementLocationNotFound = "The selected primary location does not exist for this institution or has been deleted.";
            public const string WorkArrangementLocationInactive = "The selected primary location is inactive. Activate the location before using it in a work arrangement.";
            public const string WorkArrangementLocationTypeDetails = "The selected primary location has type '{0}', which is not valid for work mode '{1}'. Select a compatible location or change the work mode.";
            public const string WorkArrangementStaffMemberLocationMissing = "The selected location is not assigned to this staffMember. Add an staffMember location assignment first.";
            public const string WorkArrangementStaffMemberLocationInactive = "The staffMember location assignment exists but is inactive. Activate it before using this work arrangement.";
            public const string WorkArrangementStaffMemberLocationNotPrimary = "The staffMember location assignment is active but is not marked as Primary. Mark it as Primary before using it as the work arrangement's primary location.";
            public const string WorkArrangementStaffMemberLocationAttendanceDisabled = "Attendance is disabled on the staffMember location assignment. Enable Attendance allowed before using this work arrangement.";
            public const string WorkArrangementStaffMemberLocationStartsLate = "The staffMember location assignment starts on {0}, but the work arrangement starts on {1}. Move the location assignment start date to {1} or earlier, or start the work arrangement on {0} or later.";
            public const string WorkArrangementStaffMemberLocationEndsEarly = "The staffMember location assignment ends on {0}, but the work arrangement ends on {1}. Extend the location assignment through {1}, or end the work arrangement on {0} or earlier.";
            public const string WorkArrangementStaffMemberLocationMustBeOpenEnded = "The staffMember location assignment ends on {0}, but the work arrangement has no end date. Remove the location assignment end date or add a work arrangement end date on or before {0}.";
            public const string WorkArrangementPrimaryLocationRequired = "A primary location is required for Office, Hybrid, and Client Site work arrangements.";
            public const string WorkArrangementLocationTypeMismatch = "The selected primary location type is not valid for the selected work mode.";
            public const string WorkFromHomePrimaryLocationNotAllowed = "A Work From Home arrangement cannot have a physical primary location.";
            public const string DuplicateStaffMemberWorkPatternDay = "The work arrangement already has an active pattern for this day.";
            public const string InvalidEffectiveDateRange = "The end date cannot be earlier than the start date.";
            public const string InvalidHybridConfiguration = "Hybrid type is required only when work mode is Hybrid.";
            public const string InvalidAttendancePolicyVersion = "Select an active Published Attendance policy version that is effective on the arrangement start date.";
            public const string AttendancePolicyConfigurationRequired = "Attendance configuration is required for an Attendance policy version.";
            public const string AttendancePolicyChannelRequired = "An Attendance policy must allow at least one attendance channel.";
            public const string AttendancePolicyLocationScopeMismatch = "The selected work mode or primary location is not allowed by the Attendance policy location scope.";
            public const string InvalidWorkArrangementDayCounts = "Work arrangement day counts must be zero or greater and within their weekly or monthly limits.";
            public const string InvalidOverrideWorkMode = "Hybrid work mode is not allowed for a temporary override request.";
            public const string DeviceMasterNotFound = "The requested device master was not found.";
            public const string DuplicateDeviceMaster = "A live device master already uses this device code or company and model combination.";
            public const string DeviceMasterAlreadyRegisteredWithInstitution = "This device is already registered with a institution and cannot be updated, have its status changed, or be deleted.";
            public const string InstitutionDeviceNotFound = "The requested institution device was not found.";
            public const string InstitutionDeviceConfigurationNotFound = "The requested institution device configuration was not found.";
            public const string InstitutionDeviceConfigurationAlreadyExists = "This institution device already has a configuration.";
            public const string InstitutionDeviceConfigurationInUse = "Delete the institution device configuration before deleting the institution device.";
            public const string EmailTemplateNotFound = "The requested email template was not found.";
            public const string DuplicateEmailTemplateCode = "An email template with this code already exists.";
            public const string EmailTemplateMustBeInactiveToDelete = "Deactivate the email template before deleting it.";
            public const string EmailTemplateHasDeliveryHistory = "The email template cannot be deleted because it has queued or delivered email history.";
            public const string UnsupportedEmailTemplateCode = "Select a predefined email template code.";
            public const string DefaultEmailConfigNotFound = "The requested default email configuration was not found.";
            public const string DuplicateDefaultEmailConfigName = "A default email configuration with this name already exists.";
            public const string DefaultEmailConfigMustRemainActive = "At least one active email configuration is required for Institution registration.";
            public const string ActiveDefaultEmailConfigCannotBeDeleted = "Activate a replacement default email configuration before deleting the current active configuration.";
            public const string DefaultEmailConfigMustBeActiveToSetDefault = "Only an active default email configuration can be marked as the registration default.";
            public const string DefaultEmailConfigCannotClearDefault = "The current registration default cannot be unset directly. Mark another active email configuration as default first.";
            public const string DefaultEmailConfigDefaultRequired = "At least one active registration default email configuration is required before Institution registration.";
            public const string DefaultEmailConfigMustBeInactiveToDelete = "Deactivate this email configuration before deleting it.";
            public const string InstitutionEmailConfigNotFound = "The requested institution email configuration was not found.";
            public const string InstitutionEmailConfigMustBeInactiveToDelete = "Deactivate this institution email configuration before deleting it.";
            public const string DuplicateInstitutionDeviceCode = "A live institution device already uses this device code for the selected institution.";
            public const string RoleHasDependencies = "The role cannot be deactivated or deleted because it is assigned to an staffMember or has module-operation permissions. Remove or soft-delete all role dependencies first.";
            public const string DepartmentHasStaffMembers = "The department cannot be deactivated or deleted because it is assigned to one or more staffMembers. Remove or soft-delete all dependent staffMembers first.";
            public const string DesignationHasStaffMembers = "The designation cannot be deactivated or deleted because it is assigned to one or more staffMembers. Remove or soft-delete all dependent staffMembers first.";
            public const string StaffEmploymentTypeHasDependencies = "The StaffEmploymentType cannot be deleted because it is referenced by one or more non-soft-deleted records or StaffEmploymentType menu mappings.";
            public const string InvalidDeviceManagementInstitution = "The selected institution is unavailable or inactive.";
            public const string InvalidDeviceManagementCampus = "The selected institution location is unavailable or inactive.";
            public const string CampusDoesNotBelongToInstitution = "The selected institution location does not belong to the selected institution.";
            public const string InvalidDeviceMaster = "The selected device master is unavailable or inactive.";
            public const string InstitutionDeviceEnrollmentInUse = "Institution device has active staffMember enrollments that must be removed or deactivated first.";
        }

        #endregion

        #region Success Messages

        /// <summary>
        /// Defines success messages for response-boundary handlers.
        /// </summary>
        public static class SuccessMessages
        {
            /// <summary>
            /// Confirms that a Institution StaffMember session was created successfully.
            /// </summary>
            public const string LoginSuccessful = "Login successful.";

            /// <summary>
            /// Confirms that the shared Common navigation hierarchy was retrieved successfully.
            /// </summary>
            public const string CommonMenuRetrieved = "Common menu retrieved successfully.";

            /// <summary>
            /// Confirms that the authenticated user's permitted navigation hierarchy was retrieved successfully.
            /// </summary>
            public const string NavigationMenuRetrieved = "Navigation menu retrieved successfully.";

            /// <summary>
            /// Confirms that the active master feature pages were retrieved successfully.
            /// </summary>
            public const string FeaturePagesRetrievedSuccessfully = "Feature pages retrieved successfully.";

            /// <summary>
            /// Confirms that the current Institution staffMember authorization bootstrap was retrieved successfully.
            /// </summary>
            public const string InstitutionUserAccessRetrieved = "Institution user access retrieved successfully.";

            /// <summary>
            /// Confirms that the current Host authorization bootstrap was retrieved successfully.
            /// </summary>
            public const string HostAccessRetrieved = "Host access retrieved successfully.";

            public const string GenderOptionsRetrieved = "Gender options fetched successfully.";
            public const string ContactRelationOptionsRetrieved = "Contact relation options fetched successfully.";
            public const string RoleOptionsRetrieved = "Role options fetched successfully.";
            public const string RolesRetrieved = "Roles retrieved successfully.";
            public const string RolePermissionsRetrieved = "Role permissions retrieved successfully.";
            public const string RolePermissionsUpdated = "Role permissions updated successfully.";
            public const string CountriesRetrieved = "Countries fetched successfully.";
            public const string StatesRetrieved = "States fetched successfully.";
            public const string DistrictsRetrieved = "Districts fetched successfully.";
            public const string LocalitiesRetrieved = "Localities fetched successfully.";
            public const string LocalityTypesRetrieved = "Locality types fetched successfully.";
            public const string ConsumedInsurancePoliciesRetrieved = "Insurance policies fetched successfully.";
            public const string WorkflowStageDeleted = "Workflow stage deleted successfully.";
            public const string StaffEmploymentTypesRetrieved = "StaffMember types fetched successfully.";
            public const string MenuDisplayStructureRetrieved = "Menu display structure fetched successfully.";

            /// <summary>
            /// Confirms that a subscription plan was soft deleted successfully.
            /// </summary>
            public const string SubscriptionPlanDeletedSuccessfully = "Subscription plan deleted successfully.";

            /// <summary>
            /// Confirms that subscription plans were retrieved successfully.
            /// </summary>
            public const string SubscriptionPlansRetrievedSuccessfully = "Subscription plans retrieved successfully.";

            /// <summary>
            /// Confirms that a subscription plan was created successfully.
            /// </summary>
            public const string SubscriptionPlanCreatedSuccessfully = "Subscription plan created successfully.";

            /// <summary>
            /// Confirms that a subscription plan was updated successfully.
            /// </summary>
            public const string SubscriptionPlanUpdatedSuccessfully = "Subscription plan updated successfully.";

            /// <summary>
            /// Confirms that institution subscription information was retrieved successfully.
            /// </summary>
            public const string InstitutionSubscriptionPlanRetrievedSuccessfully = "Institution subscription plan retrieved successfully.";

            /// <summary>
            /// Confirms that modules available to a subscription plan were retrieved successfully.
            /// </summary>
            public const string SubscriptionPlanModulesRetrievedSuccessfully = "Subscription plan modules retrieved successfully.";

            /// <summary>
            /// Confirms that selectable Module options for a subscription plan were retrieved successfully.
            /// </summary>
            public const string SubscriptionPlanModuleOptionsRetrievedSuccessfully = "Subscription plan module options retrieved successfully.";

            /// <summary>
            /// Confirms that a subscription plan's Module mapping was synchronized successfully.
            /// </summary>
            public const string SubscriptionPlanModuleMappingSavedSuccessfully = "Subscription plan module mapping saved successfully.";

            /// <summary>
            /// Confirms that a Parent Module status cascade completed successfully.
            /// </summary>
            public const string ParentModuleStatusUpdatedSuccessfully = "Parent Module status updated successfully.";

            /// <summary>
            /// Confirms that Host-managed Institution details were updated successfully.
            /// </summary>
            public const string InstitutionUpdatedSuccessfully = "Institution updated successfully.";

            /// <summary>
            /// Confirms that missing active-plan entitlement snapshot rows were synchronized for a Institution.
            /// </summary>
            public const string InstitutionPlanEntitlementsSynchronizedSuccessfully = "Institution plan entitlements synchronized successfully.";

            /// <summary>
            /// Confirms that a Institution was soft deleted successfully.
            /// </summary>
            public const string InstitutionDeletedSuccessfully = "Institution deleted successfully.";

            /// <summary>
            /// Confirms that a Institution was activated successfully.
            /// </summary>
            public const string InstitutionActivatedSuccessfully = "Institution activated successfully.";

            /// <summary>
            /// Confirms that a Institution was deactivated successfully.
            /// </summary>
            public const string InstitutionDeactivatedSuccessfully = "Institution deactivated successfully.";

            /// <summary>
            /// Confirms that a Institution verification welcome email was sent successfully.
            /// </summary>
            public const string InstitutionVerificationResentSuccessfully = "Institution verification email sent successfully.";

            public const string CampusCreated = "Institution location created successfully.";
            public const string CampusUpdated = "Institution location updated successfully.";
            public const string CampusStatusUpdated = "Institution location status updated successfully.";
            public const string CampusDeleted = "Institution location deleted successfully.";
            public const string CampusRetrieved = "Institution locations retrieved successfully.";
            public const string AttendancePolicyCreated = "Attendance policy created successfully.";
            public const string AttendancePolicyUpdated = "Attendance policy updated successfully.";
            public const string AttendancePolicyStatusUpdated = "Attendance policy status updated successfully.";
            public const string AttendancePolicyDeleted = "Attendance policy deleted successfully.";
            public const string AttendancePolicyRetrieved = "Attendance policies retrieved successfully.";
            public const string StaffMemberLocationAssignmentCreated = "StaffMember location assignment created successfully.";
            public const string StaffMemberLocationAssignmentUpdated = "StaffMember location assignment updated successfully.";
            public const string StaffMemberLocationAssignmentStatusUpdated = "StaffMember location assignment status updated successfully.";
            public const string StaffMemberLocationAssignmentDeleted = "StaffMember location assignment deleted successfully.";
            public const string StaffMemberDeviceEnrollmentCreated = "StaffMember device enrollment created successfully.";
            public const string StaffMemberDeviceEnrollmentUpdated = "StaffMember device enrollment updated successfully.";
            public const string StaffMemberDeviceEnrollmentStatusUpdated = "StaffMember device enrollment status updated successfully.";
            public const string StaffMemberDeviceEnrollmentDeleted = "StaffMember device enrollment deleted successfully.";
            public const string StaffMemberWorkArrangementCreated = "StaffMember work arrangement created successfully.";
            public const string StaffMemberWorkArrangementUpdated = "StaffMember work arrangement updated successfully.";
            public const string StaffMemberWorkArrangementStatusUpdated = "StaffMember work arrangement status updated successfully.";
            public const string StaffMemberWorkArrangementDeleted = "StaffMember work arrangement deleted successfully.";
            public const string StaffMemberWorkPatternCreated = "StaffMember work pattern created successfully.";
            public const string StaffMemberWorkPatternUpdated = "StaffMember work pattern updated successfully.";
            public const string StaffMemberWorkPatternStatusUpdated = "StaffMember work pattern status updated successfully.";
            public const string StaffMemberWorkPatternDeleted = "StaffMember work pattern deleted successfully.";
            public const string StaffMemberWorkModeOverrideCreated = "Work mode override request created successfully.";
            public const string StaffMemberWorkModeOverrideUpdated = "Work mode override request updated successfully.";
            public const string StaffMemberWorkModeOverrideStatusUpdated = "Work mode override request status updated successfully.";
            public const string StaffMemberWorkModeOverrideDeleted = "Work mode override request deleted successfully.";
            public const string StaffMemberWorkModeOverrideApproved = "Work mode override request approved successfully.";
            public const string StaffMemberWorkModeOverrideRejected = "Work mode override request rejected successfully.";
            public const string DeviceMasterCreated = "Device master created successfully.";
            public const string DeviceMasterUpdated = "Device master updated successfully.";
            public const string DeviceMasterStatusUpdated = "Device master status updated successfully.";
            public const string DeviceMasterDeleted = "Device master deleted successfully.";
            public const string DeviceMasterRetrieved = "Device masters retrieved successfully.";
            public const string InstitutionDeviceCreated = "Institution device created successfully.";
            public const string InstitutionDeviceUpdated = "Institution device updated successfully.";
            public const string InstitutionDeviceStatusUpdated = "Institution device status updated successfully.";
            public const string InstitutionDeviceDeleted = "Institution device deleted successfully.";
            public const string InstitutionDeviceRetrieved = "Institution devices retrieved successfully.";
            public const string InstitutionDeviceConfigurationCreated = "Institution device configuration created successfully.";
            public const string InstitutionDeviceConfigurationUpdated = "Institution device configuration updated successfully.";
            public const string InstitutionDeviceConfigurationDeleted = "Institution device configuration deleted successfully.";
            public const string InstitutionDeviceConfigurationRetrieved = "Institution device configurations retrieved successfully.";
            public const string EmailTemplateCreated = "Email template created successfully.";
            public const string EmailTemplateUpdated = "Email template updated successfully.";
            public const string EmailTemplateStatusUpdated = "Email template status updated successfully.";
            public const string EmailTemplateDeleted = "Email template deleted successfully.";
            public const string EmailTemplateRetrieved = "Email templates retrieved successfully.";
            public const string InstitutionEmailTemplatesSynchronized = "Missing institution email templates synchronized successfully.";
            public const string DefaultEmailConfigCreated = "Default email configuration created successfully.";
            public const string DefaultEmailConfigUpdated = "Default email configuration updated successfully.";
            public const string DefaultEmailConfigDeleted = "Default email configuration deleted successfully.";
            public const string DefaultEmailConfigRetrieved = "Default email configuration retrieved successfully.";
            public const string InstitutionEmailConfigCreated = "Institution email configuration created successfully.";
            public const string InstitutionEmailConfigUpdated = "Institution email configuration updated successfully.";
            public const string InstitutionEmailConfigDeleted = "Institution email configuration deleted successfully.";
            public const string InstitutionEmailConfigRetrieved = "Institution email configurations retrieved successfully.";
        }

        #endregion

        #region Application Limits

        /// <summary>
        /// Defines the maximum number of roles that may be assigned to one StaffMember.
        /// </summary>
        public const int MaxStaffMemberRoleAssigned = 2;

        #endregion

        #region Module Scopes

        /// <summary>
        /// Identifies modules that are available in a institution application scope.
        /// </summary>
        public const int InstitutionModuleScope = 1;

        /// <summary>
        /// Identifies modules that are available in the Host application scope.
        /// </summary>
        public const int HostModuleScope = 2;

        /// <summary>
        /// Identifies modules that are shared by authenticated Host and Institution applications.
        /// </summary>
        public const int CommonModuleScope = 3;

        #endregion

        #region Host Token Claims

        /// <summary>
        /// Identifies the Host-user identifier claim in a Host access token.
        /// </summary>
        public const string HostUserIdClaim = "HostUserId";

        /// <summary>
        /// Identifies the Host-role identifier claim in a Host access token.
        /// </summary>
        public const string HostRoleIdClaim = "HostRoleId";

        /// <summary>
        /// Identifies the Host login identifier claim in a Host access token.
        /// </summary>
        public const string LoginIdClaim = "LoginId";

        /// <summary>
        /// Identifies the application principal-type claim.
        /// </summary>
        public const string UserTypeClaim = "UserType";

        /// <summary>
        /// Identifies a Host principal in the application principal-type claim.
        /// </summary>
        public const string HostUserType = "Host";

        /// <summary>
        /// Identifies a Institution staffMember principal in the application principal-type claim.
        /// </summary>
        public const string InstitutionUserType = "Institution";

        /// <summary>
        /// Identifies the verified current Host role authorized to administer Parent Modules.
        /// </summary>
        public const long SuperAdminHostRoleId = 1;

        /// <summary>
        /// Identifies an access token in the token-purpose claim.
        /// </summary>
        public const string AccessTokenPurpose = "Access";

        #endregion

        #region Policy Codes

        /// <summary>Defines stable seeded Policy Category codes used for relationship-based lookup.</summary>
        public static class PolicyCategoryCodes
        {
            public const string Attendance = "ATTENDANCE";
        }

        /// <summary>Defines stable seeded Policy Status codes used for lifecycle filtering.</summary>
        public static class PolicyStatusCodes
        {
            public const string Draft = "DRAFT";
            public const string UnderReview = "UNDER_REVIEW";
            public const string Approved = "APPROVED";
            public const string Published = "PUBLISHED";
            public const string Archived = "ARCHIVED";
            public const string Rejected = "REJECTED";
        }

        /// <summary>Defines stable staffMember-calendar event keys returned to UI clients.</summary>
        public static class CalendarStatusCodes
        {
            public const string MandatoryHoliday = "MANDATORY_HOLIDAY";
            public const string OptionalHoliday = "OPTIONAL_HOLIDAY";
            public const string Pending = "PENDING";
            public const string Approved = "APPROVED";
            public const string OnLeave = "ON_LEAVE";
            public const string Taken = "TAKEN";
            public const string Rejected = "REJECTED";
            public const string Cancelled = "CANCELLED";
            public const string WeeklyOff = "WEEKLY_OFF";
        }

        /// <summary>Defines the shared staffMember-calendar display palette.</summary>
        public static class CalendarColors
        {
            public const string MandatoryHoliday = "#DC2626";
            public const string OptionalHoliday = "#F59E0B";
            public const string Pending = "#EAB308";
            public const string Approved = "#2563EB";
            public const string OnLeave = "#16A34A";
            public const string Taken = "#16A34A";
            public const string Rejected = "#6B7280";
            public const string Cancelled = "#9CA3AF";
            public const string WeeklyOff = "#D1D5DB";
            public const string LightText = "#FFFFFF";
            public const string DarkText = "#111827";
        }

        #endregion
    }


    public static class ConstantValues
    {
        #region Contact Types

        public enum ContactTypeEnum
        {
            None = 0,
            Personal = 1,
            Official = 2
        }

        #endregion

        #region Storage Folders

        public const string InstitutionFolder = "institutions";
        public const string StaffMemberFolder = "staffMembers";
        public const string ProfileFolder = "profile";
        public const string AssetsFolder = "assets";
        public const string BankFolder = "bank";
        public const string PoliciesFolder = "policies";
        public const string DependentFolder = "dependent";
        public const string EducationFolder = "education";
        public const string ExperienceFolder = "experience";

        #endregion

        #region Authentication And System Values

        public static readonly string invalidCredential = "Invalid credentials";
        public static readonly int ParmanentStaffEmploymentType = 1;
        public static readonly bool isSucceeded = true;
        public static readonly string IP = "100.100.100.100";
        public static readonly int Web = 1;

        #endregion

        #region Institution Location Defaults

        public static readonly string InitialCampusCode = "PRIMARY";
        public static readonly string InitialCampusTimeZoneId = "UTC";

        #endregion

        #region Institution Role Types

        public static readonly string InstitutionAdminRoleName = "Institution Administrator";
        public static readonly string InstitutionAdminRoleDisplayName = "Institution Administrator";
        public static readonly string InstitutionAdminRoleOptionName = "Institution Administrator";
        public static readonly string InstitutionManagerRoleName = "People Manager";
        public static readonly string InstitutionStaffMemberRoleName = "Workforce Member";
        public static readonly string InstitutionExternalRoleName = "External Collaborator";
        public static readonly string InstitutionStaffMemberRoleDisplayName = "Workforce User";
        public static readonly string InstitutionManagerRoleDisplayName = "People Manager";
        public static readonly string InstitutionExternalRoleDisplayName = "External User";
        public static readonly string UnknownRoleTypeDisplayName = "Unknown";

        public static readonly string InstitutionAdminRoleDescription =
            "Full company workspace administration, including users, roles, settings, and institution-level configuration.";
        public static readonly string InstitutionStaffMemberRoleDescription =
            "StaffMember self-service access to personal information, attendance, leave, documents, and assigned work features.";
        public static readonly string InstitutionManagerRoleDescription =
            "Team management access for supervisors and managers, subject to assigned role permissions.";
        public static readonly string InstitutionExternalRoleDescription =
            "Limited portal access for clients, consultants, vendors, and other external users, subject to assigned role permissions.";

        public static readonly string InstitutionAdminRoleRemark =
            "Company workspace, users, roles and settings administration.";
        public static readonly string InstitutionStaffMemberRoleRemark =
            "StaffMember self-service, attendance, leave, documents and assigned work.";
        public static readonly string InstitutionManagerRoleRemark =
            "Team and supervisor management according to assigned permissions.";
        public static readonly string InstitutionExternalRoleRemark =
            "Client, consultant, vendor and external portal access.";

        public static readonly int RoleTypeAdmin = 1;
        public static readonly int RoleTypeStaffMember = 2;
        public static readonly int RoleTypeManager = 3;
        public static readonly int RoleTypeClient = 4;

        public const string RoleTypeAdminCode = "INSTITUTION_ADMIN";
        public const string RoleTypeStaffMemberCode = "WORKFORCE_USER";
        public const string RoleTypeManagerCode = "PEOPLE_MANAGER";
        public const string RoleTypeClientCode = "EXTERNAL_USER";

        /// <summary>
        /// Resolves the stable code for a persisted Institution role type.
        /// </summary>
        public static string GetRoleTypeCode(int roleType) => roleType switch
        {
            var value when value == RoleTypeAdmin => RoleTypeAdminCode,
            var value when value == RoleTypeStaffMember => RoleTypeStaffMemberCode,
            var value when value == RoleTypeManager => RoleTypeManagerCode,
            var value when value == RoleTypeClient => RoleTypeClientCode,
            _ => string.Empty
        };

        /// <summary>
        /// Resolves the stable display name for a persisted Institution role type.
        /// </summary>
        public static string GetRoleTypeDisplayName(int roleType) => roleType switch
        {
            var value when value == RoleTypeAdmin => InstitutionAdminRoleDisplayName,
            var value when value == RoleTypeStaffMember => InstitutionStaffMemberRoleDisplayName,
            var value when value == RoleTypeManager => InstitutionManagerRoleDisplayName,
            var value when value == RoleTypeClient => InstitutionExternalRoleDisplayName,
            _ => UnknownRoleTypeDisplayName
        };

        /// <summary>
        /// Resolves the default remark for a persisted Institution role type.
        /// </summary>
        public static string GetRoleTypeRemark(int roleType) => roleType switch
        {
            var value when value == RoleTypeAdmin => InstitutionAdminRoleRemark,
            var value when value == RoleTypeStaffMember => InstitutionStaffMemberRoleRemark,
            var value when value == RoleTypeManager => InstitutionManagerRoleRemark,
            var value when value == RoleTypeClient => InstitutionExternalRoleRemark,
            _ => InstitutionAllRoleRemark
        };

        /// <summary>
        /// Preserves a user-authored remark and upgrades the legacy shared default remark.
        /// </summary>
        public static string GetRoleRemarkForDisplay(int roleType, string? remark)
        {
            return string.IsNullOrWhiteSpace(remark) || remark == InstitutionAllRoleRemark
                ? GetRoleTypeRemark(roleType)
                : remark;
        }

        public static readonly string InstitutionAllRoleRemark = "This is an auto-generated Admin account by AI for the initial setup of the institution.";

        #endregion

        #region Token Purposes

        public static readonly int SetPassword = 1;
        public static readonly int Auth = 2;

        #endregion

        #region Default Record Values

        public static readonly bool IsByDefaultTrue = true;
        public static readonly bool IsByDefaultFalse = false;

        #endregion

        #region Email Template Codes

        public static readonly string WelcomeEmail = "WELCOME_EMAIL";
        public static readonly string ForgotPasswordEmail = "FORGOT_PASSWORD";
        public static readonly string BirthdayWishEmail = "BIRTHDAY_WISH";
        public static readonly string LeaveApprovalEmail = "LEAVE_APPROVAL";
        public static readonly string AccountVerificationEmail = "ACCOUNT_VERIFICATION";

        private static readonly HashSet<string> SupportedEmailTemplateCodes = new(
            StringComparer.OrdinalIgnoreCase)
        {
            WelcomeEmail,
            ForgotPasswordEmail,
            BirthdayWishEmail,
            LeaveApprovalEmail,
            AccountVerificationEmail
        };

        public static bool IsSupportedEmailTemplateCode(string? templateCode) =>
            !string.IsNullOrWhiteSpace(templateCode) &&
            SupportedEmailTemplateCodes.Contains(templateCode.Trim());

        #endregion

        #region Attendance Device Types

        public const string AttendanceDeviceMobile = "MOBILE";
        public const string AttendanceDeviceWeb = "WEB";

        #endregion
    }

}
