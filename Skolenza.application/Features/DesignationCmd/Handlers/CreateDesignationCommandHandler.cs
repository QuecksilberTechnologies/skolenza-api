// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Creates institution-scoped designations using trusted request context.
// ================================================================

using AutoMapper;
using skolenza.application.Constants;
using skolenza.application.DTOs.Designation;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.DesignationCmd.Handlers
{
    #region Command

    /// <summary>
    /// Represents a request to create a designation.
    /// </summary>
    public class CreateDesignationCommand : IRequest<ApiResponse<List<GetDesignationResponseDTO>>>
    {
        public CreateDesignationRequestDTO DTO { get; }

        /// <summary>
        /// Initializes the create request with client-editable values.
        /// </summary>
        public CreateDesignationCommand(CreateDesignationRequestDTO dto)
        {
            DTO = dto;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles creation of designations for the authenticated institution.
    /// </summary>
    public class CreateDesignationCommandHandler : IRequestHandler<CreateDesignationCommand, ApiResponse<List<GetDesignationResponseDTO>>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICommonRequestService _commonRequestService;
        private readonly ILogger<CreateDesignationCommandHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the handler dependencies.
        /// </summary>
        public CreateDesignationCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICommonRequestService commonRequestService,
            ILogger<CreateDesignationCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _commonRequestService = commonRequestService;
            _logger = logger;
        }

        #endregion

        #region Handle

        /// <summary>
        /// Creates a designation from client-editable values and trusted institution audit context.
        /// </summary>
        public async Task<ApiResponse<List<GetDesignationResponseDTO>>> Handle(
            CreateDesignationCommand request,
            CancellationToken cancellationToken)
        {
            #region Institution Request Validation

            var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
            if (!validation.Success)
            {
                throw new UnauthorizedAccessException(
                    validation.ErrorMessage ??
                    AppConstants.ErrorMessages.Unauthorized);
            }

            #endregion

            #region Trusted Request Context

            long userStaffMemberId = validation.LoggedInStaffMemberId;
            long institutionId = validation.InstitutionId;
            int tokenRoleId = validation.RoleId;

            if (userStaffMemberId <= 0 || institutionId <= 0 || tokenRoleId <= 0)
            {
                _logger.LogWarning(
                    "Invalid Institution authorization context while creating Designation. InstitutionId: {InstitutionId}, StaffMemberId: {StaffMemberId}, TokenRoleId: {TokenRoleId}",
                    institutionId,
                    userStaffMemberId,
                    tokenRoleId);

                throw new UnauthorizedAccessException(
                    AppConstants.ErrorMessages.Unauthorized);
            }

            #endregion

            if (request.DTO.DepartmentId <= 0)
                throw new ValidationErrorException(
                    AppConstants.ErrorMessages.InvalidIdentifier);

            var designationName = request.DTO.DesignationName?.Trim();
            if (string.IsNullOrWhiteSpace(designationName))
                throw new ValidationErrorException(
                    AppConstants.ErrorMessages.InvalidRequest);

            if (await _unitOfWork.DesignationRepository.CheckDuplicateValueAsync(
                    institutionId,
                    request.DTO.DepartmentId,
                    designationName,
                    cancellationToken: cancellationToken))
                throw new ConflictException(
                    AppConstants.ErrorMessages.ResourceConflict);

            // Map client-editable values to the domain entity.
            var entity = _mapper.Map<Designation>(request.DTO);
            entity.DesignationName = designationName;
            entity.InstitutionId = institutionId;
            entity.AddedById = userStaffMemberId;
            entity.AddedDateTime = DateTime.UtcNow;
            entity.IsSoftDeleted = false;

            // Persist the prepared domain entity.
            var created = await _unitOfWork.DesignationRepository.CreateAsync(entity, cancellationToken);
            if (created == null)
                throw new ConflictException(
                    AppConstants.ErrorMessages.ResourceConflict);

            _logger.LogInformation("Designation created. DesignationId: {DesignationId}", created.Id);
            return ApiResponse<List<GetDesignationResponseDTO>>.Success(
                new List<GetDesignationResponseDTO> { _mapper.Map<GetDesignationResponseDTO>(created) },
                "Designation created successfully.");
        }

        #endregion
    }

    #endregion
}
