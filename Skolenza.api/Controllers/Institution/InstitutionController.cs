// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Exposes Institution creation, configuration, verification, and Host-side management endpoints.
// ================================================================

using skolenza.application.DTOs.Institution;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOs.Verify;
using skolenza.application.DTOS.Host;
using skolenza.application.DTOS.Institution;
using skolenza.application.Features.HostCmd.Handler;
using skolenza.application.Features.RegistrationCmd.Handlers;
using skolenza.application.Features.InstitutionConfigurationCmd.Configuration.StaffCodeCmd.Handlers;
using skolenza.application.Features.InstitutionConfigurationCmd.Institution.Commands;
using skolenza.application.Features.InstitutionConfigurationCmd.Institution.Handlers;
using skolenza.application.Features.InstitutionConfigurationCmd.Institution.Queries;
using skolenza.application.Features.InstitutionManagementCmd.Commands;
using skolenza.application.Features.InstitutionManagementCmd.Queries;
using skolenza.application.Features.VerifyEmailCmd.Handlers;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Institution;

/// <summary>
/// Provides API endpoints for Institution registration, configuration, verification, and Host-side management requests.
/// </summary>
[Route("api/Institution")]
[ApiController]
public class InstitutionController : ControllerBase
{
    #region Staff Member Code Pattern Changes

