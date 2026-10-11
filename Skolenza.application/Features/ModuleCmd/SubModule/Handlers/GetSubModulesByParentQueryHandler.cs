// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves direct children of a Header Module for Host users.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.Exceptions;
using skolenza.application.DTOS.Module.SubModule;
using skolenza.application.Interfaces;
using skolenza.application.Wrappers;
using skolenza.application.Features.ModuleCmd.SubModule.Commands;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace skolenza.application.Features.ModuleCmd.SubModule.Commands
{
    #region Query

    /// <summary>
    /// Represents a read-only request for direct children of one Header Module in a required scope.
    /// </summary>
    public class GetSubModulesByParentQuery : IRequest<ApiResponse<List<GetSubModuleResponseDTO>>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetSubModulesByParentQuery"/> class.
        /// </summary>
        /// <param name="parentModuleId">The Header Module identifier.</param>
        /// <param name="moduleScope">The required module scope.</param>
        /// <param name="isActive">When supplied, filters results by active state.</param>
        public GetSubModulesByParentQuery(int parentModuleId, short moduleScope, bool? isActive)
        {
            ParentModuleId = parentModuleId;
            ModuleScope = moduleScope;
            IsActive = isActive;
        }

        /// <summary>Gets the Header Module identifier.</summary>
        public int ParentModuleId { get; }

        /// <summary>Gets the required module scope.</summary>
        public short ModuleScope { get; }

        /// <summary>Gets the optional active-state filter.</summary>
        public bool? IsActive { get; }
    }

    #endregion
}

namespace skolenza.application.Features.ModuleCmd.SubModule.Handlers
{
    /// <summary>
    /// Handles Host-authorized retrieval of direct children after validating the Header Module shape and scope.
    /// </summary>
    public class GetSubModulesByParentQueryHandler : IRequestHandler<GetSubModulesByParentQuery, ApiResponse<List<GetSubModuleResponseDTO>>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<GetSubModulesByParentQueryHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetSubModulesByParentQueryHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">Provides the existing Module repository.</param>
        /// <param name="httpContextAccessor">Provides the ASP.NET Core-authenticated principal.</param>
        /// <param name="logger">Records unexpected processing failures.</param>
        public GetSubModulesByParentQueryHandler(
            IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor,
            ILogger<GetSubModulesByParentQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        #endregion

        #region MediatR Handler

        /// <summary>
        /// Validates the requested Header Module and retrieves only its direct child modules.
        /// </summary>
        /// <param name="request">The read-only parent-child request.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>The ordered direct-child list, which may be empty.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when the request does not have a valid Host principal.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when the scoped Header Module does not exist.</exception>
        public async Task<ApiResponse<List<GetSubModuleResponseDTO>>> Handle(
            GetSubModulesByParentQuery request,
            CancellationToken cancellationToken)
        {
            GetAuthenticatedHostUserId();

            if (request == null || request.ParentModuleId <= 0)
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
            }

            if (!IsSupportedModuleScope(request.ModuleScope))
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
            }

            try
            {
                var parentModule = await _unitOfWork.ModuleRepository.GetParentModuleForSubModuleAsync(
                    request.ParentModuleId,
                    request.ModuleScope,
                    cancellationToken);

                if (parentModule == null)
                {
                    throw new KeyNotFoundException("Parent header was not found in the requested ModuleScope.");
                }

                var modules = await _unitOfWork.ModuleRepository.GetSubModulesAsync(
                    request.ModuleScope,
                    parentModule.Id,
                    request.IsActive,
                    cancellationToken);

                return ApiResponse<List<GetSubModuleResponseDTO>>.Success(modules, "Child modules retrieved successfully.");
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unable to retrieve Child modules for ParentHeaderId {ParentHeaderId} in ModuleScope {ModuleScope}.",
                    request.ParentModuleId,
                    request.ModuleScope);
                throw;
            }
        }

        #endregion

        #region Authentication

        /// <summary>
        /// Verifies that the ASP.NET Core-authenticated principal is a Host user.
        /// </summary>
        /// <returns>The authenticated Host user identifier.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when the principal is missing, unauthenticated, non-Host, or lacks a valid Host user identifier.</exception>
        private long GetAuthenticatedHostUserId()
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                throw new UnauthorizedAccessException("An authenticated Host principal is required.");
            }

            var userType = principal.FindFirst(AppConstants.UserTypeClaim)?.Value;
            if (!string.Equals(userType, AppConstants.HostUserType, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Only Host users can retrieve Child modules.");
            }

            var hostUserIdValue = principal.FindFirst(AppConstants.HostUserIdClaim)?.Value;
            if (!long.TryParse(hostUserIdValue, out var hostUserId) || hostUserId <= 0)
            {
                throw new UnauthorizedAccessException("A valid HostUserId claim is required.");
            }

            return hostUserId;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Determines whether the requested scope is one of the two supported application module scopes.
        /// </summary>
        /// <param name="moduleScope">The requested module scope.</param>
        /// <returns><see langword="true"/> when the scope is supported.</returns>
        private static bool IsSupportedModuleScope(short moduleScope)
        {
            return moduleScope == AppConstants.InstitutionModuleScope ||
                   moduleScope == AppConstants.HostModuleScope;
        }

        #endregion
    }
}
