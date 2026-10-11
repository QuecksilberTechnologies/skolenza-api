// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves a single department through centralized error handling.
// ================================================================

using skolenza.application.DTOs.Department;
using skolenza.application.DTOS.Department;
using skolenza.application.Constants;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.DepartmentCmd.Handlers
{
    #region Query

    /// <summary>
    /// Represents a request to retrieve one department by identifier.
    /// </summary>
    public class GetDepartmentByIdQuery : IRequest<ApiResponse<GetSingleDepartmentResponseDTO>>
    {
        /// <summary>
        /// Gets the department identifier request.
        /// </summary>
        public GetSingleDepartmentRequestDTO Dto { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetDepartmentByIdQuery"/> class.
        /// </summary>
        /// <param name="dto">The department identifier request.</param>
        public GetDepartmentByIdQuery(GetSingleDepartmentRequestDTO dto)
        {
            Dto = dto;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles retrieval of a single department.
    /// </summary>
    public class GetDepartmentByIdQueryHandled : IRequestHandler<GetDepartmentByIdQuery, ApiResponse<GetSingleDepartmentResponseDTO>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonRequestService _commonRequestService;
        private readonly ILogger<GetDepartmentByIdQueryHandled> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetDepartmentByIdQueryHandled"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work used to access persistence.</param>
        /// <param name="logger">The logger used for contextual success and not-found diagnostics.</param>
        public GetDepartmentByIdQueryHandled(
            IUnitOfWork unitOfWork,
            ICommonRequestService commonRequestService,
            ILogger<GetDepartmentByIdQueryHandled> logger)
        {
            _unitOfWork = unitOfWork;
            _commonRequestService = commonRequestService;
            _logger = logger;
        }

        #endregion

        #region Handle

        /// <summary>
        /// Retrieves the requested department or delegates its error response to middleware.
        /// </summary>
        /// <param name="request">The single-department query.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>The successful department response.</returns>
        public async Task<ApiResponse<GetSingleDepartmentResponseDTO>> Handle(
            GetDepartmentByIdQuery request,
            CancellationToken cancellationToken)
        {
            if (request?.Dto == null || request.Dto.Id <= 0)
            {
                throw new skolenza.application.Exceptions.ValidationErrorException(
                    skolenza.application.Constants.AppConstants.ErrorMessages.InvalidIdentifier);
            }

            var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
            if (!validation.Success || validation.InstitutionId <= 0)
            {
                throw new UnauthorizedAccessException(
                    validation.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
            }

            var department = await _unitOfWork.DepartmentRepository.GetByIdAsync(
                request.Dto,
                validation.InstitutionId,
                cancellationToken);

            if (department == null)
            {
                _logger.LogWarning("No department found with the given ID: {DepartmentId}", request.Dto.Id);
                throw new skolenza.application.Exceptions.NotFoundException(
                    skolenza.application.Constants.AppConstants.ErrorMessages.ResourceNotFound);
            }

            _logger.LogInformation("Successfully retrieved department with ID: {DepartmentId}", request.Dto.Id);
            return ApiResponse<GetSingleDepartmentResponseDTO>.Success(
                department,
                "Department fetched successfully.");
        }

        #endregion
    }

    #endregion
}
