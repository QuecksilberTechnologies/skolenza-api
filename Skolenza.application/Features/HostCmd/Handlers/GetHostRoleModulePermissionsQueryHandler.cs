// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles HostRole module-operation permission retrieval.
// ================================================================

using skolenza.application.DTOS.Host;
using skolenza.application.Constants;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.HostCmd.Handler;

#region Query

/// <summary>
/// Represents the request to retrieve all available module-operation permissions for a Host role.
/// </summary>
public class GetHostRoleModulePermissionsQuery
    : IRequest<ApiResponse<GetHostRoleModulePermissionsResponseDTO>>
{
    /// <summary>
    /// Gets the Host-role identifier whose permission selection is requested.
    /// </summary>
    public long HostRoleId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetHostRoleModulePermissionsQuery"/> class.
    /// </summary>
    /// <param name="hostRoleId">The Host-role identifier.</param>
    public GetHostRoleModulePermissionsQuery(long hostRoleId)
    {
        HostRoleId = hostRoleId;
    }
}

#endregion

#region Handler

/// <summary>
/// Handles retrieval of the complete HostRole module-operation permission selection structure.
/// </summary>
public class GetHostRoleModulePermissionsQueryHandler
    : IRequestHandler<
        GetHostRoleModulePermissionsQuery,
        ApiResponse<GetHostRoleModulePermissionsResponseDTO>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="GetHostRoleModulePermissionsQueryHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides HostRole, Host permission, and module-operation repositories.</param>
    /// <param name="commonRequestService">Validates the current Host user request.</param>
    public GetHostRoleModulePermissionsQueryHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService)
    {
        _unitOfWork = unitOfWork;
        _commonRequestService = commonRequestService;
    }

    #endregion

    #region Handle

    /// <summary>
    /// Retrieves active, operational Host-scope module-operation mappings and marks those assigned to the requested Host role.
    /// </summary>
    /// <param name="request">The HostRole permission selection query.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The HostRole permission selection structure.</returns>
    public async Task<ApiResponse<GetHostRoleModulePermissionsResponseDTO>> Handle(
        GetHostRoleModulePermissionsQuery request,
        CancellationToken cancellationToken)
    {
        await _commonRequestService.ValidateHostSuperAdminRequestAsync();
        cancellationToken.ThrowIfCancellationRequested();

        if (request == null || request.HostRoleId <= 0)
        {
            throw new ValidationErrorException("A valid Host role ID is required.");
        }

        var hostRole = await _unitOfWork.HostRoleRepository
            .GetByIdAsync(request.HostRoleId)
            ?? throw new ApiException("Host role was not found.", 404);

        var activeMappings = (await _unitOfWork.ModuleRepository
                .GetAllModuleOperationMappingsAsync(cancellationToken))
            .Where(mapping =>
                mapping.IsActive == true &&
                mapping.IsOperational == true &&
                mapping.Module.ModuleScope == (short)AppConstants.HostModuleScope &&
                !mapping.Module.IsCommonMenu)
            .ToList();

        // The editor reflects persisted grants for every Host role, including
        // Host-Super-Admin. The seed creates the initial complete grant set;
        // subsequent changes are represented by HostRoleModuleAndPermission.
        var assignedPairs = (await _unitOfWork.HostRolePermissionRepository
                .GetHostUserPermissionsAsync(hostRole.Id, cancellationToken))
            .Select(permission => (permission.ModuleId, permission.OperationId))
            .ToHashSet();

        var response = new GetHostRoleModulePermissionsResponseDTO
        {
            HostRoleId = hostRole.Id,
            HostRoleName = hostRole.Name,
            Modules = activeMappings
                .GroupBy(mapping => mapping.ModuleId)
                .Select(moduleGroup => new HostRoleModulePermissionsModuleResponseDTO
                {
                    ModuleId = moduleGroup.Key,
                    ModuleName = moduleGroup.First().Module?.ModuleName ?? string.Empty,
                    Operations = moduleGroup
                        .GroupBy(mapping => mapping.OperationId)
                        .Select(operationGroup => operationGroup
                            .OrderBy(mapping => mapping.Id)
                            .First())
                        .OrderBy(mapping => mapping.Operation.OperationName)
                        .Select(mapping => new HostRoleModulePermissionsOperationResponseDTO
                        {
                            ModuleOperationMappingId = mapping.Id,
                            OperationId = mapping.OperationId,
                            OperationName = mapping.Operation.OperationName,
                            IsAllowed = assignedPairs.Contains((mapping.ModuleId, mapping.OperationId))
                        })
                        .ToList()
                })
                .OrderBy(module => module.ModuleName)
                .ThenBy(module => module.ModuleId)
                .ToList()
        };

        return ApiResponse<GetHostRoleModulePermissionsResponseDTO>.Success(
            response,
            "Host role permissions retrieved successfully.");
    }

    #endregion
}

#endregion
