// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves institution-scoped department projections using trusted context.
// ================================================================

using skolenza.application.DTOs.Department;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.DepartmentCmd.Handlers
{
    #region Query

    /// <summary>
    /// Represents a request to retrieve departments.
    /// </summary>
    public class GetDepartmentQuery : IRequest<ApiResponse<List<GetDepartmentResponseDTO>>>
    {
        public GetDepartmentRequestDTO DTO { get; }

        /// <summary>
        /// Initializes the query with client-supplied filters.
        /// </summary>
        public GetDepartmentQuery(GetDepartmentRequestDTO dto)
        {
            DTO = dto;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles department listing requests for the authenticated institution.
    /// </summary>
    public class GetDepartmentQueryHandler : IRequestHandler<GetDepartmentQuery, ApiResponse<List<GetDepartmentResponseDTO>>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonRequestService _commonRequestService;
        private readonly ILogger<GetDepartmentQueryHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the handler dependencies.
        /// </summary>
        public GetDepartmentQueryHandler(
            IUnitOfWork unitOfWork,
            ICommonRequestService commonRequestService,
            ILogger<GetDepartmentQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _commonRequestService = commonRequestService;
            _logger = logger;
        }

        #endregion

        #region Handle

        /// <summary>
        /// Retrieves paged departments without mutating the request DTO with server context.
        /// </summary>
        public async Task<ApiResponse<List<GetDepartmentResponseDTO>>> Handle(
            GetDepartmentQuery request,
            CancellationToken cancellationToken)
        {
            var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
            if (!validation.Success)
                throw new UnauthorizedAccessException(validation.ErrorMessage);

            var response = await _unitOfWork.DepartmentRepository.GetAsync(
                request.DTO,
                validation.InstitutionId,
                cancellationToken);

            var departments = response.Data ?? new List<GetDepartmentResponseDTO>();
            _logger.LogInformation(
                "Retrieved {Count} departments for InstitutionId: {InstitutionId}",
                departments.Count,
                validation.InstitutionId);

            return new ApiResponse<List<GetDepartmentResponseDTO>>
            {
                IsSucceeded = true,
                Message = departments.Count == 0
                    ? "No data found."
                    : $"{response.TotalCount} record(s) retrieved successfully.",
                PageNumber = response.PageNumber,
                PageSize = response.PageSize,
                TotalRecords = response.TotalCount,
                TotalPages = response.TotalPages,
                Data = departments
            };
        }

        #endregion
    }

    #endregion
}
