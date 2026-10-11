// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Creates, retrieves, updates, and soft deletes institution StaffEmploymentTypes.
// ================================================================

using AutoMapper;
using GetStaffEmploymentTypeResponseDTO = skolenza.application.DTOs.StaffType.GetStaffEmploymentTypeResponseDTO;
using skolenza.application.Constants;
using skolenza.application.DTOS.Staff.Type;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffEmploymentTypeCmd.Handlers;

#region Commands and Queries

/// <summary>
/// Represents a request to create a institution-owned StaffEmploymentType.
/// </summary>
public sealed class CreateStaffEmploymentTypeCommand : IRequest<ApiResponse<GetStaffEmploymentTypeResponseDTO>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateStaffEmploymentTypeCommand"/> class.
    /// </summary>
    /// <param name="dto">The StaffEmploymentType values to create.</param>
    public CreateStaffEmploymentTypeCommand(CreateStaffEmploymentTypeDTO dto)
    {
        DTO = dto;
    }

    /// <summary>
    /// Gets the StaffEmploymentType values to create.
    /// </summary>
    public CreateStaffEmploymentTypeDTO DTO { get; }
}

/// <summary>
/// Represents a request to retrieve institution-owned StaffEmploymentTypes.
/// </summary>
public sealed class GetStaffEmploymentTypesQuery : IRequest<ApiResponse<List<GetStaffEmploymentTypeResponseDTO>>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetStaffEmploymentTypesQuery"/> class.
    /// </summary>
    /// <param name="dto">The paging and permission request values.</param>
    public GetStaffEmploymentTypesQuery(GetStaffEmploymentTypeRequestDTO dto)
    {
        DTO = dto;
    }

    /// <summary>
    /// Gets the paging and permission request values.
    /// </summary>
    public GetStaffEmploymentTypeRequestDTO DTO { get; }
}

/// <summary>
/// Represents a request to update a institution-owned StaffEmploymentType.
/// </summary>
public sealed class UpdateStaffEmploymentTypeCommand : IRequest<ApiResponse<GetStaffEmploymentTypeResponseDTO>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateStaffEmploymentTypeCommand"/> class.
    /// </summary>
    /// <param name="dto">The StaffEmploymentType values to update.</param>
    public UpdateStaffEmploymentTypeCommand(UpdateStaffEmploymentTypeRequestDTO dto)
    {
        DTO = dto;
    }

    /// <summary>
    /// Gets the StaffEmploymentType values to update.
    /// </summary>
    public UpdateStaffEmploymentTypeRequestDTO DTO { get; }
}

/// <summary>
/// Represents a request to soft delete a institution-owned StaffEmploymentType.
/// </summary>
public sealed class DeleteStaffEmploymentTypeCommand : IRequest<ApiResponse<bool>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteStaffEmploymentTypeCommand"/> class.
    /// </summary>
    /// <param name="dto">The StaffEmploymentType deletion request.</param>
    public DeleteStaffEmploymentTypeCommand(DeleteStaffEmploymentTypeRequestDTO dto)
    {
        DTO = dto;
    }

    /// <summary>
    /// Gets the StaffEmploymentType deletion request.
    /// </summary>
    public DeleteStaffEmploymentTypeRequestDTO DTO { get; }
}

#endregion

#region Handlers

/// <summary>
/// Handles creation of institution-owned StaffEmploymentTypes.
/// </summary>
public sealed class CreateStaffEmploymentTypeCommandHandler : IRequestHandler<CreateStaffEmploymentTypeCommand, ApiResponse<GetStaffEmploymentTypeResponseDTO>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateStaffEmploymentTypeCommandHandler"/> class.
    /// </summary>
    public CreateStaffEmploymentTypeCommandHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService)
    {
        _unitOfWork = unitOfWork;
        _commonRequestService = commonRequestService;
    }

    #endregion

    #region Handle

    /// <summary>
    /// Creates one StaffEmploymentType for the authenticated Institution.
    /// </summary>
    public async Task<ApiResponse<GetStaffEmploymentTypeResponseDTO>> Handle(
        CreateStaffEmploymentTypeCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StaffEmploymentTypeHandlerRules.GetTrustedInstitutionActorAsync(_commonRequestService);
        var created = await _unitOfWork.StaffEmploymentTypeRepository.CreateAsync(
            actor.InstitutionId,
            actor.StaffMemberId,
            request.DTO,
            cancellationToken);

        return ApiResponse<GetStaffEmploymentTypeResponseDTO>.Success(created, "StaffEmploymentType created successfully.");
    }

    #endregion
}

