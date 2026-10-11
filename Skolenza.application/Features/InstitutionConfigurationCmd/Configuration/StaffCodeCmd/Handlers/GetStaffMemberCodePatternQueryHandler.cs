// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves an staffMember-code pattern while preserving Institution access and protecting the Host Institution boundary.
// ================================================================

using skolenza.application.Common.Helpers;
using skolenza.application.DTOS.Institution;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.InstitutionConfigurationCmd.Configuration.StaffCodeCmd.Handlers
{
    // ======================= QUERY ============================
    public class GetStaffMemberCodePatternQuery
        : IRequest<ApiResponse<object>>
    {
        public StaffMemberCodePatternRequestDTO DTO { get; }

        public GetStaffMemberCodePatternQuery(StaffMemberCodePatternRequestDTO dto)
        {
            DTO = dto;
        }
    }

    // ======================= HANDLER ============================
    public class GetStaffMemberCodePatternQueryHandler
        : IRequestHandler<GetStaffMemberCodePatternQuery, ApiResponse<object>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IIdEncoderService _idEncoderService;
        private readonly ILogger<GetStaffMemberCodePatternQueryHandler> _logger;

        public GetStaffMemberCodePatternQueryHandler(
            IUnitOfWork unitOfWork,
            ICommonRequestService commonRequestService,
            IIdEncoderService idEncoderService,
            ILogger<GetStaffMemberCodePatternQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _commonRequestService = commonRequestService;
            _idEncoderService = idEncoderService;
            _logger = logger;
        }

        public async Task<ApiResponse<object>> Handle(GetStaffMemberCodePatternQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "GetStaffMemberCodePattern started.");

                // ===============================
                // 1️⃣ NULL SAFETY
                // ===============================
                if (request?.DTO == null)
                {
                    throw new ValidationErrorException(
                        "Invalid request data.");
                }

                // ===============================
                // 2️⃣ INSTITUTION OR HOST VALIDATION
                // ===============================
                var institutionValidation =
                    await _commonRequestService.ValidateInstitutionUserRequestAsync();

                long targetInstitutionId;
                string? hostInstitutionEncryptionKey = null;

                if (institutionValidation.Success)
                {
                    // Institution users can access only their own institution.
                    targetInstitutionId = institutionValidation.InstitutionId;

                    // ===============================
                    // 2️⃣ PERMISSION CHECK (RBAC)
                    // ===============================
                    //var hasAccess = await _permissionService.HasAccessAsync(
                    //    validation.RoleId,
                    //    Modules.StaffMember,
                    //    Operations.View);

                    //if (!hasAccess)
                    //    throw new UnauthorizedAccessException("Access denied.");
                }
                else
                {
                    var hostContext = await HostRuntimePermissionValidator.ValidateAsync(
                        _commonRequestService,
                        _unitOfWork.StoreProcedureRepository,
                        request.DTO.ModuleId,
                        request.DTO.OperationId,
                        cancellationToken);

                    targetInstitutionId = HostInstitutionIdentifierProtector.Decrypt(
                        request.DTO.InstitutionId,
                        hostContext.InstitutionEncryptionKey,
                        _idEncoderService);
                    hostInstitutionEncryptionKey = hostContext.InstitutionEncryptionKey;
                }

                // ===============================
                // 3️⃣ FETCH DATA
                // ===============================
                var pattern = await _unitOfWork
                    .InstitutionStaffCodePatternRepository
                    .GetInstitutionStaffMemberCodePatternAsync(
                        targetInstitutionId,
                        request.DTO.IsActive);

                if (pattern == null)
                {
                    _logger.LogInformation(
                        "No staffMember code pattern found.");

                    return ApiResponse<object>
                        .Success(null, "No pattern found.");
                }

                _logger.LogInformation(
                    "StaffMember code pattern retrieved.");

                object response = hostInstitutionEncryptionKey is null
                    ? pattern
                    : MapHostResponse(pattern, hostInstitutionEncryptionKey);

                return ApiResponse<object>
                    .Success(
                        response,
                        "Pattern fetched successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "GetStaffMemberCodePattern failed.");

                throw;
            }
        }

        private HostStaffMemberCodePatternResponseDTO MapHostResponse(
            GetStaffMemberCodePatternResponseDTO pattern,
            string institutionEncryptionKey)
        {
            return new HostStaffMemberCodePatternResponseDTO
            {
                Id = pattern.Id,
                InstitutionId = HostInstitutionIdentifierProtector.Encrypt(
                    pattern.InstitutionId,
                    institutionEncryptionKey,
                    _idEncoderService),
                Prefix = pattern.Prefix,
                IncludeYear = pattern.IncludeYear,
                IncludeMonth = pattern.IncludeMonth,
                IncludeDepartment = pattern.IncludeDepartment,
                Separator = pattern.Separator,
                RunningNumberLength = pattern.RunningNumberLength,
                LastUsedNumber = pattern.LastUsedNumber,
                IsActive = pattern.IsActive,
                AddedById = pattern.AddedById,
                AddedDateTime = pattern.AddedDateTime,
                UpdatedById = pattern.UpdatedById,
                UpdatedDateTime = pattern.UpdatedDateTime
            };
        }

    }
}

