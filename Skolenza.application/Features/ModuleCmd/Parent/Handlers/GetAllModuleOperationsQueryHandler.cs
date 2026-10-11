// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the request to retrieve all ModuleOperation mappings.
// ================================================================

using AutoMapper;
using skolenza.application.DTOs.Module;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.ModuleCmd.Parent.Commands;

#region Query

/// <summary>
/// Represents the read-only request to retrieve all module-operation mappings.
/// </summary>
public class GetAllModuleOperationsQuery
    : IRequest<ApiResponse<List<ModuleOperationMappingByProductOwnerResponseDTO>>>
{
}

#endregion

#region Handler

/// <summary>
/// Handles Host-authorized retrieval of all module-operation mappings.
/// </summary>
public class GetAllModuleOperationsQueryHandler
    : IRequestHandler<
        GetAllModuleOperationsQuery,
        ApiResponse<List<ModuleOperationMappingByProductOwnerResponseDTO>>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAllModuleOperationsQueryHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides the existing module-operation mapping repository.</param>
    /// <param name="mapper">Maps persisted entities to response DTOs.</param>
    /// <param name="commonRequestService">Validates the current Host user request.</param>
    public GetAllModuleOperationsQueryHandler(
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
    /// Retrieves all mappings, returning an empty successful list when no mappings exist.
    /// </summary>
    /// <param name="request">The get-all-mappings request.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>All module-operation mappings.</returns>
    public async Task<ApiResponse<List<ModuleOperationMappingByProductOwnerResponseDTO>>> Handle(
        GetAllModuleOperationsQuery request,
        CancellationToken cancellationToken)
    {
        await _commonRequestService.ValidateHostUserRequestAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var entities = await _unitOfWork.ModuleRepository
            .GetAllModuleOperationMappingsAsync(cancellationToken);

        return ApiResponse<List<ModuleOperationMappingByProductOwnerResponseDTO>>.Success(
            _mapper.Map<List<ModuleOperationMappingByProductOwnerResponseDTO>>(entities),
            "Module operations retrieved successfully.");
    }

    #endregion
}

#endregion
