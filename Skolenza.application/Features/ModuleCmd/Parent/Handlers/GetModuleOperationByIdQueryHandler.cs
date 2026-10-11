// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the request to retrieve a ModuleOperation mapping by ID.
// ================================================================

using AutoMapper;
using skolenza.application.DTOs.Module;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.ModuleCmd.Parent.Commands;

#region Query

/// <summary>
/// Represents the read-only request to retrieve a module-operation mapping by ID.
/// </summary>
public class GetModuleOperationByIdQuery
    : IRequest<ApiResponse<ModuleOperationMappingByProductOwnerResponseDTO>>
{
    /// <summary>
    /// Gets the mapping identifier to retrieve.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetModuleOperationByIdQuery"/> class.
    /// </summary>
    /// <param name="id">The mapping identifier to retrieve.</param>
    public GetModuleOperationByIdQuery(int id)
    {
        Id = id;
    }
}

#endregion

#region Handler

/// <summary>
/// Handles Host-authorized retrieval of a module-operation mapping by ID.
/// </summary>
public class GetModuleOperationByIdQueryHandler
    : IRequestHandler<
        GetModuleOperationByIdQuery,
        ApiResponse<ModuleOperationMappingByProductOwnerResponseDTO>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="GetModuleOperationByIdQueryHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides the existing module-operation mapping repository.</param>
    /// <param name="mapper">Maps the persisted entity to its response DTO.</param>
    /// <param name="commonRequestService">Validates the current Host user request.</param>
    public GetModuleOperationByIdQueryHandler(
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
    /// Retrieves the requested module-operation mapping.
    /// </summary>
    /// <param name="request">The mapping lookup request.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The requested module-operation mapping.</returns>
    public async Task<ApiResponse<ModuleOperationMappingByProductOwnerResponseDTO>> Handle(
        GetModuleOperationByIdQuery request,
        CancellationToken cancellationToken)
    {
        await _commonRequestService.ValidateHostUserRequestAsync();
        cancellationToken.ThrowIfCancellationRequested();

        if (request is null || request.Id <= 0)
        {
            throw new ValidationErrorException("A valid module operation mapping ID is required.");
        }

        var entity = await _unitOfWork.ModuleRepository
            .GetModuleOperationMappingByIdAsync(request.Id, cancellationToken)
            ?? throw new ApiException("Module operation mapping not found.", 404);

        return ApiResponse<ModuleOperationMappingByProductOwnerResponseDTO>.Success(
            _mapper.Map<ModuleOperationMappingByProductOwnerResponseDTO>(entity),
            "Module operation retrieved successfully.");
    }

    #endregion
}

#endregion
