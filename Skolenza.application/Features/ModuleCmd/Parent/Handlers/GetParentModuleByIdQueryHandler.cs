// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves a scope-filtered Parent/Header Module for Host users.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOS.Module.ParentModule;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.ModuleCmd.Parent.Commands
{
    #region Query

    /// <summary>
    /// Represents a read-only request for one Parent/Header Module in a required scope.
    /// </summary>
    public class GetParentModuleByIdQuery : IRequest<ApiResponse<GetParentModuleResponseDTO>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetParentModuleByIdQuery"/> class.
        /// </summary>
        /// <param name="id">The Header Module identifier.</param>
        /// <param name="moduleScope">The requested module scope.</param>
        public GetParentModuleByIdQuery(int id, short moduleScope)
        {
            Id = id;
            ModuleScope = moduleScope;
        }

        /// <summary>Gets the Header Module identifier.</summary>
        public int Id { get; }

        /// <summary>Gets the requested module scope.</summary>
        public short ModuleScope { get; }
    }

    #endregion

    /// <summary>
    /// Handles Host-authorized lookup of a Parent/Header Module within its requested scope.
    /// </summary>
    public class GetParentModuleByIdQueryHandler : IRequestHandler<GetParentModuleByIdQuery, ApiResponse<GetParentModuleResponseDTO>>
    {
        #region Fields
        private readonly ICommonRequestService _commonRequestService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetParentModuleByIdQueryHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetParentModuleByIdQueryHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">Provides the existing Module repository.</param>
        /// <param name="logger">Records unexpected processing failures.</param>
        /// <param name="commonRequestService">Validates the current Host Super Admin request.</param>
        public GetParentModuleByIdQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetParentModuleByIdQueryHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        #endregion

        #region MediatR Handler

        /// <summary>
        /// Retrieves a Header Module only when its identifier and requested scope both match.
        /// </summary>
        /// <param name="request">The read-only request.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>The matching Header Module response.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when the request does not have a valid Host principal.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when the scoped Header Module does not exist.</exception>
        public async Task<ApiResponse<GetParentModuleResponseDTO>> Handle(
            GetParentModuleByIdQuery request,
            CancellationToken cancellationToken)
        {
            await _commonRequestService.ValidateHostSuperAdminRequestAsync();

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
                var module = await _unitOfWork.ModuleRepository.GetParentModuleByIdAsync(
                    request.Id,
                    request.ModuleScope,
                    cancellationToken);

                if (module == null)
                {
                    throw new KeyNotFoundException("Parent Module was not found in the requested ModuleScope.");
                }

                return ApiResponse<GetParentModuleResponseDTO>.Success(module, "Parent Module retrieved successfully.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unable to retrieve Parent Module {ModuleId} in ModuleScope {ModuleScope}.", request.Id, request.ModuleScope);
                throw;
            }
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
