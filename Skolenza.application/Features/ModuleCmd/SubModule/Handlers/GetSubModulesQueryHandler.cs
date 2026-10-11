// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves direct SubModule lists for authenticated Host users.
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
    /// Represents a read-only request for direct child SubModules in a required scope.
    /// </summary>
    public class GetSubModulesQuery : IRequest<ApiResponse<List<GetSubModuleResponseDTO>>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetSubModulesQuery"/> class.
        /// </summary>
        /// <param name="moduleScope">The required module scope.</param>
        /// <param name="parentModuleId">When supplied, filters direct children by Header Module identifier.</param>
        /// <param name="isActive">When supplied, filters results by active state.</param>
        public GetSubModulesQuery(short moduleScope, int? parentModuleId, bool? isActive)
        {
            ModuleScope = moduleScope;
            ParentModuleId = parentModuleId;
            IsActive = isActive;
        }

        /// <summary>Gets the required module scope.</summary>
        public short ModuleScope { get; }

        /// <summary>Gets the optional Header Module filter.</summary>
        public int? ParentModuleId { get; }

        /// <summary>Gets the optional active-state filter.</summary>
        public bool? IsActive { get; }
    }

    #endregion
}

namespace skolenza.application.Features.ModuleCmd.SubModule.Handlers
{
    /// <summary>
    /// Handles Host-authorized retrieval of direct child SubModule lists in one scope.
    /// </summary>
    public class GetSubModulesQueryHandler : IRequestHandler<GetSubModulesQuery, ApiResponse<List<GetSubModuleResponseDTO>>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<GetSubModulesQueryHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetSubModulesQueryHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">Provides the existing Module repository.</param>
        /// <param name="httpContextAccessor">Provides the ASP.NET Core-authenticated principal.</param>
        /// <param name="logger">Records unexpected processing failures.</param>
        public GetSubModulesQueryHandler(
            IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor,
            ILogger<GetSubModulesQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        #endregion

        #region MediatR Handler

        /// <summary>
        /// Retrieves direct children in the requested scope with no N+1 parent lookup.
        /// </summary>
        /// <param name="request">The read-only list request.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>The ordered direct-child list, which may be empty.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when the request does not have a valid Host principal.</exception>
        public async Task<ApiResponse<List<GetSubModuleResponseDTO>>> Handle(
            GetSubModulesQuery request,
            CancellationToken cancellationToken)
        {
            GetAuthenticatedHostUserId();

            if (request == null)
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
            }

            if (!IsSupportedModuleScope(request.ModuleScope))
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
            }

            if (request.ParentModuleId.HasValue && request.ParentModuleId.Value <= 0)
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
            }

            try
            {
                var modules = await _unitOfWork.ModuleRepository.GetSubModulesAsync(
                    request.ModuleScope,
                    request.ParentModuleId,
                    request.IsActive,
                    cancellationToken);

                return ApiResponse<List<GetSubModuleResponseDTO>>.Success(modules, "Child modules retrieved successfully.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unable to retrieve Child modules in ModuleScope {ModuleScope}.", request.ModuleScope);
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
