// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Get Experience Info Query Handler requests.
// ================================================================

using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.DTOS.Staff.Experience;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffMemberCmd.ExperienceInfo.Handlers;

#region Query


/// <summary>
/// Represents the GetExperienceInfoQuery application component.
/// </summary>
public class GetExperienceInfoQuery
    : IRequest<ApiResponse<List<GetStaffMemberExperienceResponseDTO>>>
{
    public GetExperienceRequestDTO DTO { get; set; }

    public GetExperienceInfoQuery(GetExperienceRequestDTO dto)
    {
        DTO = dto;
    }
}

/// <summary>
/// Handles GetExperienceInfoQuery requests.
/// </summary>
#endregion

#region Handler

public class GetExperienceInfoQueryHandler
    : IRequestHandler<GetExperienceInfoQuery, ApiResponse<List<GetStaffMemberExperienceResponseDTO>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetExperienceInfoQuery> _logger;
    private readonly IIdEncoderService _idEncoderService;
    private readonly ICommonRequestService _commonRequestService;

    public GetExperienceInfoQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetExperienceInfoQuery> logger,
        IIdEncoderService idEncoderService,
        ICommonRequestService commonRequestService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _idEncoderService = idEncoderService;
        _commonRequestService = commonRequestService;
    }

    public async Task<ApiResponse<List<GetStaffMemberExperienceResponseDTO>>> Handle(
       GetExperienceInfoQuery request,
       CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("🚀 START GetExperience Handler");

            // ===============================
            // 1️⃣ VALIDATION
            // ===============================
            var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();

            if (!validation.Success)
            {
                _logger.LogWarning("❌ Validation failed");
                throw new UnauthorizedAccessException(validation.ErrorMessage);
            }

            if (request?.DTO == null)
            {
                _logger.LogWarning("❌ Request DTO is null");
                throw new ValidationErrorException("Invalid request");
            }

            if (string.IsNullOrWhiteSpace(request.DTO.StaffMemberId))
            {
                _logger.LogWarning("❌ StaffMemberId is missing");
                throw new ValidationErrorException("StaffMemberId is required");
            }

            _logger.LogInformation("✅ Validation Passed | UserId: {UserId}", validation.UserStaffMemberId);

            // ===============================
            // 2️⃣ DECODE
            // ===============================
            _logger.LogInformation("🔓 Decoding StaffMemberId: {EncodedId}", request.DTO.StaffMemberId);

            var staffMemberId = RequestCommonHelper.DecodeOnlyStaffMemberId(
                request.DTO.StaffMemberId,
                validation.Claims.InstitutionEncriptionKey,
                _idEncoderService);

            _logger.LogInformation("Decoded StaffMemberId: {DecodedId}", staffMemberId);

            if (staffMemberId <= 0)
            {
                _logger.LogError("❌ Invalid decoded StaffMemberId");
                throw new ValidationErrorException("Invalid StaffMemberId.");
            }

            // ===============================
            // 3️⃣ FETCH
            // ===============================
            _logger.LogInformation("📥 Fetching experience data...");

            var expEntities =
                await _unitOfWork.StaffMemberExperienceRepository
                    .GetByStaffMemberIdWithDocumentsAsync(staffMemberId, request.DTO);

            _logger.LogInformation("📦 Data fetched | Count: {Count}",
                expEntities?.Data?.Count ?? 0);

            // ===============================
            // 4️⃣ EMPTY LIST = SUCCESS
            // ===============================
            if (expEntities == null || expEntities.Data == null || !expEntities.Data.Any())
            {
                _logger.LogWarning("⚠️ No experience records found");

                return ApiResponse<List<GetStaffMemberExperienceResponseDTO>>
                    .SuccessPaginatedPercentage(
                        Data: new List<GetStaffMemberExperienceResponseDTO>(),
                        Message: "No experience info found.",
                        PageNumber: expEntities?.PageNumber ?? 1,
                        PageSize: expEntities?.PageSize ?? 0,
                        TotalRecords: expEntities?.TotalCount ?? 0,
                        TotalPages: expEntities?.TotalPages ?? 0,
                        HasUploadedAll: expEntities?.HasUploadedAll ?? false,
                        CompletionPercentage: expEntities?.CompletionPercentage ?? 0
                    );
            }

            // ===============================
            // 5️⃣ ENCODE IDS (FIXED )
            // ===============================
            foreach (var item in expEntities.Data)
            {
                if (!string.IsNullOrWhiteSpace(item.StaffMemberId) &&
                    long.TryParse(item.StaffMemberId, out var empId))
                {
                    item.StaffMemberId = _idEncoderService.EncodeId_long(
                        empId,
                        validation.Claims.InstitutionEncriptionKey);
                }
                else
                {
                    _logger.LogWarning("⚠️ Skipping encoding | Invalid StaffMemberId in item");
                }
            }

            _logger.LogInformation("🔐 StaffMemberId encoding completed");

            // ===============================
            // 6️⃣ SUCCESS RESPONSE
            // ===============================
            _logger.LogInformation("✅ END GetExperience SUCCESS");

            return ApiResponse<List<GetStaffMemberExperienceResponseDTO>>
                .SuccessPaginatedPercentage(
                    Data: expEntities.Data,
                    Message: "Experience info retrieved successfully.",
                    PageNumber: expEntities.PageNumber,
                    PageSize: expEntities.PageSize,
                    TotalRecords: expEntities.TotalCount,
                    TotalPages: expEntities.TotalPages,
                    HasUploadedAll: expEntities.HasUploadedAll,
                    CompletionPercentage: expEntities.CompletionPercentage
                );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ ERROR GetExperience Handler");
            throw;
        }
    }
}






#endregion
