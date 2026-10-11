// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Get StaffMember Profile Status Query Handler requests.
// ================================================================

using skolenza.application.Common.Helpers.skolenza.application.Configuration;
using skolenza.application.Common.Helpers.EncryptionHelper;
using skolenza.application.Common.Helpers.PercentageHelper;
using skolenza.application.Constants;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Staff.CompletionPercentage;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffMemberCmd.StaffMemberBase.Handlers;

#region Query


/// <summary>
/// Represents the GetStaffMemberProfileStatusQuery application component.
/// </summary>
public class GetStaffMemberProfileStatusQuery
    : IRequest<ApiResponse<List<CompletionSectionDTO>>>
{
    public string? StaffMemberId { get; }

    /// <summary>Gets the module and operation required for institution-role authorization.</summary>
    public PermissionRequestDTO PermissionRequest { get; }

    public GetStaffMemberProfileStatusQuery(string empid, PermissionRequestDTO permissionRequest)
    {
        StaffMemberId = empid;
        PermissionRequest = permissionRequest;
    }
}

/// <summary>
/// Handles GetStaffMemberProfileStatusQuery requests.
/// </summary>
#endregion

#region Handler

public class GetStaffMemberProfileStatusQueryHandler
    : IRequestHandler<GetStaffMemberProfileStatusQuery, ApiResponse<List<CompletionSectionDTO>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetStaffMemberProfileStatusQueryHandler> _logger;
    private readonly IConfiguration _config;
    private readonly IIdEncoderService _idEncoderService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICommonRequestService _commonRequestService;
    public GetStaffMemberProfileStatusQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetStaffMemberProfileStatusQueryHandler> logger,
        IIdEncoderService idEncoderService,
        IConfiguration config,
        IHttpContextAccessor httpContextAccessor, ICommonRequestService commonRequestService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _config = config;
        _idEncoderService = idEncoderService;
        _httpContextAccessor = httpContextAccessor;
        _commonRequestService = commonRequestService;
    }

    public async Task<ApiResponse<List<CompletionSectionDTO>>> Handle(
   GetStaffMemberProfileStatusQuery request,
   CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("GetStaffMemberProfileStatus started");

            // ===============================
            // 1️⃣ VALIDATION (AUTH CENTRALIZED)
            // ===============================
            var validation =
                await _commonRequestService.ValidateInstitutionUserRequestAsync();

            if (!validation.Success)
                throw new UnauthorizedAccessException(validation.ErrorMessage);

            // ===============================
            // 2️⃣ NULL SAFETY
            // ===============================
            if (string.IsNullOrWhiteSpace(request.StaffMemberId))
                throw new ValidationErrorException("Invalid StaffMemberId.");

            // ===============================
            // 3️⃣ DECODE STAFF_MEMBER ID
            // ===============================
            var staffMemberId =
                _idEncoderService.DecodeId_long(
                    request.StaffMemberId,
                    validation.Claims.InstitutionEncriptionKey);

            if (staffMemberId <= 0)
                throw new ValidationErrorException("Invalid StaffMemberId.");

            if (!await _commonRequestService.CanAccessStaffMemberDataAsync(
                    validation,
                    staffMemberId,
                    StaffMemberDataAccessRequirement.PersonalDetails,
                    cancellationToken))
                throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
            // 5️⃣ FETCH DATA
            // ===============================
            var sections =
                await _unitOfWork.StaffMembers
                    .GetStaffMemberCompletionAsync(staffMemberId);

            var result = sections ?? new List<CompletionSectionDTO>();

            _logger.LogInformation("GetStaffMemberProfileStatus success");

            // ===============================
            // 6️⃣ SUCCESS
            // ===============================
            var response = ApiResponse<List<CompletionSectionDTO>>
                .Success(
                    result,
                    result.Any()
                        ? "StaffMember profile completion retrieved."
                        : "No profile completion data found."
                );

            response.CompletionPercentage =
                StaffMemberProfileCompletionCalculator.CalculateOverallCompletion(result);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error fetching profile completion | StaffMemberId: {StaffMemberId}",
                request.StaffMemberId);

            throw; //  MUST
        }
    }
}

#endregion
