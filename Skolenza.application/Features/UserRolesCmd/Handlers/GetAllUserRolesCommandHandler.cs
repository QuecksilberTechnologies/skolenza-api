// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Get All User Roles Command Handler requests.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.DTOS.UserRoles;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.UserRolesCmd.Handlers
{

    #region Command

    // ===============================
    // COMMAND
    // ===============================
    /// <summary>
    /// Represents the GetAllUserRolesCommand application component.
    /// </summary>
    public class GetAllUserRolesCommand : IRequest<ApiResponse<List<UserRoleDTO>>>
    {
        public GetUserRoleRequestDTO DTO { get; set; }

        public GetAllUserRolesCommand(GetUserRoleRequestDTO dto)
        {
            DTO = dto;
        }
    }

    // ===============================
    // HANDLER
    // ===============================
    /// <summary>
    /// Handles GetAllUserRolesCommand requests.
    /// </summary>
        #endregion

    #region Handler

public class GetAllUserRolesCommandHandler
    : IRequestHandler<GetAllUserRolesCommand, ApiResponse<List<UserRoleDTO>>>
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAllUserRolesCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IIdEncoderService _idEncoderService;

        public GetAllUserRolesCommandHandler(
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ILogger<GetAllUserRolesCommandHandler> logger,
            ICommonRequestService commonRequestService,
            IIdEncoderService idEncoderService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
            _idEncoderService = idEncoderService;
        }

        public async Task<ApiResponse<List<UserRoleDTO>>> Handle(
            GetAllUserRolesCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("🚀 GetAllUserRoles started");

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                if (request?.DTO == null)
                    throw new ValidationErrorException("Invalid request.");

                // ===============================
                // 2️⃣ STAFF_MEMBER ID DECODE
                // ===============================
                if (string.IsNullOrEmpty(request.DTO.StaffMemberId))
                    throw new ValidationErrorException("Invalid StaffMemberId.");

                var staffMemberId = RequestCommonHelper.DecodeOnlyStaffMemberId(
                    request.DTO.StaffMemberId,
                    validation.Claims.InstitutionEncriptionKey,
                    _idEncoderService);

                // ===============================
                // 3️⃣ GET USER ROLES
                // ===============================
                var userRoles = await _unitOfWork.UserRoleRepository
                    .GetStaffMemberRolesWithDetailsByIdAsync(
                        staffMemberId,
                        validation.InstitutionId);

                if (userRoles == null || userRoles.Count == 0)
                {
                    _logger.LogWarning("No roles found for StaffMemberId {StaffMemberId}", request.DTO.StaffMemberId);

                    return new ApiResponse<List<UserRoleDTO>>
                    {
                        IsSucceeded = true,
                        Message = "No roles found",
                        Data = new List<UserRoleDTO>()
                    };
                }

                // ===============================
                // 4️⃣ LOG ROLE IDS
                // ===============================
                var allRoleIdsCsv = string.Join(",",
                    userRoles
                        .Where(r => r.RoleId != null)
                        .Select(r => r.RoleId.ToString()));

                _logger.LogInformation(
                    "Fetched Role IDs for StaffMemberId {StaffMemberId}: {Roles}",
                    request.DTO.StaffMemberId,
                    allRoleIdsCsv);

                // ===============================
                // 5️⃣ MAP TO DTO
                // ===============================
                var userRoleDTOs = _mapper.Map<List<UserRoleDTO>>(userRoles);

                // ===============================
                // 6️⃣ RETURN RESPONSE
                // ===============================
                return new ApiResponse<List<UserRoleDTO>>
                {
                    IsSucceeded = true,
                    Message = "Roles fetched successfully",
                    Data = userRoleDTOs
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in GetAllUserRoles");
                throw; // middleware handle karega
            }
        }
    }


    #endregion
}

