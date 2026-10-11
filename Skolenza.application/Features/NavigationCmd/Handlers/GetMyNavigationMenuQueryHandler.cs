// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves the lightweight navigation hierarchy for the authenticated principal.
// ================================================================

using skolenza.application.Common.Enums;
using skolenza.application.Constants;
using skolenza.application.DTOS.Navigation;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.NavigationCmd.Handlers;

/// <summary>
/// Represents the parameterless request to retrieve the current user's permitted navigation hierarchy.
/// </summary>
public sealed class GetMyNavigationMenuQuery : IRequest<ApiResponse<NavigationMenuResponseDTO>>;

/// <summary>
/// Retrieves only the entitled, role-permitted Institution or Host Module hierarchy.
/// </summary>
public sealed class GetMyNavigationMenuQueryHandler
    : IRequestHandler<GetMyNavigationMenuQuery, ApiResponse<NavigationMenuResponseDTO>>
{
    private readonly ICommonRequestService _commonRequestService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetMyNavigationMenuQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMyNavigationMenuQueryHandler"/> class.
    /// </summary>
    public GetMyNavigationMenuQueryHandler(
        ICommonRequestService commonRequestService,
        IUnitOfWork unitOfWork,
        ILogger<GetMyNavigationMenuQueryHandler> logger)
    {
        _commonRequestService = commonRequestService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Resolves the authenticated principal from the access token and returns only its currently authorized navigation.
    /// </summary>
    public async Task<ApiResponse<NavigationMenuResponseDTO>> Handle(
        GetMyNavigationMenuQuery request,
        CancellationToken cancellationToken)
    {
        var principal = await _commonRequestService.ValidateAuthenticatedRequestAsync();
        IReadOnlyCollection<NavigationMenuItemResponseDTO> menuItems;

        if (principal.UserType == LoginUserType.InstitutionStaffMember)
        {
            if (!principal.InstitutionId.HasValue || principal.InstitutionId.Value <= 0 || principal.AuthenticatedUserId <= 0)
            {
                throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
            }

            menuItems = await _unitOfWork.ModuleRepository.GetInstitutionNavigationMenuAsync(
                principal.InstitutionId.Value,
                principal.AuthenticatedUserId,
                cancellationToken);
        }
        else if (principal.UserType == LoginUserType.Host)
        {
            // The authenticated-request validation rejects a stale Host-role token. Resolve the
            // current role only to build the role's permitted menu without accepting client input.
            var hostContext = await _commonRequestService.ValidateHostUserPermissionRequestAsync();
            if (hostContext.TokenHostRoleId != hostContext.CurrentHostRoleId)
            {
                throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
            }

            menuItems = await _unitOfWork.ModuleRepository.GetHostNavigationMenuAsync(
                hostContext.CurrentHostRoleId,
                cancellationToken);
        }
        else
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        _logger.LogInformation(
            "Retrieved {MenuCount} root navigation item(s) for {UserType} user {UserId}.",
            menuItems.Count,
            principal.UserType,
            principal.AuthenticatedUserId);

        return ApiResponse<NavigationMenuResponseDTO>.Success(
            new NavigationMenuResponseDTO
            {
                UserType = principal.UserType.ToString(),
                Items = menuItems
            },
            AppConstants.SuccessMessages.NavigationMenuRetrieved);
    }
}
