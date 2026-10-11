// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the request to create a ModuleOperation mapping.
// ================================================================

using AutoMapper;
using skolenza.application.DTOs.Module;
using skolenza.application.DTOs.ModuleOperation;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;

namespace skolenza.application.Features.ModuleCmd.Parent.Commands;

#region Command

/// <summary>
/// Represents the request to create a module-operation mapping.
/// </summary>
public class CreateModuleOperationCommand
    : IRequest<ApiResponse<ModuleOperationMappingByProductOwnerResponseDTO>>
{
    /// <summary>
    /// Gets the mapping values to create.
    /// </summary>
    public CreateModuleOperationRequestDTO? DTO { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateModuleOperationCommand"/> class.
    /// </summary>
    /// <param name="dto">The client-editable mapping values.</param>
    public CreateModuleOperationCommand(CreateModuleOperationRequestDTO? dto)
    {
        DTO = dto;
    }
}

#endregion

#region Handler

/// <summary>
/// Handles Host-authorized creation of module-operation mappings.
/// </summary>
public class CreateModuleOperationCommandHandler
    : IRequestHandler<
        CreateModuleOperationCommand,
        ApiResponse<ModuleOperationMappingByProductOwnerResponseDTO>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateModuleOperationCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides the existing module-operation mapping repository.</param>
    /// <param name="mapper">Maps client-editable values into the mapping entity.</param>
    /// <param name="commonRequestService">Validates the current Host user request.</param>
    public CreateModuleOperationCommandHandler(
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
    /// Creates a mapping and assigns its create audit values from the authenticated Host user.
    /// </summary>
    /// <param name="request">The mapping creation request.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The created module-operation mapping.</returns>
    public async Task<ApiResponse<ModuleOperationMappingByProductOwnerResponseDTO>> Handle(
        CreateModuleOperationCommand request,
        CancellationToken cancellationToken)
    {
        var hostContext = await _commonRequestService.ValidateHostSuperAdminRequestAsync();
        var hostUserId = hostContext.HostUserId;
        cancellationToken.ThrowIfCancellationRequested();

        var dto = request?.DTO
            ?? throw new ValidationErrorException("Module operation mapping details are required.");

        ValidateMappingValues(dto.ModuleId, dto.OperationId, dto.DataViewStructureId, dto.PageTypeId);
        await ValidateModuleHierarchyForOperationAsync(dto.ModuleId, cancellationToken);

        var utcNow = DateTime.UtcNow;
        var entity = _mapper.Map<ModuleOperationMapping>(dto);
        entity.AddedById = hostUserId;
        entity.AddedDateTime = utcNow;
        entity.UpdatedById = null;
        entity.UpdatedDateTime = null;

        var created = await _unitOfWork.ModuleRepository
            .CreateModuleOperationMappingAsync(entity, cancellationToken);

        var responseEntity = await _unitOfWork.ModuleRepository
            .GetModuleOperationMappingByIdAsync(created.Id, cancellationToken)
            ?? throw new ApiException("Module operation mapping was not created.", 500);

        return ApiResponse<ModuleOperationMappingByProductOwnerResponseDTO>.Success(
            _mapper.Map<ModuleOperationMappingByProductOwnerResponseDTO>(responseEntity),
            "Module operation created successfully.");
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validates mapping identifiers against the entity's required and optional key fields.
    /// </summary>
    /// <param name="moduleId">The related module identifier.</param>
    /// <param name="operationId">The related operation identifier.</param>
    /// <param name="dataViewStructureId">The optional data-view structure identifier.</param>
    /// <param name="pageTypeId">The optional page-type identifier.</param>
    private static void ValidateMappingValues(
        int moduleId,
        int operationId,
        int? dataViewStructureId,
        int? pageTypeId)
    {
        if (moduleId <= 0 || operationId <= 0)
        {
            throw new ValidationErrorException("Valid module and operation IDs are required.");
        }

        if (dataViewStructureId is <= 0 || pageTypeId is <= 0)
        {
            throw new ValidationErrorException("Optional related IDs must be positive when supplied.");
        }
    }

    /// <summary>
    /// Validates that an operation's owning Module and every ancestor are active before a mapping is created.
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
                "The operation cannot be created or activated because its module is deleted or unavailable.");
        }

        // An operation can be active only when its owning Module and complete ancestor hierarchy are active.
        var inactiveModule = hierarchy.FirstOrDefault(module => !module.IsActive);
        if (inactiveModule is not null)
        {
            throw new ValidationErrorException(
                $"The operation cannot be created or activated because the module '{inactiveModule.ModuleName}' is inactive.");
        }
    }

    #endregion
}

#endregion