/// <summary>
/// Handles paged retrieval of institution-owned StaffEmploymentTypes.
/// </summary>
public sealed class GetStaffEmploymentTypesQueryHandler : IRequestHandler<GetStaffEmploymentTypesQuery, ApiResponse<List<GetStaffEmploymentTypeResponseDTO>>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="GetStaffEmploymentTypesQueryHandler"/> class.
    /// </summary>
    public GetStaffEmploymentTypesQueryHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService)
    {
        _unitOfWork = unitOfWork;
        _commonRequestService = commonRequestService;
    }

    #endregion

    #region Handle

    /// <summary>
    /// Retrieves a paged StaffEmploymentType list for the authenticated Institution.
    /// </summary>
    public async Task<ApiResponse<List<GetStaffEmploymentTypeResponseDTO>>>
        Handle(GetStaffEmploymentTypesQuery request, CancellationToken cancellationToken)
    {
        if (request.DTO.PageNumber < 1 || request.DTO.PageSize is < 1 or > 100)
        {
            throw new ValidationErrorException("PageNumber must be positive; PageSize must be 1 to 100.");
        }

        var actor = await StaffEmploymentTypeHandlerRules.GetTrustedInstitutionActorAsync(_commonRequestService);
        var all = await _unitOfWork.StaffEmploymentTypeRepository.GetAllAsync(actor.InstitutionId, cancellationToken);
        var offset = (long)(request.DTO.PageNumber - 1) * request.DTO.PageSize;
        var page = offset >= all.Count
            ? new List<GetStaffEmploymentTypeResponseDTO>()
            : all.Skip((int)offset).Take(request.DTO.PageSize).ToList();
        var response = ApiResponse<List<GetStaffEmploymentTypeResponseDTO>>.Success(
            page,
            AppConstants.SuccessMessages.StaffEmploymentTypesRetrieved);

        response.PageNumber = request.DTO.PageNumber;
        response.PageSize = request.DTO.PageSize;
        response.TotalRecords = all.Count;
        response.TotalPages = (int)Math.Ceiling((double)all.Count / request.DTO.PageSize);
        return response;
    }

    #endregion
}

/// <summary>
/// Handles updates to institution-owned StaffEmploymentTypes.
/// </summary>
public sealed class UpdateStaffEmploymentTypeCommandHandler : IRequestHandler<UpdateStaffEmploymentTypeCommand, ApiResponse<GetStaffEmploymentTypeResponseDTO>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateStaffEmploymentTypeCommandHandler> _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateStaffEmploymentTypeCommandHandler"/> class.
    /// </summary>
    public UpdateStaffEmploymentTypeCommandHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService,
        IMapper mapper,
        ILogger<UpdateStaffEmploymentTypeCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _commonRequestService = commonRequestService;
        _mapper = mapper;
        _logger = logger;
    }

    #endregion

    #region Handle

    /// <summary>
    /// Updates an StaffEmploymentType after enforcing authenticated Institution ownership.
    /// </summary>
    public async Task<ApiResponse<GetStaffEmploymentTypeResponseDTO>> Handle(
        UpdateStaffEmploymentTypeCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DTO.Id <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        var actor = await StaffEmploymentTypeHandlerRules.GetTrustedInstitutionActorAsync(_commonRequestService);
        var typeName = StaffEmploymentTypeHandlerRules.ValidateAndNormalize(request.DTO);
        var staffEmploymentType = await _unitOfWork.StaffEmploymentTypeRepository.GetForUpdateAsync(
            actor.InstitutionId,
            request.DTO.Id,
            cancellationToken);

        if (staffEmploymentType is null)
        {
            throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);
        }

        if (await _unitOfWork.StaffEmploymentTypeRepository.NameExistsAsync(
                actor.InstitutionId,
                typeName,
                staffEmploymentType.Id,
                cancellationToken))
        {
            throw new ConflictException("StaffEmploymentType already exists in this institution.");
        }

        _mapper.Map(request.DTO, staffEmploymentType);
        staffEmploymentType.TypeName = typeName;
        staffEmploymentType.UpdatedById = actor.StaffMemberId;
        staffEmploymentType.UpdatedDateTime = DateTime.UtcNow;

        var updated = await _unitOfWork.StaffEmploymentTypeRepository.UpdateAsync(staffEmploymentType, cancellationToken);
        _logger.LogInformation(
            "StaffEmploymentType updated. StaffEmploymentTypeId: {StaffEmploymentTypeId}, InstitutionId: {InstitutionId}, StaffMemberId: {StaffMemberId}",
            staffEmploymentType.Id,
            actor.InstitutionId,
            actor.StaffMemberId);

        return ApiResponse<GetStaffEmploymentTypeResponseDTO>.Success(updated, "StaffEmploymentType updated successfully.");
    }

    #endregion
}

