// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Validates authenticated Host or Institution requests and retrieves the shared Common menu hierarchy.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOS.Module.CommonMenu;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.ModuleCmd.CommonMenu.Handlers;

#region Query

/// <summary>
/// Represents a request to retrieve shared application navigation for the current authenticated principal.
/// </summary>
public sealed class GetCommonMenuQuery : IRequest<ApiResponse<IReadOnlyCollection<CommonMenuItemResponseDTO>>>;

#endregion

#region Handler

/// <summary>
/// Validates the current Host or Institution principal before returning the shared Common navigation hierarchy.
/// </summary>
public sealed class GetCommonMenuQueryHandler
    : IRequestHandler<GetCommonMenuQuery, ApiResponse<IReadOnlyCollection<CommonMenuItemResponseDTO>>>
{
    #region Fields

    private readonly ICommonRequestService _commonRequestService;
    private readonly IUnitOfWork _unitOfWork;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCommonMenuQueryHandler"/> class.
    /// </summary>
    /// <param name="commonRequestService">Validates the current Host or Institution principal.</param>
    /// <param name="unitOfWork">Provides the shared Module repository.</param>
    public GetCommonMenuQueryHandler(
        ICommonRequestService commonRequestService,
        IUnitOfWork unitOfWork)
    {
        _commonRequestService = commonRequestService;
        _unitOfWork = unitOfWork;
    }

    #endregion

    #region Handle

    /// <summary>
    /// Validates the authenticated principal and retrieves the deterministic shared Common navigation hierarchy.
    /// </summary>
    /// <param name="request">The parameterless authenticated Common-menu request.</param>
    /// <param name="cancellationToken">A token used to cancel request processing.</param>
    /// <returns>The application-wide Common menu available to authenticated users.</returns>
    public async Task<ApiResponse<IReadOnlyCollection<CommonMenuItemResponseDTO>>> Handle(
        GetCommonMenuQuery request,
        CancellationToken cancellationToken)
    {
        // Validate the authenticated principal before exposing shared application navigation.
        await _commonRequestService.ValidateAuthenticatedRequestAsync();

        var commonMenu = await _unitOfWork.ModuleRepository
            .GetCommonMenuHierarchyAsync(cancellationToken);

        if (commonMenu == null)
        {
            throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);
        }

        return ApiResponse<IReadOnlyCollection<CommonMenuItemResponseDTO>>.Success(
            commonMenu,
            AppConstants.SuccessMessages.CommonMenuRetrieved);
    }

    #endregion
}

#endregion
