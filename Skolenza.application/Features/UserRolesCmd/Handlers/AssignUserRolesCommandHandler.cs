// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Assign User Roles Command Handler requests.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.DTOS.UserRoles;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.UserRolesCmd.Handlers
{

    #region Command

    // ===============================
    // COMMAND
    // ===============================
    /// <summary>
    /// Represents the AssignUserRolesCommand application component.
    /// </summary>
    public class AssignUserRolesCommand  : IRequest<ApiResponse<List<UserRoleDTO>>>
    {
        public UserRoleListDTO DTO { get; set; }

        public AssignUserRolesCommand(UserRoleListDTO dto)
        {
            DTO = dto;
        }
    }

    // ===============================
    // HANDLER
    // ===============================
    /// <summary>
    /// Handles AssignUserRolesCommand requests.
    /// </summary>
        #endregion

    #region Handler

public class AssignUserRolesCommandHandler
    : IRequestHandler<AssignUserRolesCommand, ApiResponse<List<UserRoleDTO>>>
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AssignUserRolesCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IIdEncoderService _idEncoderService;

        public AssignUserRolesCommandHandler(
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ILogger<AssignUserRolesCommandHandler> logger,
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
    AssignUserRolesCommand request,
    CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("🚀 AssignUserRoles started");

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                if (request?.DTO == null)
                    throw new ValidationErrorException("Invalid request.");

                // ===============================
                // 2️⃣ STAFF_MEMBER ID CHECK
                // ===============================
                if (string.IsNullOrWhiteSpace(request.DTO.StaffMemberId))
                    throw new ValidationErrorException("Invalid StaffMemberId.");

                var staffMemberId = RequestCommonHelper.DecodeOnlyStaffMemberId(
                    request.DTO.StaffMemberId,
                    validation.Claims.InstitutionEncriptionKey,
                    _idEncoderService);

                // ===============================
                // 3️⃣ FETCH EXISTING ROLES
                // ===============================
                var existingRoles = await _unitOfWork.UserRoleRepository
                    .GetStaffMemberRolesWithDetailsByIdAsync(staffMemberId, validation.InstitutionId);

                var incomingRoles = request.DTO.UserRoles ?? new List<UserRoleDTO>();

                //  Convert to HashSet for ultra fast lookup (O(1))
                var existingRoleIds = existingRoles.Select(x => x.RoleId).ToHashSet();
                var incomingRoleIds = incomingRoles.Select(x => x.RoleId).ToHashSet();

                // ==========================================================
                //  BULK DELETE (Soft Delete)
                // ==========================================================

                var rolesToDelete = existingRoles
                    .Where(x => !incomingRoleIds.Contains(x.RoleId ?? 0))
                             .ToList();

                if (rolesToDelete.Any())
                {
                    foreach (var role in rolesToDelete)
                    {
                        role.IsSoftDeleted = true;
                        role.IsActive = false;
                        role.SoftDeletedById = validation.LoggedInStaffMemberId;
                        role.DeletedDateTime = DateTime.UtcNow;
                    }

                    //  Single bulk update call
                    _unitOfWork.UserRoleRepository.UpdateRange(rolesToDelete);
                }

                // ==========================================================
                //  BULK INSERT
                // ==========================================================
                var rolesToInsert = incomingRoles
                    .Where(x => !existingRoleIds.Contains(x.RoleId))
                    .Select(role => new UserRole
                    {
                        StaffMemberId = staffMemberId,
                        RoleId = role.RoleId,

                        IsPrimaryRole = false,


                        IsActive = role.IsActive,
                        IsSoftDeleted = false,

                        AddedById = validation.LoggedInStaffMemberId,
                        AddedDateTime = DateTime.UtcNow,



                    })
                    .ToList();

                if (rolesToInsert.Any())
                {
                    await _unitOfWork.UserRoleRepository.AddRangeAsync(rolesToInsert);
                }

                // ==========================================================
                //  BULK UPDATE
                // ==========================================================
                var rolesToUpdate = existingRoles
                    .Where(x => incomingRoleIds.Contains(x.RoleId??0))
                    .ToList();

                if (rolesToUpdate.Any())
                {
                    foreach (var existing in rolesToUpdate)
                    {
                        var input = incomingRoles.First(x => x.RoleId == existing.RoleId);

                        existing.IsActive = input.IsActive;

                        // force business rules
                        existing.IsPrimaryRole = false;


                        existing.UpdatedById = validation.LoggedInStaffMemberId;
                        existing.UpdatedDateTime = DateTime.UtcNow;
                    }

                    //  Single bulk update call
                    _unitOfWork.UserRoleRepository.UpdateRange(rolesToUpdate);
                }

                // ===============================
                //  SINGLE DB COMMIT
                // ===============================
                await _unitOfWork.SaveChangesAsync();

                // ===============================
                // 9️⃣ RETURN UPDATED DATA
                // ===============================
                var updatedRoles = await _unitOfWork.UserRoleRepository
                    .GetStaffMemberRolesWithDetailsByIdAsync(staffMemberId, validation.InstitutionId);

                var userRoleDTOs = _mapper.Map<List<UserRoleDTO>>(updatedRoles);

                return new ApiResponse<List<UserRoleDTO>>
                {
                    IsSucceeded = true,
                    Message = "Roles assigned successfully",
                    Data = userRoleDTOs
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in AssignUserRoles");
                throw;
            }
        }
    }

    #endregion
}

