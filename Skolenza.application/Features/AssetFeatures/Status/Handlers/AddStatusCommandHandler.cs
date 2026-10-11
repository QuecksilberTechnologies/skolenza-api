// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Creates institution-owned asset statuses from authenticated requests.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOS.AssetDTO.status;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.AssetFeatures.Status.Handlers;

#region Command

/// <summary>
/// Represents the request to create an asset status.
/// </summary>
public class AddStatusCommand : IRequest<ApiResponse<GetStatusResponseDTO>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddStatusCommand"/> class.
    /// </summary>
    public AddStatusCommand(CreateStatusRequestDTO dto)
    {
        DTO = dto;
    }

    /// <summary>
    /// Gets the client-supplied asset status values.
    /// </summary>
    public CreateStatusRequestDTO DTO { get; }
}

#endregion

#region Handler

/// <summary>
/// Handles creation of institution-owned asset statuses.
/// </summary>
public class AddStatusCommandHandler : IRequestHandler<AddStatusCommand, ApiResponse<GetStatusResponseDTO>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICommonRequestService _commonRequestService;
    private readonly ILogger<AddStatusCommandHandler> _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="AddStatusCommandHandler"/> class.
    /// </summary>
    public AddStatusCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICommonRequestService commonRequestService,
        ILogger<AddStatusCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _commonRequestService = commonRequestService;
        _logger = logger;
    }

    #endregion

    #region Handle

    /// <inheritdoc />
    public async Task<ApiResponse<GetStatusResponseDTO>> Handle(
        AddStatusCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DTO is null)
        {
            throw new ValidationErrorException(
                "Invalid request data.",
                new List<string> { "Request DTO is required." });
        }

        if (string.IsNullOrWhiteSpace(request.DTO.StatusName)
            || string.IsNullOrWhiteSpace(request.DTO.ColorKey))
        {
            throw new ValidationErrorException(
                "StatusName and ColorKey are required.",
                new List<string> { "StatusName and ColorKey cannot be empty." });
        }

        #region Institution Request Validation

        var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!validation.Success)
        {
            throw new UnauthorizedAccessException(
                validation.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
        }

        #endregion

        #region Trusted Request Context

        long userStaffMemberId = validation.LoggedInStaffMemberId;
        long institutionId = validation.InstitutionId;
        int tokenRoleId = validation.RoleId;

        if (userStaffMemberId <= 0 || institutionId <= 0 || tokenRoleId <= 0)
        {
            _logger.LogWarning(
                "Invalid Institution authorization context while creating Asset Status. InstitutionId: {InstitutionId}, StaffMemberId: {StaffMemberId}, TokenRoleId: {TokenRoleId}",
                institutionId, userStaffMemberId, tokenRoleId);
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        #endregion

        #region Runtime Permission Validation

        var permissionResult = await _unitOfWork.StoreProcedureRepository
            .CheckInstitutionStaffMemberPermissionAsync(
                institutionId,
                userStaffMemberId,
                tokenRoleId,
                request.DTO.ModuleId,
                request.DTO.OperationId,
                cancellationToken);

        InstitutionRuntimePermissionValidator.EnsureAllowed(permissionResult);

        #endregion

        // Map client-editable values and apply server-controlled context.
        var entity = _mapper.Map<AssetStatus>(request.DTO);
        entity.InstitutionId = institutionId;
        entity.IsActive = true;
        entity.IsSoftDeleted = false;
        entity.AddedById = userStaffMemberId;
        entity.AddedDateTime = DateTime.UtcNow;

        var createdEntity = await _unitOfWork.AssetStatusRepository.CreateAsync(entity, cancellationToken);
        if (createdEntity is null)
        {
            throw new ApiException("Failed to create asset status.", 500);
        }

        return ApiResponse<GetStatusResponseDTO>.Success(
            _mapper.Map<GetStatusResponseDTO>(createdEntity),
            "Asset Status created successfully.");
    }

    #endregion
}

#endregion
