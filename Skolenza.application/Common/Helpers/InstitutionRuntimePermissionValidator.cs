// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Converts the trusted Institution permission-function result into the
//           correct authorization exception for every application feature.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOS.RoleModulePermission;
using skolenza.application.Exceptions;

namespace skolenza.application.Common.Helpers;

/// <summary>
/// Applies the common HTTP authorization contract to a current Institution
/// module-operation permission result.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><c>1</c>: permission is granted.</description></item>
/// <item><description><c>0</c>: the authenticated user is not permitted, so the API returns 403.</description></item>
/// <item><description><c>-1</c>, <c>-2</c>, or an unknown result: the authentication context is invalid or stale, so the API returns 401.</description></item>
/// </list>
/// </remarks>
public static class InstitutionRuntimePermissionValidator
{
    /// <summary>
    /// Allows a granted Institution permission result or throws the standardized
    /// exception that the API error middleware serializes.
    /// </summary>
    /// <param name="permissionResult">The result returned by the Institution permission function.</param>
    /// <exception cref="ForbiddenAccessException">Thrown when an authenticated user has no requested permission.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the authentication context is invalid or stale.</exception>
    public static void EnsureAllowed(InstitutionsUserPermissionCheckResponseDTO? permissionResult)
    {
        var resultCode = permissionResult?.ResultCode;

        switch (resultCode)
        {
            case 1:
                return;
            case 0:
                throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
            case -1:
            case -2:
            default:
                throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }
    }
}
