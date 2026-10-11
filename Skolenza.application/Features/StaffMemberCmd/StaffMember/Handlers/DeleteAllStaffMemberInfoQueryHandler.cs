// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Delete All StaffMember Info Query Handler requests.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;


namespace skolenza.application.Features.StaffMemberCmd.StaffMemberBase.Handlers
{

    #region Query

    /// <summary>
    /// Represents the DeleteStaffMemberQuery application component.
    /// </summary>
    public class DeleteStaffMemberQuery : IRequest<ApiResponse<bool>>
    {
      public DeleteBaseStaffMemberRequestDTO DTO;

        public DeleteStaffMemberQuery(DeleteBaseStaffMemberRequestDTO dto)
        {
            DTO = dto;
        }
    }
    /// <summary>
    /// Handles DeleteBaseStaffMemberInfoQuery requests.
    /// </summary>
        #endregion

    #region Handler

public class DeleteBaseStaffMemberInfoQueryHandler : IRequestHandler<DeleteStaffMemberQuery, ApiResponse<bool>>
    {
        private readonly IBaseStaffMemberRepository _staffMemberRepository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteBaseStaffMemberInfoQueryHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IIdEncoderService _idEncoderService;
        private readonly IPermissionService _permissionService;
        public DeleteBaseStaffMemberInfoQueryHandler(
            IBaseStaffMemberRepository staffMemberRepository,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            IUnitOfWork unitOfWork,
            ILogger<DeleteBaseStaffMemberInfoQueryHandler> logger, ICommonRequestService commonRequestService, IIdEncoderService idEncoderService, IPermissionService permissionService)
        {
            _staffMemberRepository = staffMemberRepository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
            _idEncoderService = idEncoderService;
            _permissionService = permissionService;
        }
        public async Task<ApiResponse<bool>> Handle(
    DeleteStaffMemberQuery request,
    CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("DeleteStaffMember started");

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                var validation =
                    await _commonRequestService.ValidateInstitutionUserRequestAsync();

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                // ===============================
                // 2️⃣ NULL SAFETY
                // ===============================
                if (request?.DTO == null)
                    throw new ValidationErrorException("Invalid request.");
                var staffMemberId = RequestCommonHelper.DecodeOnlyStaffMemberId(
                        request.DTO.StaffMemberId,
                        validation.Claims.InstitutionEncriptionKey,
                        _idEncoderService);

                if (staffMemberId <= 0)
                    throw new ValidationErrorException("Invalid StaffMemberId.");
                // 4️⃣ FETCH STAFF_MEMBER
                // ===============================
                var staffMember =
                    await _unitOfWork.StaffMembers.GetByIdAsync(
                        staffMemberId,
                        validation.InstitutionId,
                        true);

                if (staffMember == null)
                    throw new ApiException("StaffMember not found.", 404);

                // ===============================
                // 5️⃣ START TRANSACTION (IMPORTANT)
                // ===============================
                await _unitOfWork.BeginTransactionAsync();

                // ===============================
                // 6️⃣ DELETE (SOFT / CASCADE)
                // ===============================
                var isSuccess =
                    await _unitOfWork.StaffMembers.DeleteAllAsync(staffMember);

                if (!isSuccess)
                    throw new ApiException("Failed to delete staffMember.", 500);

                // ===============================
                // 7️⃣ COMMIT
                // ===============================
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("DeleteStaffMember success | Id: {Id}", staffMemberId);

                return ApiResponse<bool>.Success(true, "StaffMember deleted successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                _logger.LogError(
                    ex,
                    "Error deleting staffMember | Id: {Id}",
                    request.DTO?.StaffMemberId);

                throw; //  MUST
            }
        }

    }



    #endregion
}


