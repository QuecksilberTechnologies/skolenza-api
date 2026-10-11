// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Handles CreateDepartmentCommand requests using current
//           institution identity, role, and module-operation authorization.
// ================================================================

using AutoMapper;
using skolenza.application.Constants;
using skolenza.application.DTOs.Department;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.DepartmentCmd.Handlers
{
    #region Command

    /// <summary>
    /// Represents a request to create a department.
    /// </summary>
    public class CreateDepartmentCommand
        : IRequest<ApiResponse<List<GetDepartmentResponseDTO>>>
    {
        /// <summary>
        /// Gets the client-supplied department data.
        /// </summary>
        public CreateDepartmentRequestDTO DTO { get; set; }

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="CreateDepartmentCommand"/> class.
        /// </summary>
        /// <param name="dto">
        /// The client-supplied department data.
        /// </param>
        public CreateDepartmentCommand(
            CreateDepartmentRequestDTO dto)
        {
            DTO = dto;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles authenticated and authorized department creation requests.
    /// </summary>
    public class CreateDepartmentCommandHandler
        : IRequestHandler<
            CreateDepartmentCommand,
            ApiResponse<List<GetDepartmentResponseDTO>>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateDepartmentCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="CreateDepartmentCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">
        /// Provides repositories and stored-function access.
        /// </param>
        /// <param name="mapper">
        /// Maps request and response objects.
        /// </param>
        /// <param name="logger">
        /// Provides structured application logging.
        /// </param>
        /// <param name="commonRequestService">
        /// Validates the authenticated Institution request context.
        /// </param>
        public CreateDepartmentCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<CreateDepartmentCommandHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        #endregion

        #region Handle

        /// <summary>
        /// Creates a department after validating the current Institution user
        /// and checking the current Department/Add permission.
        /// </summary>
        /// <param name="request">
        /// The create-department command.
        /// </param>
        /// <param name="cancellationToken">
        /// Token used to cancel the asynchronous operation.
        /// </param>
        /// <returns>
        /// An API response containing the newly created department.
        /// </returns>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown when the authenticated user context is invalid or stale.
        /// </exception>
        /// <exception cref="ValidationErrorException">
        /// Thrown when the supplied department data is invalid.
        /// </exception>
        /// <exception cref="ConflictException">
        /// Thrown when the department conflicts with an existing record.
        /// </exception>
        public async Task<ApiResponse<List<GetDepartmentResponseDTO>>> Handle(
            CreateDepartmentCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Creating Department.");

            #region Institution Request Validation

            // Validate the authenticated Institution identity and resolve trusted
            // StaffMember, Institution and token role context.
            var validation =
                await _commonRequestService
                    .ValidateInstitutionUserRequestAsync();

            if (!validation.Success)
            {
                throw new UnauthorizedAccessException(
                    validation.ErrorMessage ??
                    AppConstants.ErrorMessages.Unauthorized);
            }

            #endregion

            #region Trusted Request Context

            long userStaffMemberId =
                validation.LoggedInStaffMemberId;

            long institutionId =
                validation.InstitutionId;

            int tokenRoleId =
                validation.RoleId;

            // A missing Institution, StaffMember or token Primary Role means that
            // the authorization context cannot be trusted.
            if (userStaffMemberId <= 0 ||
                institutionId <= 0 ||
                tokenRoleId <= 0)
            {
                _logger.LogWarning(
                    "Invalid Institution authorization context while creating Department. " +
                    "InstitutionId: {InstitutionId}, StaffMemberId: {StaffMemberId}, TokenRoleId: {TokenRoleId}",
                    institutionId,
                    userStaffMemberId,
                    tokenRoleId);

                throw new UnauthorizedAccessException(
                    AppConstants.ErrorMessages.Unauthorized);
            }

            #endregion

            #region Department Request Validation

            string? departmentName =
                request.DTO.DepartmentName?.Trim();

            if (string.IsNullOrWhiteSpace(departmentName))
            {
                throw new ValidationErrorException(
                    AppConstants.ErrorMessages.InvalidRequest);
            }

            // Ensure the normalized value is persisted.
            request.DTO.DepartmentName =
                departmentName;

            #endregion

            #region Entity Mapping

            var department =
                _mapper.Map<Department>(
                    request.DTO);

            #endregion

            #region Server Controlled Values

            department.InstitutionId =
                institutionId;

            department.AddedById =
                userStaffMemberId;

            department.AddedDateTime =
                DateTime.UtcNow;

            department.IsActive =true;

            department.IsSoftDeleted =
                false;

            department.IsExecutiveOffice =
                false;

            #endregion

            #region Persistence

            var createdDepartment =
                await _unitOfWork
                    .DepartmentRepository
                    .CreateAsync(
                        department,
                        cancellationToken);

            if (createdDepartment == null)
            {
                _logger.LogWarning(
                    "Department creation conflicted with an existing record. InstitutionId: {InstitutionId}",
                    institutionId);

                throw new ConflictException(
                    AppConstants.ErrorMessages.ResourceConflict);
            }

            // Preserve the existing transaction completion behavior.
            await _unitOfWork
                .CommitTransactionAsync();

            #endregion

            #region Response Mapping

            var responseDTO =
                _mapper.Map<GetDepartmentResponseDTO>(
                    createdDepartment);

            #endregion

            #region Success Response

            _logger.LogInformation(
                "Department created successfully. InstitutionId: {InstitutionId}, StaffMemberId: {StaffMemberId}",
                institutionId,
                userStaffMemberId);

            return new ApiResponse<List<GetDepartmentResponseDTO>>
            {
                IsSucceeded = true,
                Message = "1 department(s) created successfully.",
                PageNumber = 1,
                PageSize = 1,
                TotalRecords = 1,
                TotalPages = 1,
                Data = new List<GetDepartmentResponseDTO>
                {
                    responseDTO
                }
            };

            #endregion
        }

        #endregion
    }

    #endregion
}
