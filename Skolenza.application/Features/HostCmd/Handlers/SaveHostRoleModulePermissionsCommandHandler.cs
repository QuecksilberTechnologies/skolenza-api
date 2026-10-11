// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles bulk HostRole module-operation permission assignment.
// ================================================================

using skolenza.application.DTOS.Host;
using skolenza.application.Constants;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;

namespace skolenza.application.Features.HostCmd.Handler;

#region Command

/// <summary>
/// Represents the request to save the complete module-operation permission selection for a Host role.
/// </summary>
public class SaveHostRoleModulePermissionsCommand
    : IRequest<ApiResponse<int>>
{
    /// <summary>
    /// Gets the selected HostRole module-operation permissions.
    /// </summary>
    public SaveHostRoleModulePermissionsRequestDTO? DTO { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SaveHostRoleModulePermissionsCommand"/> class.
    /// </summary>
    /// <param name="dto">The complete HostRole permission selection.</param>
    public SaveHostRoleModulePermissionsCommand(SaveHostRoleModulePermissionsRequestDTO? dto)
    {
        DTO = dto;
    }
}

#endregion

#region Handler

/// <summary>
/// Handles delta-based save operations for HostRole module-operation permissions.
/// </summary>
public class SaveHostRoleModulePermissionsCommandHandler
    : IRequestHandler<SaveHostRoleModulePermissionsCommand, ApiResponse<int>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SaveHostRoleModulePermissionsCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides HostRole, Host permission, and module-operation repositories.</param>
    /// <param name="commonRequestService">Validates the current Host user request.</param>
    public SaveHostRoleModulePermissionsCommandHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService)
    {
        _unitOfWork = unitOfWork;
        _commonRequestService = commonRequestService;
    }

    #endregion

    #region Handle

    /// <summary>
    /// Inserts, preserves, reactivates, or soft-deactivates permission rows according to the requested selection.
    /// </summary>
    /// <param name="request">The HostRole permission selection command.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The number of permission rows changed.</returns>
    public async Task<ApiResponse<int>> Handle(
        SaveHostRoleModulePermissionsCommand request,
        CancellationToken cancellationToken)
    {
        // Permission administration itself is a Host Admin responsibility. This is
        // deliberately stricter than ordinary Host API authentication so another
        // Host role cannot alter its own or another role's grants.
        var hostUserId = (await _commonRequestService
            .ValidateHostSuperAdminRequestAsync()).HostUserId;
        cancellationToken.ThrowIfCancellationRequested();

        var dto = request?.DTO
            ?? throw new ValidationErrorException("Host role permission details are required.");
        var requestedPermissions = dto.Permissions
            ?? throw new ValidationErrorException("Permission selection is required.");

        if (dto.HostRoleId <= 0)
        {
            throw new ValidationErrorException("A valid Host role ID is required.");
        }

        if (requestedPermissions.Any(permission =>
                permission == null ||
                permission.ModuleId <= 0 ||
                permission.OperationId <= 0))
        {
            throw new ValidationErrorException("Every permission requires valid module and operation IDs.");
        }

        var requestedPairs = requestedPermissions
            .Select(permission => (permission.ModuleId, permission.OperationId))
            .ToList();
        if (requestedPairs.Distinct().Count() != requestedPairs.Count)
        {
            throw new ValidationErrorException("Duplicate module-operation permissions are not allowed.");
        }

        var hostRole = await _unitOfWork.HostRoleRepository
            .GetByIdAsync(dto.HostRoleId)
            ?? throw new ApiException("Host role was not found.", 404);

        var activeHostMappings = (await _unitOfWork.ModuleRepository
                .GetAllModuleOperationMappingsAsync(cancellationToken))
            .Where(mapping =>
                mapping.IsActive == true &&
                mapping.IsOperational == true &&
                mapping.Module is { IsActive: true } &&
                mapping.Module.ModuleScope == (short)AppConstants.HostModuleScope &&
                mapping.Operation.IsActive)
            .ToList();
        var validMappingPairs = activeHostMappings
            .Select(mapping => (mapping.ModuleId, mapping.OperationId))
            .ToHashSet();
        if (requestedPairs.Any(pair => !validMappingPairs.Contains(pair)))
        {
            throw new ValidationErrorException(
                "Every requested module-operation permission must be an active module-operation mapping.");
        }

        var requestedSet = requestedPairs.ToHashSet();
        var existingPermissions = await _unitOfWork.HostRolePermissionRepository
            .GetByHostRoleIdAsync(hostRole.Id, cancellationToken);
        var existingByPair = existingPermissions.ToDictionary(
            permission => (permission.ModuleId, permission.OperationId));
        var utcNow = DateTime.UtcNow;

        var toDeactivate = existingPermissions
            .Where(permission =>
                permission.IsActive &&
                !permission.IsSoftDeleted &&
                !requestedSet.Contains((permission.ModuleId, permission.OperationId)))
            .ToList();
        foreach (var permission in toDeactivate)
        {
            permission.IsActive = false;
            permission.IsSoftDeleted = true;
            permission.UpdatedById = hostUserId;
            permission.UpdatedDateTime = utcNow;
            permission.DeletedById = hostUserId;
            permission.DeletedDateTime = utcNow;
        }

        var toReactivate = existingPermissions
            .Where(permission =>
                requestedSet.Contains((permission.ModuleId, permission.OperationId)) &&
                (!permission.IsActive || permission.IsSoftDeleted))
            .ToList();
        foreach (var permission in toReactivate)
        {
            permission.IsActive = true;
            permission.IsSoftDeleted = false;
            permission.UpdatedById = hostUserId;
            permission.UpdatedDateTime = utcNow;
            permission.DeletedById = null;
            permission.DeletedDateTime = null;
        }

        var toInsert = requestedPairs
            .Where(pair => !existingByPair.ContainsKey(pair))
            .Select(pair => new HostRoleModuleAndPermission
            {
                HostRoleId = hostRole.Id,
                ModuleId = pair.ModuleId,
                OperationId = pair.OperationId,
                IsActive = true,
                IsSoftDeleted = false,
                AddedById = hostUserId,
                AddedDateTime = utcNow,
                UpdatedById = null,
                UpdatedDateTime = null,
                DeletedById = null,
                DeletedDateTime = null
            })
            .ToList();

        if (toInsert.Count > 0)
        {
            await _unitOfWork.HostRolePermissionRepository
                .BulkInsertAsync(toInsert, cancellationToken);
        }

        var changedCount = toInsert.Count + toReactivate.Count + toDeactivate.Count;
        if (changedCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ApiResponse<int>.Success(
            changedCount,
            "Host role permissions updated successfully.");
    }

    #endregion
}

#endregion
