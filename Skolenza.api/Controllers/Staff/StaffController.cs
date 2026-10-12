// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Exposes staffMember endpoints and delegates application errors to middleware.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Staff.Bank;
using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.application.DTOS.Staff.ResetPassword;
using skolenza.application.Features.StaffMemberCmd.StaffMemberBase.Handlers;
using skolenza.application.Features.StaffMemberCmd.ResetPassword.Handlers;
using skolenza.application.Features.StaffMemberCmd.UpdateStatus.Handler;
using skolenza.application.Features.StaffMemberCmd.UpdateVerification.Handler;
using skolenza.application.Interfaces.ILogger;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace skolenza.api.Controllers.Staff
{
    /// <summary>
    /// Handles all Staff Member related operations like create, update, delete, and view.
    /// </summary>
    [Route("api/Staff")]
    [ApiController]
    public partial class StaffController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILoggerService _logger;

        /// <summary>Sets edit defaults for Work Locations, Devices, Work Arrangement, Work Pattern and Overrides.</summary>
        /// <remarks>Requires institution Admin plus existing STAFF_DIRECTORY action permission. Applies to current and future staffMembers.</remarks>
        [Authorize]
        [HttpPost("section-defaults")]
        public async Task<IActionResult> UpdateSectionDefaults(
            [FromBody] UpdateStaffMemberSectionDefaultsRequestDTO dto,
            CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new UpdateStaffMemberSectionDefaultsCommand(dto), cancellationToken));
        }

        public StaffController(IMediator mediator, ILoggerService logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Validates IMEI number. Must be 15 digits and numeric only.
        /// </summary>


        /// <summary>
        /// Used-In-Angular: creates staffMember.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: creates base staffMember info.</para>
        /// <para>Handler flow: CreateBaseStaffMemberInfoCommand is processed by CreateBaseStaffMemberInfoCommandHandler; operation(s): GetStaffMemberIdByUserLogin, GetRoleAsync, CreateStaffMemberAsync, AddMinutes.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetBaseStaffMemberResponseDTO: Id (string?), EmployementCode (string?), LastName (string?), MiddleName (string?), FirstName (string?), GenderId (int), DesignationName (string?), DepartmentName (string?), EmergencyContactPerson (string?), EmergencyContactNumber (string?), BloodGroup (string?), MobileNumber (string?), Relation (int?), RelationType (string?), CountryId (int?), CountryCode (string?), Nationality (string?), RoleName (string?), RoleId (int?), RoleType (int?), Type (string?), DateOfBirth (DateTime?), DateOfOnBoarding (DateTime?), DateOfExit (DateTime?), DesignationId (int), GenderName (string?), StaffEmploymentTypeId (int), StaffCategoryId (short?), DepartmentId (int), OfficialEmail (string?), HasPermanent (bool?), IsActive (bool), IsEditAllowed (bool?), IsMarried (bool?), IsInfoVerified (bool?), CompletionPercentage (double)</para>
        /// <para>Angular function(s): StaffMembersApi.createStaffMember (app/core/services/staffMember-api.ts:95).</para>
        /// <para>Angular purpose: creates staffMember.</para>
        /// <para>Integrated UI page(s): /app/staffMembers; /app/profile/basic-info</para>
        /// <para>Angular UI component(s): StaffMemberManageDialog (app/shared/components/staffMember/staffMember-manage-dialog/staffMember-manage-dialog.ts); StaffMembers (app/features/staffMembers/staffMembers.ts); StaffMemberBasicInfo (app/features/user-menu/staffMember-profile/staffMember-basic-info/staffMember-basic-info.ts)</para>
        /// </remarks>
        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> CreateStaffMember([FromBody] CreateBaseStaffMemberRequestDTO staffMemberCreateDto)
        {
              // ✅ IMEI validation
                if (staffMemberCreateDto == null)
                {
                    _logger.LogInfo($"Invalid IMEI: {staffMemberCreateDto}");
                    throw new skolenza.application.Exceptions.ValidationErrorException(
                        skolenza.application.Constants.AppConstants.ErrorMessages.InvalidRequest);
                }

                _logger.LogInfo("Creating new staffMember process started.");

                var command = new CreateBaseStaffMemberInfoCommand(staffMemberCreateDto);
                var result = await _mediator.Send(command);

                _logger.LogInfo("StaffMember created successfully.");
                return Ok(result);


        }
        /// <summary>
        /// Used-In-Angular: updates profile images.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates profile image.</para>
        /// <para>Handler flow: UpdateProfileImageCommand is processed by UpdateIdentityInfoCommandHandler; operation(s): DeleteFileAsync, UpdateProfileImage.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): StaffMemberBasicAPI.updateProfileImages (app/core/services/staffMember-basic-api.ts:180).</para>
        /// <para>Angular purpose: updates profile images.</para>
        /// <para>Integrated UI page(s): /app/profile</para>
        /// <para>Angular UI component(s): ProfileSummary (app/shared/components/profile/profile-summary/profile-summary.ts); StaffMemberProfile (app/features/user-menu/staffMember-profile/staffMember-profile.ts)</para>
        /// </remarks>


        [Authorize]
        [HttpPost("profile/pic/update")]
        public async Task<IActionResult> UpdateProfieImage([FromForm] UpdateStaffMemberImageRequestDTO requestDto)
        {

                _logger.LogInfo("Update image.");

                var command = new UpdateProfileImageCommand(requestDto);
                var result = await _mediator.Send(command);
                return Ok(result);


        }

        /// <summary>
        /// Used-In-Angular: retrieves profile images.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves staffMember image.</para>
        /// <para>Handler flow: GetStaffMemberImageQuery is processed by GetStaffMemberImageQueryHandler; operation(s): GetImage, GetFileUrl.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetStaffMemberImageReponseDTO: StaffMemberId (string?), Id (long), FileName (string?), FilePath (string?), IsActive (bool), IsPrimary (bool), HasImageUploaded (bool), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): StaffMemberBasicAPI.getProfileImages (app/core/services/staffMember-basic-api.ts:174).</para>
        /// <para>Angular purpose: retrieves profile images.</para>
        /// <para>Integrated UI page(s): /app/profile</para>
        /// <para>Angular UI component(s): ProfileSummary (app/shared/components/profile/profile-summary/profile-summary.ts); StaffMemberProfile (app/features/user-menu/staffMember-profile/staffMember-profile.ts)</para>
        /// </remarks>
        [Authorize]
        [HttpGet("Image/get")]
        public async Task<IActionResult> GetAllStaffMemberImage([FromQuery] GetStaffMemberImageRequestDTO requestDto)
        {

                _logger.LogInfo("Fetching all staffMembers.");

                var command = new GetStaffMemberImageQuery(requestDto);
                var result = await _mediator.Send(command);
                return Ok(result);


        }

        /// <summary>
        /// Used-In-Angular: updates edit status.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates editable status.</para>
        /// <para>Handler flow: UpdateEditableStatusCommand is processed by BankEditableStatusCommandUpdateHandler; operation(s): GetByIdAsync, UpdateEditStatus.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): ProfileAccessApi.updateEditStatus (app/core/services/profile-access-api.ts:50).</para>
        /// <para>Angular purpose: updates edit status.</para>
        /// <para>Integrated UI page(s): /app/profile/bank-info; /app/profile/basic-info; /app/profile/contact-info; /app/profile/dependent-info; /app/profile/education-info; /app/profile/experience-info; /app/profile/identity-info; /app/profile/insurance-info</para>
        /// <para>Angular UI component(s): ProfileHeader (app/shared/components/profile/profile-header/profile-header.ts); StaffMemberBankInfo (app/features/user-menu/staffMember-profile/staffMember-bank-info/staffMember-bank-info.ts); StaffMemberBasicInfo (app/features/user-menu/staffMember-profile/staffMember-basic-info/staffMember-basic-info.ts); StaffMemberContactInfo (app/features/user-menu/staffMember-profile/staffMember-contact-info/staffMember-contact-info.ts); StaffMemberDependentInfo (app/features/user-menu/staffMember-profile/staffMember-dependent-info/staffMember-dependent-info.ts); StaffMemberEducationInfo (app/features/user-menu/staffMember-profile/staffMember-education-info/staffMember-education-info.ts); StaffMemberExperienceInfo (app/features/user-menu/staffMember-profile/staffMember-experience-info/staffMember-experience-info.ts); StaffMemberIdentityInfo (app/features/user-menu/staffMember-profile/staffMember-identity-info/staffMember-identity-info.ts)</para>
        /// </remarks>
        [Authorize]
        [HttpPost("update-edit-status")]
        public async Task<IActionResult> UpdateSectionStatusBulk([FromBody] UpdateEditStatusRequestDTO_ dto)
        {
            if (dto == null)
                throw new skolenza.application.Exceptions.ValidationErrorException(
                    skolenza.application.Constants.AppConstants.ErrorMessages.InvalidRequest);

            var command = new UpdateEditableStatusCommand(dto);
            var result = await _mediator.Send(command);

            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: updates verification status.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates verification status.</para>
        /// <para>Handler flow: UpdateVerificationStatusCommand is processed by BankVerificationStatusUpdateCommandHandler; operation(s): GetByIdAsync, UpdateVerificationStatus.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): ProfileAccessApi.updateVerificationStatus (app/core/services/profile-access-api.ts:44).</para>
        /// <para>Angular purpose: updates verification status.</para>
        /// <para>Integrated UI page(s): /app/profile/bank-info; /app/profile/basic-info; /app/profile/contact-info; /app/profile/dependent-info; /app/profile/education-info; /app/profile/experience-info; /app/profile/identity-info; /app/profile/insurance-info</para>
        /// <para>Angular UI component(s): ProfileHeader (app/shared/components/profile/profile-header/profile-header.ts); StaffMemberBankInfo (app/features/user-menu/staffMember-profile/staffMember-bank-info/staffMember-bank-info.ts); StaffMemberBasicInfo (app/features/user-menu/staffMember-profile/staffMember-basic-info/staffMember-basic-info.ts); StaffMemberContactInfo (app/features/user-menu/staffMember-profile/staffMember-contact-info/staffMember-contact-info.ts); StaffMemberDependentInfo (app/features/user-menu/staffMember-profile/staffMember-dependent-info/staffMember-dependent-info.ts); StaffMemberEducationInfo (app/features/user-menu/staffMember-profile/staffMember-education-info/staffMember-education-info.ts); StaffMemberExperienceInfo (app/features/user-menu/staffMember-profile/staffMember-experience-info/staffMember-experience-info.ts); StaffMemberIdentityInfo (app/features/user-menu/staffMember-profile/staffMember-identity-info/staffMember-identity-info.ts)</para>
        /// </remarks>
        [HttpPost("update-verification-status")]
        public async Task<IActionResult> UpdateVerificationStatus([FromBody] UpdateVerificationStatusRequestDTO_ dto)
        {


            var command = new UpdateVerificationStatusCommand(dto);
            var result = await _mediator.Send(command);
            return Ok(result);
        }


        /// <summary>
        /// Used-In-Angular: updates bulk.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates section bulk.</para>
        /// <para>Handler flow: UpdateSectionBulkCommand is processed by UpdateSectionBulkCommandHandler; operation(s): UpdateSectionVerifyStatusAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): StaffMembersApi.updateBulk (app/core/services/staffMember-api.ts:128).</para>
        /// <para>Angular purpose: updates bulk.</para>
        /// <para>Integrated UI page(s): /app/staffMembers</para>
        /// <para>Angular UI component(s): VerificationSettings (app/shared/components/staffMember/verification-settings/verification-settings.ts); StaffMembers (app/features/staffMembers/staffMembers.ts)</para>
        /// </remarks>
        [Authorize]
        [HttpPost("update-bulk")]
        public async Task<IActionResult> UpdateSectionStatusBulk([FromBody] UpdateStaffMemberSectionStatusRequestDTO dto)
        {

            var command = new UpdateSectionBulkCommand(dto);
            var result = await _mediator.Send(command);

            return Ok(result);
        }

                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: performs the Angular function reset password.</para>
                /// <para>Handler flow: ResetPasswordCommand is processed by ResetPasswordCommandHandler; operation(s): GetByIdAsync, UpdatePassword.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: POST /api/staffMember/reset-password.</para>
                /// </remarks>
                [Authorize]
                [HttpPost("reset-password")]
                public async Task<IActionResult> ResetStaffMemberPassword(
                    [FromBody] ResetStaffMemberPasswordRequestDTO requestDto)
                {
                    if (requestDto is null)
                    {
                        throw new skolenza.application.Exceptions.ValidationErrorException(
                            skolenza.application.Constants.AppConstants.ErrorMessages.InvalidRequest);
                    }

                    _logger.LogInfo("Received authorized Institution StaffMember password-reset request.");

                    var command = new ResetPasswordCommand(requestDto);
                    var result = await _mediator.Send(command);

                    return Ok(result);
                }

        /// <summary>
        /// Used-In-Angular: retrieves all percentage.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves staffMember profile status.</para>
        /// <para>Handler flow: GetStaffMemberProfileStatusQuery is processed by GetStaffMemberProfileStatusQueryHandler; operation(s): GetStaffMemberCompletionAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); CompletionSectionDTO: SectionName (string?), CompletionPercent (double?), IsInfoVerified (bool?), IsEditAllowed (bool?), IsSectionCreate (bool)</para>
        /// <para>Angular function(s): StaffMembersApi.getAllPercentage (app/core/services/staffMember-api.ts:122).</para>
        /// <para>Angular purpose: retrieves all percentage.</para>
        /// <para>Integrated UI page(s): /app/staffMembers</para>
        /// <para>Angular UI component(s): VerificationSettings (app/shared/components/staffMember/verification-settings/verification-settings.ts); StaffMembers (app/features/staffMembers/staffMembers.ts)</para>
        /// </remarks>
        [Authorize]
        [HttpGet("get-all-percentage")]
        public async Task<IActionResult> GetAllStaffMemberPercentageAsync(
            [FromQuery] string staffMemberId,
            [FromQuery] skolenza.application.DTOs.BaseDTO.PermissionRequestDTO permissionRequest)
        {

                if (string.IsNullOrWhiteSpace(staffMemberId))
                {
                    _logger.LogInfo("Invalid StaffMemberId received.");
                    throw new skolenza.application.Exceptions.ValidationErrorException(
                        skolenza.application.Constants.AppConstants.ErrorMessages.InvalidIdentifier);
                }

                _logger.LogInfo("Fetching staffMember completion percentage...");

                var query = new GetStaffMemberProfileStatusQuery(staffMemberId, permissionRequest);
                var result = await _mediator.Send(query);

                _logger.LogInfo("StaffMember percentage fetched successfully.");

                return Ok(result);


        }


        /// <summary>
        /// Used-In-Angular: retrieves staffMember basics.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves base staffMember info.</para>
        /// <para>Handler flow: GetBaseStaffMemberInfoQuery is processed by GetBaseStaffMemberInfoQueryHandler; operation(s): GetInfo.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetBaseStaffMemberResponseDTO: Id (string?), EmployementCode (string?), LastName (string?), MiddleName (string?), FirstName (string?), GenderId (int), DesignationName (string?), DepartmentName (string?), EmergencyContactPerson (string?), EmergencyContactNumber (string?), BloodGroup (string?), MobileNumber (string?), Relation (int?), RelationType (string?), CountryId (int?), CountryCode (string?), Nationality (string?), RoleName (string?), RoleId (int?), RoleType (int?), Type (string?), DateOfBirth (DateTime?), DateOfOnBoarding (DateTime?), DateOfExit (DateTime?), DesignationId (int), GenderName (string?), StaffEmploymentTypeId (int), StaffCategoryId (short?), DepartmentId (int), OfficialEmail (string?), HasPermanent (bool?), IsActive (bool), IsEditAllowed (bool?), IsMarried (bool?), IsInfoVerified (bool?), CompletionPercentage (double)</para>
        /// <para>Angular function(s): StaffMemberBasicAPI.getStaffMemberBasics (app/core/services/staffMember-basic-api.ts:136).</para>
        /// <para>Angular purpose: retrieves staffMember basics.</para>
        /// <para>Integrated UI page(s): /app/profile/basic-info; /app/profile/identity-info; /app/staffMembers/permissions/:staffMemberId</para>
        /// <para>Angular UI component(s): StaffMembersPermissionsStore (app/features/staffMembers/staffMembers-permissions/staffMembers-permissions.store.ts); StaffMemberBasicInfo (app/features/user-menu/staffMember-profile/staffMember-basic-info/staffMember-basic-info.ts); StaffMemberIdentityInfo (app/features/user-menu/staffMember-profile/staffMember-identity-info/staffMember-identity-info.ts); StaffMembersPermissions (app/features/staffMembers/staffMembers-permissions/staffMembers-permissions.ts)</para>
        /// </remarks>
        [Authorize]
        [HttpGet("get")]
        public async Task<IActionResult> GetStaffMember([FromQuery] GetBaseStaffMemberRequestDTO requestDto)
        {

                _logger.LogInfo("Fetching all staffMembers.");

                var command = new GetBaseStaffMemberInfoQuery(requestDto);
                var result = await _mediator.Send(command);


                return Ok(result);


        }
        /// <summary>
        /// Used-In-Angular: retrieves staffMember summary.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves staffMember summary.</para>
        /// <para>Handler flow: GetStaffMemberSummaryQuery is processed by GetStaffMemberSummaryQueryHandler.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): StaffMembersApi.getStaffMemberSummary (app/core/services/staffMember-api.ts:142).</para>
        /// <para>Angular purpose: retrieves staffMember summary.</para>
        /// <para>Integrated UI page(s): /app/staffMembers</para>
        /// <para>Angular UI component(s): AvatarPopup (app/features/staffMembers/avatar-popup/avatar-popup.ts); StaffMembers (app/features/staffMembers/staffMembers.ts)</para>
        /// </remarks>
        [Authorize]
        [HttpGet("get-summary")]
        public async Task<IActionResult> GetStaffMemberSummary([FromQuery] GetStaffMemberSummaryRequestDTO requestDto)
        {

            _logger.LogInfo("Fetching all staffMembers.");

            var command = new GetStaffMemberSummaryQuery(requestDto);
            var result = await _mediator.Send(command);

            if (!result.IsSucceeded)
            {
                _logger.LogInfo("No staffMembers summary found or request failed.");
                return BadRequest(result);
            }

            return Ok(result);


        }
        /// <summary>
        /// Used-In-Angular: retrieves profile summary.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves staffMember profile summary.</para>
        /// <para>Handler flow: GetStaffMemberProfileSummaryQuery is processed by GetStaffMemberProfileSummaryQueryHandler.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): StaffMemberBasicAPI.fetchProfileSummary (app/core/services/staffMember-basic-api.ts:158); StaffMemberBasicAPI.getProfileSummary (app/core/services/staffMember-basic-api.ts:187).</para>
        /// <para>Angular purpose: retrieves profile summary.</para>
        /// <para>Integrated UI page(s): /app/profile/basic-info; /app/profile</para>
        /// <para>Angular UI component(s): StaffMemberBasicInfo (app/features/user-menu/staffMember-profile/staffMember-basic-info/staffMember-basic-info.ts); ProfileSummary (app/shared/components/profile/profile-summary/profile-summary.ts); StaffMemberProfile (app/features/user-menu/staffMember-profile/staffMember-profile.ts)</para>
        /// </remarks>
        [Authorize]
        [HttpGet("get-profile-summary")]
        public async Task<IActionResult> GetStaffMemberProfileSummary([FromQuery] GetStaffMemberSummaryRequestDTO requestDto)
        {

                _logger.LogInfo("Fetching all staffMembers.");
                var command = new GetStaffMemberProfileSummaryQuery(requestDto);
                var result = await _mediator.Send(command);



                return Ok(result);


        }
        /// <summary>
        /// Used-In-Angular: retrieves all staffMembers.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves all staffMember info.</para>
        /// <para>Handler flow: GetAllStaffMemberInfoQuery is processed by GetAllStaffMemberInfoQueryHandler; operation(s): GetAllInfo, including a page-batched active Institution role lookup.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetAllStaffMemberInfoResponseDTO: StaffMemberId (string?), EmployementCode (string?), LastName (string?), MiddleName (string?), FirstName (string?), GenderId (int), GenderName (string?), CountryId (int), CountryCode (string?), MobileNumber (string?), Nationality (string), DateOfBirth (string?), DateOfOnBoarding (string?), DepartmentId (int), DepartmentName (string?), DesignationId (int), DesignationName (string?), StaffEmploymentTypeId (int), StaffCategoryId (short?), StaffEmploymentTypeName (string?), OfficialEmail (string?), EmergencyContactPerson (string?), IsActive (bool), StaffMemberImagePath (string?), CompletionPercentage (double?), HasImagePicUploaded (bool?), MaxRoleAssigned (int), AssignedRoles (List&lt;AssignedStaffMemberRoleDTO&gt;); AssignedStaffMemberRoleDTO: Id (int), Code (string), Name (string), Type (string: SYSTEM/CUSTOM)</para>
        /// <para>Angular function(s): StaffMembersApi.getAllStaffMembers (app/core/services/staffMember-api.ts:102).</para>
        /// <para>Angular purpose: retrieves all staffMembers.</para>
        /// <para>Integrated UI page(s): /app/assets/list; /app/staffMembers</para>
        /// <para>Angular UI component(s): AssignAssetPopup (app/features/assets-management/assign-asset-popup/assign-asset-popup.ts); StaffMembersStore (app/features/staffMembers/staffMembers.store.ts); LeaveBalanceManageDialog (app/features/leaves/leave-requests/leave-balances/leave-balance-manage-dialog/leave-balance-manage-dialog.ts); TicketManageDialog (app/features/tickets/ticket-lists/ticket-manage-dialog/ticket-manage-dialog.ts); AssetsManagement (app/features/assets-management/assets-management.ts); StaffMembers (app/features/staffMembers/staffMembers.ts)</para>
        /// </remarks>
        [Authorize]
        [HttpGet("get-all")]
        public async Task<IActionResult> GetAllStaffMember([FromQuery] GetAllStaffMemberInfoRequestDTO requestDto)
        {

                _logger.LogInfo("Fetching all staffMembers.");

                var command = new GetAllStaffMemberInfoQuery(requestDto);
                var result = await _mediator.Send(command);
                return Ok(result);


        }

        /// <summary>
        /// Used-In-Angular: deletes staffMember.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: deletes staffMember.</para>
        /// <para>Handler flow: DeleteStaffMemberQuery is processed by DeleteBaseStaffMemberInfoQueryHandler; operation(s): GetByIdAsync, DeleteAllAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): StaffMembersApi.deleteStaffMember (app/core/services/staffMember-api.ts:115).</para>
        /// <para>Angular purpose: deletes staffMember.</para>
        /// <para>Integrated UI page(s): /app/staffMembers</para>
        /// <para>Angular UI component(s): StaffMembersStore (app/features/staffMembers/staffMembers.store.ts); StaffMembers (app/features/staffMembers/staffMembers.ts)</para>
        /// </remarks>
        [Authorize]
        [HttpDelete("delete-all")]
        public async Task<IActionResult> Delete([FromQuery] DeleteBaseStaffMemberRequestDTO dto)
        {

                _logger.LogInfo($"Deleting staffMember with Id: {dto.StaffMemberId}");
                var command = new DeleteStaffMemberQuery(dto);
                var result = await _mediator.Send(command);

                _logger.LogInfo("StaffMember deleted successfully.");
                return Ok(result);

        }
        /// <summary>
        /// Used-In-Angular: updates staffMember update status.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: performs the Angular function activate all staffMember.</para>
        /// <para>Handler flow: ActivateAllStaffMemberQuery is processed by ActivateStaffMemberInfoQueryHandler; operation(s): GetByIdAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): StaffMembersApi.updateStaffMemberUpdateStatus (app/core/services/staffMember-api.ts:135).</para>
        /// <para>Angular purpose: updates staffMember update status.</para>
        /// <para>Integrated UI page(s): /app/staffMembers</para>
        /// <para>Angular UI component(s): StaffMembersStore (app/features/staffMembers/staffMembers.store.ts); StaffMembers (app/features/staffMembers/staffMembers.ts)</para>
        /// </remarks>
        [Authorize]
        [HttpPut("update-status")]
        public async Task<IActionResult> UpdateStaffMemberStatus(
            [FromQuery] ActivateAllStaffMemberRequestDTO dto)

            {
            _logger.LogInfo(
                    $"Updating staffMember active status. StaffMemberId: {dto.StaffMemberId}, IsActive: {dto.IsActive}");

                var command = new ActivateAllStaffMemberQuery(dto);
                var result = await _mediator.Send(command);
            _logger.LogInfo(
                    $"StaffMember {(dto.IsActive ? "activated" : "deactivated")} successfully.");

                return Ok(result);


        }

        /// <summary>
        // Updates staffMember details.
        // </summary>
        /// <summary>
        /// Used-In-Angular: updates staffMember; updates staffMember basics.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates staffMember.</para>
        /// <para>Handler flow: UpdateStaffMemberCommand is processed by UpdateBaseStaffMemberCommandHandler; operation(s): GetByIdAsync, UpdateStaffMemberAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): StaffMembersApi.updateStaffMember (app/core/services/staffMember-api.ts:108); StaffMemberBasicAPI.updateStaffMemberBasics (app/core/services/staffMember-basic-api.ts:142).</para>
        /// <para>Angular purpose: updates staffMember; updates staffMember basics.</para>
        /// <para>Integrated UI page(s): /app/staffMembers; /app/profile/basic-info</para>
        /// <para>Angular UI component(s): StaffMemberManageDialog (app/shared/components/staffMember/staffMember-manage-dialog/staffMember-manage-dialog.ts); StaffMembers (app/features/staffMembers/staffMembers.ts); StaffMemberBasicInfo (app/features/user-menu/staffMember-profile/staffMember-basic-info/staffMember-basic-info.ts)</para>
        /// </remarks>
        [Authorize]
        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody] UpdateStaffMemberRequestDTO dto)
        {

                _logger.LogInfo($"Updating staffMember record. StaffMemberId: {dto.StaffMemberId}");

                var command = new UpdateStaffMemberCommand(dto);
                var result = await _mediator.Send(command);

                _logger.LogInfo("StaffMember updated successfully.");
                return Ok(result);


        }
        /// <summary>
        // Updates staffMember details.
        // </summary>
        /// <summary>
        /// Used-In-Angular: updates staffMember official basics.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates base staffMember by admin.</para>
        /// <para>Handler flow: UpdateBaseStaffMemberByAdminCommand is processed by UpdateBaseStaffMemberByAdminCommandHandler; operation(s): GetByIdAsync, UpdateStaffMemberAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): StaffMemberBasicAPI.updateStaffMemberOfficialBasics (app/core/services/staffMember-basic-api.ts:150).</para>
        /// <para>Angular purpose: updates staffMember official basics.</para>
        /// <para>Integrated UI page(s): /app/staffMembers; /app/profile/basic-info</para>
        /// <para>Angular UI component(s): StaffMemberManageDialog (app/shared/components/staffMember/staffMember-manage-dialog/staffMember-manage-dialog.ts); StaffMembers (app/features/staffMembers/staffMembers.ts); StaffMemberBasicInfo (app/features/user-menu/staffMember-profile/staffMember-basic-info/staffMember-basic-info.ts)</para>
        /// </remarks>
        [Authorize]
        [HttpPost("official/update")]

        public async Task<IActionResult> OfficialUpdate([FromBody] UpdateStaffMemberRequestOfficialDTO dto)
        {

                _logger.LogInfo($"Updating staffMember record. StaffMemberId: {dto.StaffMemberId}");

                var command = new UpdateBaseStaffMemberByAdminCommand(dto);
                var result = await _mediator.Send(command);

            return Ok(result);

        }
    }
}
