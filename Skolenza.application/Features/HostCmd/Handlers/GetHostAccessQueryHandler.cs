// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Resolves the authenticated Host user's current role permissions and composes lightweight runtime module access.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOS.Host;
using skolenza.application.DTOS.Host.Access;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.HostCmd.Handler;

#region Query

/// <summary>
/// Represents the parameterless request for the authenticated Host user's current module and operation access.
/// </summary>
public sealed class GetHostAccessQuery : IRequest<ApiResponse<HostAccessResponseDTO>>;

#endregion

#region Handler

/// <summary>
/// Validates the Host identity and returns the current HostRole module-operation permission set.
/// </summary>
public sealed class GetHostAccessQueryHandler
    : IRequestHandler<GetHostAccessQuery, ApiResponse<HostAccessResponseDTO>>
{
    #region Fields

    private readonly ICommonRequestService _commonRequestService;
    private readonly IUnitOfWork _unitOfWork;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="GetHostAccessQueryHandler"/> class.
    /// </summary>
    /// <param name="commonRequestService">Validates the authenticated Host request.</param>
    /// <param name="unitOfWork">Provides current Host user, role, and permission queries.</param>
    public GetHostAccessQueryHandler(
        ICommonRequestService commonRequestService,
        IUnitOfWork unitOfWork)
    {
        _commonRequestService = commonRequestService;
        _unitOfWork = unitOfWork;
    }

    #endregion

    #region Handle

    /// <summary>
    /// Retrieves the authenticated Host user's current role-based module and operation access.
    /// </summary>
    /// <param name="request">The parameterless Host access query.</param>
    /// <param name="cancellationToken">Token used to cancel the asynchronous request.</param>
    /// <returns>The current effective Host access bootstrap.</returns>
    public async Task<ApiResponse<HostAccessResponseDTO>> Handle(
        GetHostAccessQuery request,
        CancellationToken cancellationToken)
    {
        // Validate the Host identity before resolving current authorization state.
        var hostUserId = await _commonRequestService.ValidateHostUserRequestAsync();
        var hostUser = await _unitOfWork.HostUserRepository.GetByIdAsync(hostUserId);
        if (hostUser == null || !hostUser.IsActive || hostUser.IsSoftDeleted)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        var hostRole = hostUser.HostRole;
        if (hostRole == null || !hostRole.IsActive || hostRole.IsSoftDeleted)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        // Read permissions from the Host-specific authorization model used by legacy Host login.
        var permissions = await _unitOfWork.HostRolePermissionRepository
            .GetHostUserPermissionsAsync(hostRole.Id, cancellationToken);
        
        var response = new HostAccessResponseDTO
        {
            Modules = BuildModules(permissions)
        };

        return ApiResponse<HostAccessResponseDTO>.Success(
            response,
            AppConstants.SuccessMessages.HostAccessRetrieved);
    }

    #endregion

    #region Access Composition

    /// <summary>
    /// Groups current Host permission rows into modules with unique allowed operations.
    /// </summary>
    /// <param name="permissions">The current Host permission rows projected from the Host authorization model.</param>
    /// <returns>The ordered Host module access collection.</returns>
    private static IReadOnlyCollection<HostAccessModuleResponseDTO> BuildModules(
        IReadOnlyCollection<HostUserPermissionResponseDTO> permissions)
    {
        return permissions
            // Preserve the legacy Host permission set; composition only deduplicates module-operation grants.
            .GroupBy(permission => new
            {
                permission.ModuleId,
                permission.ModuleName,
                permission.DisplayName
            })
            .OrderBy(group => group.Key.ModuleName, StringComparer.Ordinal)
            .ThenBy(group => group.Key.ModuleId)
            .Select(moduleGroup => new HostAccessModuleResponseDTO
            {
                ModuleId = moduleGroup.Key.ModuleId,
                ModuleName = moduleGroup.Key.ModuleName!,
                DisplayName = moduleGroup.Key.DisplayName,
                Operations = moduleGroup
                    .GroupBy(permission => permission.OperationId)
                    .Select(operationGroup => operationGroup
                        .OrderBy(permission => permission.OperationName, StringComparer.Ordinal)
                        .First())
                    .OrderBy(permission => permission.OperationName, StringComparer.Ordinal)
                    .ThenBy(permission => permission.OperationId)
                    .Select(permission => new HostAccessOperationResponseDTO
                    {
                        OperationId = permission.OperationId,
                        OperationName = permission.OperationName!
                    })
                    .ToArray()
            })
            .ToArray();
    }

    #endregion
}

#endregion