/// <summary>
/// Handles safe soft deletion of institution-owned StaffEmploymentTypes.
/// </summary>
public sealed class DeleteStaffEmploymentTypeCommandHandler : IRequestHandler<DeleteStaffEmploymentTypeCommand, ApiResponse<bool>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;
    private readonly ILogger<DeleteStaffEmploymentTypeCommandHandler> _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteStaffEmploymentTypeCommandHandler"/> class.
    /// </summary>
    public DeleteStaffEmploymentTypeCommandHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService,
        ILogger<DeleteStaffEmploymentTypeCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _commonRequestService = commonRequestService;
        _logger = logger;
    }

    #endregion

    #region Handle

    /// <summary>
    /// Soft deletes an StaffEmploymentType only when no protected dependency exists.
    /// </summary>
    public async Task<ApiResponse<bool>> Handle(
        DeleteStaffEmploymentTypeCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DTO.Id <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        var actor = await StaffEmploymentTypeHandlerRules.GetTrustedInstitutionActorAsync(_commonRequestService);
        var staffEmploymentType = await _unitOfWork.StaffEmploymentTypeRepository.GetForUpdateAsync(
            actor.InstitutionId,
            request.DTO.Id,
            cancellationToken);

        if (staffEmploymentType is null)
        {
            throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);
        }

        if (await _unitOfWork.StaffEmploymentTypeRepository.HasDeletionDependenciesAsync(
                actor.InstitutionId,
                staffEmploymentType.Id,
                cancellationToken))
        {
            _logger.LogWarning(
                "StaffEmploymentType deletion blocked by dependencies. StaffEmploymentTypeId: {StaffEmploymentTypeId}, InstitutionId: {InstitutionId}",
                staffEmploymentType.Id,
                actor.InstitutionId);
            throw new ConflictException(AppConstants.ErrorMessages.StaffEmploymentTypeHasDependencies);
        }

        staffEmploymentType.SoftDeletedById = actor.StaffMemberId;
        staffEmploymentType.SoftDeletedDateTime = DateTime.UtcNow;
        staffEmploymentType.UpdatedById = actor.StaffMemberId;
        staffEmploymentType.UpdatedDateTime = DateTime.UtcNow;

        await _unitOfWork.StaffEmploymentTypeRepository.SoftDeleteAsync(staffEmploymentType, cancellationToken);
        _logger.LogInformation(
            "StaffEmploymentType soft deleted. StaffEmploymentTypeId: {StaffEmploymentTypeId}, InstitutionId: {InstitutionId}, StaffMemberId: {StaffMemberId}",
            staffEmploymentType.Id,
            actor.InstitutionId,
            actor.StaffMemberId);

        return ApiResponse<bool>.Success(true, "StaffEmploymentType deleted successfully.");
    }

    #endregion
}

#endregion

#region Helpers

internal static class StaffEmploymentTypeHandlerRules
{
    /// <summary>
    /// Resolves the trusted Institution and actor identifiers from the established request validation flow.
    /// </summary>
    internal static async Task<(long InstitutionId, long StaffMemberId)> GetTrustedInstitutionActorAsync(
        ICommonRequestService commonRequestService)
    {
        var validation = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!validation.Success || validation.InstitutionId <= 0 || validation.LoggedInStaffMemberId <= 0 || validation.RoleId <= 0)
        {
            throw new UnauthorizedAccessException(
                validation.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
        }

        return (validation.InstitutionId, validation.LoggedInStaffMemberId);
    }

    /// <summary>
    /// Validates and normalizes the StaffEmploymentType fields shared by creation and update.
    /// </summary>
    internal static string ValidateAndNormalize(CreateStaffEmploymentTypeDTO request)
    {
        var name = request.TypeName?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 255 ||
            request.Description?.Length > 255 || request.Remark?.Length > 255)
        {
            throw new ValidationErrorException(
                "TypeName is required; type name, description and remark must not exceed 255 characters.");
        }

        return name;
    }
}

#endregion
