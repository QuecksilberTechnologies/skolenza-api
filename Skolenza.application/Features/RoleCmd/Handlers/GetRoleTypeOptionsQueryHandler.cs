// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Publishes the centralized Institution role types for UI controls.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOS.Role;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.RoleCmd.Handlers
{
    #region Query

    /// <summary>
    /// Represents an authenticated request for the centralized Institution role-type options.
    /// </summary>
    public sealed class GetRoleTypeOptionsQuery
        : IRequest<ApiResponse<IReadOnlyList<RoleTypeOptionResponseDTO>>>
    {
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles role-type option requests without reading institution data or changing persistence.
    /// </summary>
    public sealed class GetRoleTypeOptionsQueryHandler
        : IRequestHandler<
            GetRoleTypeOptionsQuery,
            ApiResponse<IReadOnlyList<RoleTypeOptionResponseDTO>>>
    {
        /// <summary>
        /// Returns all supported role types from the shared role constants.
        /// </summary>
        public Task<ApiResponse<IReadOnlyList<RoleTypeOptionResponseDTO>>> Handle(
            GetRoleTypeOptionsQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<RoleTypeOptionResponseDTO> options = ConstantValues.InstitutionRoleTypes
                .Select(option => new RoleTypeOptionResponseDTO
                {
                    Id = option.Id,
                    Code = option.Code,
                    Name = option.DisplayName,
                    Description = option.Description
                })
                .ToList();

            return Task.FromResult(
                ApiResponse<IReadOnlyList<RoleTypeOptionResponseDTO>>.Success(
                    options,
                    AppConstants.SuccessMessages.RoleOptionsRetrieved));
        }
    }

    #endregion
}
