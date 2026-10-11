// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves a direct SubModule for authenticated Host users.
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
    /// Represents a read-only request for one direct child SubModule in a required scope.
    /// </summary>
    public class GetSubModuleByIdQuery : IRequest<ApiResponse<GetSubModuleResponseDTO>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetSubModuleByIdQuery"/> class.
        /// </summary>
        /// <param name="id">The SubModule identifier.</param>
        /// <param name="moduleScope">The required module scope.</param>
        public GetSubModuleByIdQuery(int id, short moduleScope)
        {
            Id = id;
            ModuleScope = moduleScope;
        }

        /// <summary>Gets the SubModule identifier.</summary>
        public int Id { get; }

        /// <summary>Gets the required module scope.</summary>
        public short ModuleScope { get; }
    }

    #endregion
}

namespace skolenza.application.Features.ModuleCmd.SubModule.Handlers
{
    /// <summary>
    /// Handles Host-authorized lookup of a direct child SubModule in its requested scope.
    /// </summary>
    public class GetSubModuleByIdQueryHandler : IRequestHandler<GetSubModuleByIdQuery, ApiResponse<GetSubModuleResponseDTO>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<GetSubModuleByIdQueryHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetSubModuleByIdQueryHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">Provides the existing Module repository.</param>
        /// <param name="httpContextAccessor">Provides the ASP.NET Core-authenticated principal.</param>
        /// <param name="logger">Records unexpected processing failures.</param>
        public GetSubModuleByIdQueryHandler(
            IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor,
            ILogger<GetSubModuleByIdQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        #endregion

        #region MediatR Handler

        /// <summary>
        /// Retrieves one direct child only when its identifier and requested scope both match.
        /// </summary>
        /// <param name="request">The read-only request.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>The matching direct-child response.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when the request does not have a valid Host principal.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when the scoped direct child does not exist.</exception>
        public async Task<ApiResponse<GetSubModuleResponseDTO>> Handle(
            GetSubModuleByIdQuery request,
            CancellationToken cancellationToken)
        {
            GetAuthenticatedHostUserId();

            if (request == null || request.Id <= 0)
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
            }

            if (!IsSupportedModuleScope(request.ModuleScope))
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
            }

            try
            {
                var module = await _unitOfWork.ModuleRepository.GetSubModuleByIdAsync(
                    request.Id,
                    request.ModuleScope,
                    cancellationToken);

                if (module == null)
                {
                    throw new KeyNotFoundException("Child module was not found in the requested ModuleScope.");
                }

                return ApiResponse<GetSubModuleResponseDTO>.Success(module, "Child module retrieved successfully.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unable to retrieve Child module {ModuleId} in ModuleScope {ModuleScope}.", request.Id, request.ModuleScope);
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
