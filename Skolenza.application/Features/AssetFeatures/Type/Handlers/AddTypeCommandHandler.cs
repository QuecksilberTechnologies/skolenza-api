// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Creates institution-owned asset types from authenticated requests.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOS.AssetDTO.type;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.AssetFeatures.Type.Handlers;

#region Command

/// <summary>
/// Represents the request to create an asset type.
/// </summary>
public class AddTypeCommand : IRequest<ApiResponse<List<GetTypeResponseDTO>>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddTypeCommand"/> class.
    /// </summary>
    /// <param name="dto">The client-supplied asset type values.</param>
    public AddTypeCommand(AddTypeRequestDTO dto)
    {
        DTO = dto;
    }

    /// <summary>
    /// Gets the client-supplied asset type values.
    /// </summary>
    public AddTypeRequestDTO DTO { get; }
}

#endregion

#region Handler

/// <summary>
/// Handles creation of institution-owned asset types.
/// </summary>
public class AddTypeCommandHandler : IRequestHandler<AddTypeCommand, ApiResponse<List<GetTypeResponseDTO>>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<AddTypeCommandHandler> _logger;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="AddTypeCommandHandler"/> class.
    /// </summary>
    public AddTypeCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<AddTypeCommandHandler> logger,
        ICommonRequestService commonRequestService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _commonRequestService = commonRequestService;
    }

    #endregion

    #region Handle

    /// <inheritdoc />
    public async Task<ApiResponse<List<GetTypeResponseDTO>>> Handle(
        AddTypeCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DTO is null)
        {
            throw new ValidationErrorException(
                "Invalid request data.",
                new List<string> { "Request DTO is required." });
        }

        if (string.IsNullOrWhiteSpace(request.DTO.TypeName))
        {
            throw new ValidationErrorException(
                "Type Name is required.",
                new List<string> { "TypeName cannot be empty." });
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
                "Invalid Institution authorization context while creating Asset Type. InstitutionId: {InstitutionId}, StaffMemberId: {StaffMemberId}, TokenRoleId: {TokenRoleId}",
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
        var entity = _mapper.Map<AssetType>(request.DTO);
        entity.InstitutionId = institutionId;
        entity.AddedById = userStaffMemberId;
        entity.AddedDateTime = DateTime.UtcNow;
        entity.IsSoftDeleted = false;

        var createdEntity = await _unitOfWork.AssetTypeRepository.CreateAsync(entity, cancellationToken);
        if (createdEntity is null)
        {
            _logger.LogWarning("Asset type creation returned no entity for institution {InstitutionId}.", institutionId);
            throw new ApiException("Failed to add asset type.", 500);
        }

        var response = _mapper.Map<GetTypeResponseDTO>(createdEntity);
        return ApiResponse<List<GetTypeResponseDTO>>.Success(
            new List<GetTypeResponseDTO> { response },
            "Asset Type added successfully.");
    }

    #endregion
}

#endregion