    /// <summary>Previews or confirms insertion of the authenticated institution's staff member code pattern.</summary>
    /// <remarks>
    /// Not-Used-In-Angular. Requires INSTITUTION_STAFF_CODE Add permission. Send Confirm=false
    /// for a read-only preview, then the same Pattern with Confirm=true and returned PreviewHash.
    /// Response includes CanCommit, staff member current/proposed codes, row errors and Applied.
    /// An existing active pattern returns conflict; use update instead. Institution comes from authentication.
    /// </remarks>
    [Authorize]
    [HttpPost("add-staff-member-code-pattern")]
    public async Task<IActionResult> AddStaffMemberCodePattern(
        [FromBody] SaveStaffMemberCodePatternRequestDTO request, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new CreateStaffMemberCodePatternCommand(request), cancellationToken));
    }

    /// <summary>Previews or confirms an institution pattern change and recodes existing staff members, including the Admin.</summary>
    /// <remarks>
    /// Not-Used-In-Angular. Requires INSTITUTION_STAFF_CODE Update permission. Preview uses existing
    /// DateOfOnBoarding values as joining dates and preserves recognized running numbers.
    /// Confirm=false never writes. Confirm=true requires the returned PreviewHash; changed data
    /// requires a fresh preview (409). Invalid dates/codes/collisions block the entire operation.
    /// Pattern and staff member codes commit atomically. Login, role and staff member identity are preserved.
    /// </remarks>
    [Authorize]
    [HttpPut("update-staff-member-code-pattern")]
    public async Task<IActionResult> UpdateStaffMemberCodePattern(
        [FromBody] SaveStaffMemberCodePatternRequestDTO request, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new UpdateStaffMemberCodePatternCommand(request), cancellationToken));
    }

    #endregion

    #region Fields

    private readonly IMediator _mediator;
    private readonly ILoggerService _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="InstitutionController"/> class.
    /// </summary>
    /// <param name="mediator">The mediator used to dispatch application requests.</param>
    /// <param name="logger">The application logger.</param>
    public InstitutionController(IMediator mediator, ILoggerService logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    #endregion

    #region Institution Self-Registration

    /// <summary>
    /// Used-In-Angular: creates institution.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: creates institution.</para>
    /// <para>Handler flow: CreateInstitutionCommand creates the Institution and a minimum valid initial Campus in the same transaction, then completes subscription, security, role and administrator setup.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?); InstitutionCreateResponseDTO: Success (bool), EmailSent (bool?), Message (string)</para>
    /// <para>Angular function(s): InstitutionsApi.registerInstitution (app/core/services/institutions-api.ts).</para>
    /// <para>Angular purpose: creates institution.</para>
    /// <para>Integrated UI page(s): /auth/register-institution</para>
    /// <para>Angular UI component(s): Registration (app/features/authentication/registration/registration.ts)</para>
    /// <para>Failure contract: IsSucceeded=false returns a safe, actionable Message and a stable ErrorCode. Validation, duplicate, missing-plan, email-configuration, cancellation, and stage-specific setup failures do not expose raw exception details.</para>
    /// </remarks>
    [AllowAnonymous]
    [HttpPost("create-institution")]
    [ProducesResponseType(
        typeof(ApiResponse<application.DTOs.Registration.InstitutionCreateResponseDTO>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> InstitutionCreation(
        [FromBody] application.DTOs.Registration.InstitutionCreateRequestDTO requestDTO)
    {
        _logger.LogInfo("Received request to register a new institution.");
        var command = new CreateInstitutionCommand(requestDTO.ToCreationRequest());
        var result = await _mediator.Send(command);

        return Ok(result);
    }

    #endregion

    #region Institution Management Queries
    /// <summary>
    /// Used-In-Angular: updates institution by host.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: updates new institution.</para>
    /// <para>Handler flow: UpdateNewInstitutionCommand updates the Institution aggregate and the UI-selected Campus in the same transaction; a Institution country change is inherited when the selected location omits CountryId.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); HostInstitutionResponseDTO: Id (string), CompanyName (string), InstitutionCode (string?), CompanyEmailDomain (string), InstitutionEmail (string), ContactPersonName (string?), ContactNumber (string?), CountryId (int), IsVerified (bool), IsActive (bool)</para>
    /// <para>Angular function(s): InstitutionsApi.updateInstitutionByHost (app/core/services/institutions-api.ts:169).</para>
    /// <para>Angular purpose: updates institution by host.</para>
    /// <para>Integrated UI page(s): /app/institutions/new; /app/institutions/:institutionId/edit</para>
    /// <para>Angular UI component(s): InstitutionForm (app/features/host/institutions/institution-form/institution-form.ts)</para>
    /// </remarks>

    [Authorize]
    [HttpPut("new-institution-update-by-host/{encryptedInstitutionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateNewInstitutionAsync(
        string encryptedInstitutionId,
        [FromBody] NewInstitutionUpdateRequestDTO? requestDTO,
        [FromQuery] PermissionRequestDTO? permissionRequest,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateNewInstitutionCommand(encryptedInstitutionId, requestDTO, permissionRequest),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Used-In-Angular: performs the Angular function sync active plan entitlements.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: performs the Angular function synchronize institution plan entitlements.</para>
    /// <para>Handler flow: SynchronizeInstitutionPlanEntitlementsCommand is processed by SynchronizeInstitutionPlanEntitlementsCommandHandler; operation(s): GetHostManagedInstitutionByIdAsync, SaveChangesAsync.</para>
    /// <para>Response DTO property analysis: SynchronizeInstitutionPlanEntitlementsRequestDTO: InstitutionId (string); ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); SynchronizeInstitutionPlanEntitlementsResponseDTO: InstitutionId (string), SubscriptionPlanId (int), SourceModuleCount (int), AddedModuleCount (int), ExistingModuleCount (int), SourceOperationCount (int), AddedOperationCount (int), ExistingOperationCount (int), Modules (List&lt;InstitutionPlanEntitlementModuleSyncResponseDTO&gt;), Operations (List&lt;InstitutionPlanEntitlementOperationSyncResponseDTO&gt;)</para>
    /// <para>Angular function(s): InstitutionsApi.syncActivePlanEntitlements (app/core/services/institutions-api.ts:216).</para>
    /// <para>Angular purpose: performs the Angular function sync active plan entitlements.</para>
    /// <para>Integrated UI page(s): /app/institutions</para>
    /// <para>Angular UI component(s): InstitutionsStore (app/features/host/institutions/institutions.store.ts); Institutions (app/features/host/institutions/institutions.ts)</para>
    /// </remarks>
    [Authorize]
    [HttpPost("sync-active-plan-entitlements")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SynchronizeInstitutionPlanEntitlementsAsync(
        [FromBody] SynchronizeInstitutionPlanEntitlementsRequestDTO? requestDTO,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SynchronizeInstitutionPlanEntitlementsCommand(requestDTO),
            cancellationToken);
        return Ok(result);
    }
        /// <summary>
        /// Not-Used-In-Angular.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Not-Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves institution delete dependency info.</para>
        /// <para>Handler flow: GetInstitutionDeleteDependencyInfoQuery is processed by GetInstitutionDeleteDependencyInfoQueryHandler; operation(s): GetHostManagedInstitutionByIdAsync.</para>
        /// <para>Response DTO property analysis: PermissionRequestDTO: ModuleId (int), OperationId (int); ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); InstitutionDeleteDependencyInfoResponseDTO: InstitutionId (string), Message (string), TransactionalDataGroups (List&lt;string&gt;)</para>
        /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
        /// <para>Backend endpoint: GET /api/institution/{}/delete-dependencies.</para>
        /// </remarks>

        [Authorize]
        [HttpGet("{id}/delete-dependencies")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInstitutionDeleteDependencyInfoAsync(
            string id,
            [FromQuery] PermissionRequestDTO permissionRequest,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetInstitutionDeleteDependencyInfoQuery(id, permissionRequest),
                cancellationToken);
            return Ok(result);
        }
    /// <summary>
    /// Used-In-Angular: creates institution by host.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: creates new institution.</para>
    /// <para>Handler flow: CreateNewInstitutionCommand bridges to the transactional creation handler, which persists the supplied full InitialLocation with the Institution.</para>
    /// <para>Response DTO property analysis: NewInstitutionCreationRequestDTO contains institution, profile, location, and staffMember-code pattern data. SMTP configuration is intentionally excluded; the welcome email uses the active Host default configuration. ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); InstitutionCreateResponseDTO: Success (bool), EmailSent (bool?), Message (string)</para>
    /// <para>Angular function(s): InstitutionsApi.createInstitutionByHost (app/core/services/institutions-api.ts:117).</para>
    /// <para>Angular purpose: creates institution by host.</para>
    /// <para>Integrated UI page(s): /app/institutions/new; /app/institutions/:institutionId/edit</para>
    /// <para>Angular UI component(s): InstitutionForm (app/features/host/institutions/institution-form/institution-form.ts)</para>
    /// </remarks>

    [Authorize]
    [HttpPost("new-tentant-creation-by-host")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateNewInstitutionAsync(
        [FromBody] NewInstitutionCreationRequestDTO requestDTO,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateNewInstitutionCommand(requestDTO), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Used-In-Angular: validates email.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves all institutions.</para>
    /// <para>Handler flow: GetAllInstitutionsQuery is processed by GetAllInstitutionsQueryHandler; operation(s): GetHostManagedInstitutionsAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); HostInstitutionResponseDTO: Id (string), CompanyName (string), InstitutionCode (string?), CompanyEmailDomain (string), InstitutionEmail (string), ContactPersonName (string?), ContactNumber (string?), CountryId (int), IsVerified (bool), IsActive (bool)</para>
    /// <para>Angular function(s): InstitutionsApi.verifyEmail (app/core/services/institutions-api.ts:131).</para>
    /// <para>Angular purpose: validates email.</para>
    /// <para>Integrated UI page(s): /auth/registration-verify</para>
    /// <para>Angular UI component(s): RegistrationVerify (app/features/authentication/registration/registration-verify/registration-verify.ts)</para>
    /// </remarks>
    [Authorize]
    [HttpGet("get-all-institutions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllInstitutionsAsync([FromQuery] GetAllInstitutionsRequestDTO requestDTO)
    {
        var result = await _mediator.Send(new GetAllInstitutionsQuery(requestDTO));
        return Ok(result);
    }

    /// <summary>
    /// Used-In-Angular: retrieves institution by id.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves institution by id.</para>
    /// <para>Handler flow: GetInstitutionByIdQuery is processed by GetInstitutionByIdQueryHandler; operation(s): GetHostManagedInstitutionDetailAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); HostInstitutionDetailResponseDTO: Id (string), InstitutionTypeId (int), InstitutionTypeName (string?), CompanyName (string), InstitutionCode (string?), CompanyEmailDomain (string), InstitutionEmail (string), ContactPersonName (string?), GenderId (int?), ContactNumber (string?), CountryId (int), DefaultCurrency (int?), IsVerified (bool), IsActive (bool), Profiles (List&lt;HostInstitutionProfileDetailDTO&gt;), Campuses (List&lt;HostCampusDetailDTO&gt;), Subscriptions (List&lt;HostInstitutionSubscriptionDetailDTO&gt;), Departments (List&lt;HostInstitutionDepartmentDetailDTO&gt;), StaffMemberCodePatterns (List&lt;HostInstitutionStaffMemberCodePatternDetailDTO&gt;), StaffMembers (List&lt;HostInstitutionStaffMemberDetailDTO&gt;), LoginCredentials (List&lt;HostInstitutionLoginCredentialDetailDTO&gt;), EmailConfigurations (List&lt;HostInstitutionEmailConfigurationDetailDTO&gt;)</para>
    /// <para>Angular function(s): InstitutionsApi.getInstitutionById (app/core/services/institutions-api.ts:139).</para>
    /// <para>Angular purpose: retrieves institution by id.</para>
    /// <para>Integrated UI page(s): /app/institutions/new; /app/institutions/:institutionId/edit; /app/institutions</para>
    /// <para>Angular UI component(s): InstitutionDetail (app/features/host/institutions/institution-detail/institution-detail.ts); InstitutionForm (app/features/host/institutions/institution-form/institution-form.ts); Institutions (app/features/host/institutions/institutions.ts)</para>
    /// </remarks>
    [Authorize]
    [HttpGet("get-institution-by-id")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInstitutionByIdAsync([FromQuery] GetInstitutionByIdRequestDTO requestDTO)
    {
        var result = await _mediator.Send(new GetInstitutionByIdQuery(requestDTO));
        return Ok(result);
    }

    #endregion

    #region Institution Management Commands
        /// <summary>
        /// Not-Used-In-Angular.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Not-Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates host managed institution.</para>
        /// <para>Handler flow: UpdateHostManagedInstitutionCommand updates the Institution and synchronizes its canonical initial Campus country and generated company-based name in the same SaveChanges transaction.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); HostInstitutionResponseDTO: Id (string), CompanyName (string), InstitutionCode (string?), CompanyEmailDomain (string), InstitutionEmail (string), ContactPersonName (string?), ContactNumber (string?), CountryId (int), IsVerified (bool), IsActive (bool)</para>
        /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
        /// <para>Backend endpoint: PUT /api/institution/{}.</para>
        /// </remarks>

        [Authorize]
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateHostManagedInstitutionAsync(
            string id,
            [FromBody] UpdateHostManagedInstitutionRequestDTO? requestDTO,
            [FromQuery] PermissionRequestDTO? permissionRequest,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new UpdateHostManagedInstitutionCommand(id, requestDTO, permissionRequest),
                cancellationToken);

            return Ok(result);
        }
        /// <summary>
        /// Not-Used-In-Angular.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Not-Used-In-Angular.</para>
        /// <para>API endpoint purpose: deletes host managed institution.</para>
        /// <para>Handler flow: DeleteHostManagedInstitutionCommand is processed by DeleteHostManagedInstitutionCommandHandler; operation(s): GetHostManagedInstitutionByIdAsync, SaveChangesAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
        /// <para>Backend endpoint: DELETE /api/institution/{}.</para>
        /// </remarks>

        [Authorize]
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteHostManagedInstitutionAsync(
            string id,
            [FromQuery] PermissionRequestDTO? permissionRequest,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new DeleteHostManagedInstitutionCommand(id, permissionRequest),
                cancellationToken);

            return Ok(result);
        }
    /// <summary>
    /// Used-In-Angular: performs the Angular function resend verification email.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: performs the Angular function resend institution verification.</para>
    /// <para>Handler flow: ResendInstitutionVerificationCommand is processed by ResendInstitutionVerificationCommandHandler; operation(s): GetHostManagedInstitutionByIdAsync, AddMinutes.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
    /// <para>Angular function(s): InstitutionsApi.resendVerificationEmail (app/core/services/institutions-api.ts:208).</para>
    /// <para>Angular purpose: performs the Angular function resend verification email.</para>
    /// <para>Integrated UI page(s): /app/institutions</para>
    /// <para>Angular UI component(s): InstitutionsStore (app/features/host/institutions/institutions.store.ts); Institutions (app/features/host/institutions/institutions.ts)</para>
    /// </remarks>

    [Authorize]
    [HttpPost("{id}/resend-verification-by-host")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResendInstitutionVerificationAsync(
        string id,
        [FromQuery] PermissionRequestDTO? permissionRequest,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ResendInstitutionVerificationCommand(id, permissionRequest),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Used-In-Angular: updates institution.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: updates host managed institution.</para>
    /// <para>Handler flow: UpdateHostManagedInstitutionCommand updates the Institution and synchronizes its canonical initial Campus country and generated company-based name in the same SaveChanges transaction.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); HostInstitutionResponseDTO: Id (string), CompanyName (string), InstitutionCode (string?), CompanyEmailDomain (string), InstitutionEmail (string), ContactPersonName (string?), ContactNumber (string?), CountryId (int), IsVerified (bool), IsActive (bool)</para>
    /// <para>Angular function(s): InstitutionsApi.updateInstitution (app/core/services/institutions-api.ts:177).</para>
    /// <para>Angular purpose: updates institution.</para>
    /// <para>Integrated UI page(s): No static Angular route was resolved; see Angular UI component(s).</para>
    /// <para>Angular UI component(s): No consuming Angular component was statically resolved.</para>
    /// </remarks>
    [Authorize]
    [HttpPost("update-institution")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateInstitutionAsync([FromBody] UpdateInstitutionRequestDTO requestDTO)
    {
        var result = await _mediator.Send(
            new UpdateHostManagedInstitutionCommand(requestDTO.InstitutionId, requestDTO, requestDTO));
        return Ok(result);
    }

    /// <summary>
    /// Used-In-Angular: performs the Angular function activate institution.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: performs the Angular function activate institution.</para>
    /// <para>Handler flow: ActivateInstitutionCommand is processed by ActivateInstitutionCommandHandler; operation(s): GetHostManagedInstitutionByIdAsync, SaveChangesAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); HostInstitutionResponseDTO: Id (string), CompanyName (string), InstitutionCode (string?), CompanyEmailDomain (string), InstitutionEmail (string), ContactPersonName (string?), ContactNumber (string?), CountryId (int), IsVerified (bool), IsActive (bool)</para>
    /// <para>Angular function(s): InstitutionsApi.activateInstitution (app/core/services/institutions-api.ts:189).</para>
    /// <para>Angular purpose: performs the Angular function activate institution.</para>
    /// <para>Integrated UI page(s): /app/institutions/new; /app/institutions/:institutionId/edit; /app/institutions</para>
    /// <para>Angular UI component(s): InstitutionForm (app/features/host/institutions/institution-form/institution-form.ts); InstitutionsStore (app/features/host/institutions/institutions.store.ts); Institutions (app/features/host/institutions/institutions.ts)</para>
    /// </remarks>
    [Authorize]
    [HttpPost("activate-institution")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ActivateInstitutionAsync([FromBody] ActivateInstitutionRequestDTO requestDTO)
    {
        var result = await _mediator.Send(new ActivateInstitutionCommand(requestDTO));
        return Ok(result);
    }

    /// <summary>
    /// Used-In-Angular: performs the Angular function deactivate institution.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: performs the Angular function deactivate institution.</para>
    /// <para>Handler flow: DeactivateInstitutionCommand is processed by DeactivateInstitutionCommandHandler; operation(s): GetHostManagedInstitutionByIdAsync, SaveChangesAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); HostInstitutionResponseDTO: Id (string), CompanyName (string), InstitutionCode (string?), CompanyEmailDomain (string), InstitutionEmail (string), ContactPersonName (string?), ContactNumber (string?), CountryId (int), IsVerified (bool), IsActive (bool)</para>
    /// <para>Angular function(s): InstitutionsApi.deactivateInstitution (app/core/services/institutions-api.ts:195).</para>
    /// <para>Angular purpose: performs the Angular function deactivate institution.</para>
    /// <para>Integrated UI page(s): /app/institutions/new; /app/institutions/:institutionId/edit; /app/institutions</para>
    /// <para>Angular UI component(s): InstitutionForm (app/features/host/institutions/institution-form/institution-form.ts); InstitutionsStore (app/features/host/institutions/institutions.store.ts); Institutions (app/features/host/institutions/institutions.ts)</para>
    /// </remarks>
    [Authorize]
    [HttpPost("deactivate-institution")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeactivateInstitutionAsync([FromBody] DeactivateInstitutionRequestDTO requestDTO)
    {
        var result = await _mediator.Send(new DeactivateInstitutionCommand(requestDTO));
        return Ok(result);
    }

    /// <summary>
    /// Used-In-Angular: deletes institution.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: deletes host managed institution.</para>
    /// <para>Handler flow: DeleteHostManagedInstitutionCommand is processed by DeleteHostManagedInstitutionCommandHandler; operation(s): GetHostManagedInstitutionByIdAsync, SaveChangesAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
    /// <para>Angular function(s): InstitutionsApi.deleteInstitution (app/core/services/institutions-api.ts:201).</para>
    /// <para>Angular purpose: deletes institution.</para>
    /// <para>Integrated UI page(s): /app/institutions</para>
    /// <para>Angular UI component(s): InstitutionsStore (app/features/host/institutions/institutions.store.ts); Institutions (app/features/host/institutions/institutions.ts)</para>
    /// </remarks>
    [Authorize]
    [HttpPost("delete-institution")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteInstitutionAsync([FromBody] DeleteInstitutionRequestDTO requestDTO)
    {
        var result = await _mediator.Send(
            new DeleteHostManagedInstitutionCommand(requestDTO.InstitutionId, requestDTO));
        return Ok(result);
    }

    #endregion

    #region Existing Host User Command
        /// <summary>
        /// Not-Used-In-Angular.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Not-Used-In-Angular.</para>
        /// <para>API endpoint purpose: creates host user.</para>
        /// <para>Handler flow: CreateHostUserCommand is processed by CreateHostUserCommandHandler; operation(s): AddAsync, SaveChangesAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); CreateHostUserResponseDTO: Id (long), HostRoleId (long), Name (string), LoginId (string), Email (string?), MobileNumber (string?), IsActive (bool), RoleName (string?), Permissions (List&lt;HostUserPermissionResponseDTO&gt;)</para>
        /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
        /// <para>Backend endpoint: POST /api/institution/create-host-user.</para>
        /// </remarks>

        [Authorize]
        [HttpPost("create-host-user")]
        public async Task<IActionResult> CreateHostUser([FromBody] CreateHostUserRequestDTO institutionCreateRequestDTO)
        {
            _logger.LogInfo("Received request for register a new Institution" + institutionCreateRequestDTO.ToString());
            var command = new CreateHostUserCommand(institutionCreateRequestDTO);
            var result = await _mediator.Send(command);

            return Ok(result);
        }

    #endregion

    #region Existing Institution Configuration Queries
        /// <summary>
        /// Not-Used-In-Angular.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Not-Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves all institution by subscription plan id.</para>
        /// <para>Handler flow: GetAllInstitutionBySubscriptionPlanIdQuery is processed by GetAllInstitutionBySubscriptionPlanIdQueryHandler; operation(s): GetAllInstitutionBySubscriptionIdAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); InstitutionResponseDTO: Id (long), CompanyName (string), InstitutionCode (string?), CompanyEmailDomain (string), InstitutionEmail (string), ContactPersonName (string?), ContactNumber (string?), CountryId (int), IsVerified (bool), IsActive (bool)</para>
        /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
        /// <para>Backend endpoint: GET /api/institution/get-all-institution-by-subscription-plan-id.</para>
        /// </remarks>

        [Authorize]
        [HttpGet("get-all-institution-by-subscription-plan-Id")]
        public async Task<IActionResult> GetAllInstitutionBySubscriptionIdAsync([FromQuery] application.DTOs.Institution.InstitutionRequestDTO code)
        {
            _logger.LogInfo($"Getting email templates for code: {code}");

            var query = new GetAllInstitutionBySubscriptionPlanIdQuery(code);
            var result = await _mediator.Send(query);

            return Ok(result);
        }

    /// <summary>
    /// Used-In-Angular: retrieves staffMember code pattern.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves staffMember code pattern.</para>
    /// <para>Handler flow: GetStaffMemberCodePatternQuery is processed by GetStaffMemberCodePatternQueryHandler; operation(s): GetInstitutionStaffMemberCodePatternAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
    /// <para>Angular function(s): InstitutionsApi.getStaffMemberCodePattern (app/core/services/institutions-api.ts:154).</para>
    /// <para>Angular purpose: retrieves staffMember code pattern.</para>
    /// <para>Integrated UI page(s): No static Angular route was resolved; see Angular UI component(s).</para>
    /// <para>Angular UI component(s): No consuming Angular component was statically resolved.</para>
    /// </remarks>
    [Authorize]
    [HttpGet("get-staff-member-code-pattern")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStaffMemberCodePatternAsync([FromQuery] StaffMemberCodePatternRequestDTO code)
    {
        _logger.LogInfo("Fetching staffMember code pattern for institution.");

        var query = new GetStaffMemberCodePatternQuery(code);
        var result = await _mediator.Send(query);

        return Ok(result);
    }
        /// <summary>
        /// Not-Used-In-Angular.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Not-Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves institution enabled module.</para>
        /// <para>Handler flow: GetInstitutionEnabledModuleCommand is processed by GetAllInstitutionEnabledModuleOperationByInstitutionIdCommandHandler; operation(s): GetAllInstitutionEnabledModulesAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetModuleHierarchyResponseDTO: InstitutionId (long?), Modules (List&lt;ModuleNodedto&gt;)</para>
        /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
        /// <para>Backend endpoint: POST /api/institution/get.</para>
        /// </remarks>

        [Authorize]
        [HttpPost("get")]
        public async Task<IActionResult> GetAllInstitutionEnabledModuleOperationsByInstitutionIdAsync([FromBody] InstitutionEnabledModuleRequestDTO code)
        {
            _logger.LogInfo($"Getting email templates for code: {code}");

            var query = new GetInstitutionEnabledModuleCommand(code);
            var result = await _mediator.Send(query);
            return Ok(result);
        }

    /// <summary>
    /// Used-In-Angular: retrieves institution operations.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves all institution operations.</para>
    /// <para>Handler flow: GetAllInstitutionOperationsCommand is processed by GetAllInstitutionOperationsCommandHandler; operation(s): GetAllInstitutionModuleWithOperation.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); InstitutionEnabledOperationsResponseDTO: InstitutionId (long?), Modules (List&lt;EnabledModuleActiveDTO&gt;?)</para>
    /// <para>Angular function(s): InstitutionsApi.getInstitutionOperations (app/core/services/institutions-api.ts:146).</para>
    /// <para>Angular purpose: retrieves institution operations.</para>
    /// <para>Integrated UI page(s): /app/policies/attendance-policies; /auth/login; /app/admin-dashboard; /app/departments; /app/designations; /app/device-masters; /app/modules/module-operations; /app/modules/operations</para>
    /// <para>Angular UI component(s): CurrentUserPermissionsStore (app/core/stores/current-user-permissions.store.ts); StaffMembersPermissionsStore (app/features/staffMembers/staffMembers-permissions/staffMembers-permissions.store.ts); RolePermissionsStore (app/features/roles/role-permissions/role-permissions.store.ts); hasModuleOperationGuard (app/core/guards/has-module-operation-guard.ts); hasModulePermissionGuard (app/core/guards/has-module-permission-guard.ts); superAdminGuard (app/core/guards/super-admin-guard.ts); AttendancePolicies (app/features/attendance-policies/attendance-policies.ts); Login (app/features/authentication/login/login.ts)</para>
    /// </remarks>
    [Authorize]
    [HttpGet("get-all-institution-operations")]
    public async Task<IActionResult> GetAllNodeLeafeWithOperationsAsync([FromQuery] InstitutionEnabledOperationsRequestDTO code)
    {
        _logger.LogInfo($"Getting email templates for code: {code}");

        var query = new GetAllInstitutionOperationsCommand(code);
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    #endregion

    #region Existing Institution Configuration Command

    /// <summary>
    /// Used-In-Angular: updates modules and operations.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: performs the Angular function institution enabled module operations update.</para>
    /// <para>Handler flow: InstitutionEnabledModuleOperationsUpdateCommand is processed by InstitutionEnabledModuleOperationsUpdateCommandHandler.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
    /// <para>Angular function(s): InstitutionsApi.updateModulesAndOperations (app/core/services/institutions-api.ts:183).</para>
    /// <para>Angular purpose: updates modules and operations.</para>
    /// <para>Integrated UI page(s): /app/institutions/:institutionId/modules</para>
    /// <para>Angular UI component(s): InstitutionModulesStore (app/features/host/institutions/institution-modules/institution-modules.store.ts); InstitutionModules (app/features/host/institutions/institution-modules/institution-modules.ts)</para>
    /// </remarks>
    [Authorize]
    [HttpPost("update-modules-and-operations")]
    public async Task<IActionResult> InstitutionModuleOperationsUpdate([FromBody] InstitutionModuleOperationsUpdateRequestDTO code)
    {
        _logger.LogInfo($"Getting email templates for code: {code}");

        var query = new InstitutionEnabledModuleOperationsUpdateCommand(code);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    #endregion

    #region Existing Institution Verification Command

    /// <summary>
    /// Used-In-Angular: validates email.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: validates email.</para>
    /// <para>Handler flow: VerifyEmailCommand is processed by VerifyEmailCommandHandler; operation(s): GetUserInfoFromToken, UpdateInstitutionAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); VerifyEmailResponseDTO: StaffMemberId (long), InstitutionId (long?), UserId (string), Expiry (string), IsExpired (bool)</para>
    /// <para>Angular function(s): InstitutionsApi.verifyEmail (app/core/services/institutions-api.ts:124).</para>
    /// <para>Angular purpose: validates email.</para>
    /// <para>Integrated UI page(s): /auth/registration-verify</para>
    /// <para>Angular UI component(s): RegistrationVerify (app/features/authentication/registration/registration-verify/registration-verify.ts)</para>
    /// </remarks>
    [AllowAnonymous]
    [HttpPost("verify")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequestDTO request)
    {
        var command = new VerifyEmailCommand(request);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    #endregion
}
