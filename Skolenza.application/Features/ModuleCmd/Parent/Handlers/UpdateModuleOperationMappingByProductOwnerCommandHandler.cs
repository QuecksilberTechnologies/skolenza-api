// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the request to update a ModuleOperation mapping.
// ================================================================

using AutoMapper;
using skolenza.application.DTOs.Module;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.ModuleCmd.Parent.Commands;

#region Command

/// <summary>
/// Represents the request to update a module-operation mapping.
/// </summary>
public class UpdateModuleOperationMappingByProductOwnerCommand
    : IRequest<ApiResponse<ModuleOperationMappingByProductOwnerResponseDTO>>
{
    /// <summary>
    /// Gets the mapping values to update.
    /// </summary>
    public UpdateModuleOperationMappingByProductOwnerRequestDTO? DTO { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateModuleOperationMappingByProductOwnerCommand"/> class.
    /// </summary>
    /// <param name="dto">The client-editable mapping values.</param>
    public UpdateModuleOperationMappingByProductOwnerCommand(
        UpdateModuleOperationMappingByProductOwnerRequestDTO? dto)
    {
        DTO = dto;
    }
}

#endregion

#region Handler

/// <summary>
/// Handles Host-authorized updates to module-operation mappings.
/// </summary>
public class UpdateModuleOperationMappingByProductOwnerCommandHandler
    : IRequestHandler<
        UpdateModuleOperationMappingByProductOwnerCommand,
        ApiResponse<ModuleOperationMappingByProductOwnerResponseDTO>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateModuleOperationMappingByProductOwnerCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides the existing module-operation mapping repository.</param>
    /// <param name="mapper">Maps client-editable values into the existing entity.</param>
    /// <param name="commonRequestService">Validates the current Host user request.</param>
    public UpdateModuleOperationMappingByProductOwnerCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICommonRequestService commonRequestService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _commonRequestService = commonRequestService;
    }

    #endregion

    #region Handle

    /// <summary>
    /// Updates a mapping while preserving its create audit fields.
    /// </summary>
    /// <param name="request">The mapping update request.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The updated module-operation mapping.</returns>
    public async Task<ApiResponse<ModuleOperationMappingByProductOwnerResponseDTO>> Handle(
        UpdateModuleOperationMappingByProductOwnerCommand request,
        CancellationToken cancellationToken)
    {
        var hostContext = await _commonRequestService.ValidateHostSuperAdminRequestAsync();
        var hostUserId = hostContext.HostUserId;
        
        cancellationToken.ThrowIfCancellationRequested();

        var dto = request?.DTO
            ?? throw new ValidationErrorException("Module operation mapping details are required.");

        var mappingId = ResolveMappingId(dto);
        ValidateMappingValues(mappingId, dto.ModuleId, dto.OperationId, dto.DataViewStructureId, dto.PageTypeId);

        var existing = await _unitOfWork.ModuleRepository
            .GetModuleOperationMappingByIdAsync(mappingId, cancellationToken)
            ?? throw new ApiException("Module operation mapping not found.", 404);

        if (dto.IsActive == true && existing.IsActive != true)
        {
            await ValidateModuleHierarchyForOperationAsync(dto.ModuleId, cancellationToken);
        }

        _mapper.Map(dto, existing);
        var utcNow = DateTime.UtcNow;
        existing.UpdatedById = hostUserId;
        existing.UpdatedDateTime = utcNow;

        var updated = await _unitOfWork.ModuleRepository
            .UpdateModuleOperationMappingAsync(existing, cancellationToken);

        var responseEntity = await _unitOfWork.ModuleRepository
            .GetModuleOperationMappingByIdAsync(updated.Id, cancellationToken)
            ?? throw new ApiException("Module operation mapping not found.", 404);

        return ApiResponse<ModuleOperationMappingByProductOwnerResponseDTO>.Success(
            _mapper.Map<ModuleOperationMappingByProductOwnerResponseDTO>(responseEntity),
            "Module operation updated successfully.");
    }

    #endregion

    #region Validation

    /// <summary>
    /// Resolves the supported current or legacy mapping identifier.
    /// </summary>
    /// <param name="dto">The mapping update values.</param>
    /// <returns>The mapping identifier.</returns>
    /// <exception cref="ValidationErrorException">Thrown when the identifiers conflict or are invalid.</exception>
    private static int ResolveMappingId(UpdateModuleOperationMappingByProductOwnerRequestDTO dto)
    {
        if (dto.Id > 0 && dto.ModuleOperationMappingId.HasValue &&
            dto.ModuleOperationMappingId.Value != dto.Id)
        {
            throw new ValidationErrorException("Module operation mapping identifiers do not match.");
        }

        return dto.Id > 0 ? dto.Id : dto.ModuleOperationMappingId.GetValueOrDefault();
    }

    /// <summary>
    /// Validates mapping identifiers against the entity's required and optional key fields.
    /// </summary>
    /// <param name="mappingId">The mapping identifier.</param>
    /// <param name="moduleId">The related module identifier.</param>
    /// <param name="operationId">The related operation identifier.</param>
    /// <param name="dataViewStructureId">The optional data-view structure identifier.</param>
    /// <param name="pageTypeId">The optional page-type identifier.</param>
    private static void ValidateMappingValues(
        int mappingId,
        int moduleId,
        int operationId,
        int? dataViewStructureId,
        int? pageTypeId)
    {
        if (mappingId <= 0 || moduleId <= 0 || operationId <= 0)
        {
            throw new ValidationErrorException("Valid mapping, module, and operation IDs are required.");
        }

        if (dataViewStructureId is <= 0 || pageTypeId is <= 0)
        {
            throw new ValidationErrorException("Optional related IDs must be positive when supplied.");
        }
    }

    /// <summary>
    /// Validates that a mapping's exact owning Module and every ancestor are active before the mapping is activated.
    /// </summary>
    /// <param name="moduleId">The exact owning Module identifier.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    private async Task ValidateModuleHierarchyForOperationAsync(
        int moduleId,
        CancellationToken cancellationToken)
    {
        var hierarchy = await _unitOfWork.ModuleRepository
            .GetModuleHierarchyForOperationActivationAsync(moduleId, cancellationToken);

        if (hierarchy is null || hierarchy.Count == 0)
        {
            throw new ValidationErrorException(
                "The module operation cannot be activated because its module is deleted or unavailable.");
        }

        // A module operation can be active only when its exact child Module and complete ancestor hierarchy are active.
        var childModule = hierarchy[0];
        if (!childModule.IsActive)
        {
            throw new ValidationErrorException(
                $"The module operation cannot be activated because the child module '{childModule.ModuleName}' is inactive.");
        }

        for (var hierarchyIndex = 1; hierarchyIndex < hierarchy.Count; hierarchyIndex++)
        {
            var parentModule = hierarchy[hierarchyIndex];
            if (!parentModule.IsActive)
            {
                throw new ValidationErrorException(
                    $"The module operation cannot be activated because the parent module '{parentModule.ModuleName}' is inactive.");
            }
        }
    }

    #endregion
}

#endregion
