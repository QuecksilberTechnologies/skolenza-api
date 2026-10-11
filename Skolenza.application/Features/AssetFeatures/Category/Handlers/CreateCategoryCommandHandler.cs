// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Creates institution-owned asset categories from authenticated requests.
// ================================================================

using AutoMapper;
using skolenza.application.DTOS.AssetDTO.category;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;

namespace skolenza.application.Features.AssetFeatures.Category.Handlers;

#region Command

/// <summary>Represents the request to create an asset category.</summary>
public class AddCategoryCommand : IRequest<ApiResponse<GetCategoryResponseDTO>>
{
    /// <summary>Initializes a new instance of the <see cref="AddCategoryCommand"/> class.</summary>
    public AddCategoryCommand(AddCategoryReqestDTO dto) => DTO = dto;

    /// <summary>Gets the client-supplied asset category values.</summary>
    public AddCategoryReqestDTO DTO { get; }
}

#endregion

#region Handler

/// <summary>Handles creation of institution-owned asset categories.</summary>
public class CreateCategoryCommandHandler
    : IRequestHandler<AddCategoryCommand, ApiResponse<GetCategoryResponseDTO>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>Initializes a new instance of the <see cref="CreateCategoryCommandHandler"/> class.</summary>
    public CreateCategoryCommandHandler(
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

    /// <inheritdoc />
    public async Task<ApiResponse<GetCategoryResponseDTO>> Handle(
        AddCategoryCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DTO is null || string.IsNullOrWhiteSpace(request.DTO.CategoryName))
        {
            throw new ValidationErrorException(
                "Category name is required.",
                new List<string> { "CategoryName cannot be empty." });
        }

        // Resolve the trusted institution-user context.
        var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!validation.Success)
        {
            throw new UnauthorizedAccessException(validation.ErrorMessage);
        }

        // Map client-editable values and apply server-controlled context.
        var entity = _mapper.Map<AssetCategory>(request.DTO);
        entity.InstitutionId = validation.InstitutionId;
        entity.AddedById = validation.LoggedInStaffMemberId;
        entity.AddedDateTime = DateTime.UtcNow;
        entity.IsActive = true;
        entity.IsSoftDeleted = false;

        var createdEntity = await _unitOfWork.AssetCategoryRepository.CreateAsync(entity, cancellationToken);
        if (createdEntity is null)
        {
            throw new ApiException("Category creation failed.", 500);
        }

        return ApiResponse<GetCategoryResponseDTO>.Success(
            _mapper.Map<GetCategoryResponseDTO>(createdEntity),
            "Asset category created successfully.");
    }

    #endregion
}

#endregion
