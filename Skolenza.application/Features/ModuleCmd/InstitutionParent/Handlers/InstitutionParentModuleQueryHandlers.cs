// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Handles Host Super Admin queries for Institution-enabled Parent and Sub-Parent Header Modules.
// ================================================================

using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOS.Module.InstitutionParentModule;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.ModuleCmd.InstitutionParent.Queries;

#region Queries

/// <summary>
/// Represents the Host-managed request to retrieve a Institution's entitled Header Module tree.
/// </summary>
public sealed class GetInstitutionParentModuleHeadersQuery
    : IRequest<ApiResponse<List<InstitutionParentModuleResponseDTO>>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetInstitutionParentModuleHeadersQuery"/> class.
    /// </summary>
    /// <param name="requestDTO">The encrypted Institution and Header Module filters.</param>
    public GetInstitutionParentModuleHeadersQuery(InstitutionParentModuleHeaderRequestDTO? requestDTO)
    {
        RequestDTO = requestDTO;
    }

    /// <summary>Gets the encrypted Institution and Header Module filters.</summary>
    public InstitutionParentModuleHeaderRequestDTO? RequestDTO { get; }
}

/// <summary>
/// Represents the Host-managed request for the Institution Parent Module list endpoint.
/// </summary>
public sealed class GetInstitutionParentModulesQuery
    : IRequest<ApiResponse<List<InstitutionParentModuleResponseDTO>>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetInstitutionParentModulesQuery"/> class.
    /// </summary>
    /// <param name="requestDTO">The minimal list filter and paging values.</param>
    public GetInstitutionParentModulesQuery(InstitutionParentModuleListRequestDTO? requestDTO)
    {
        RequestDTO = requestDTO;
    }

    /// <summary>Gets the minimal list filter and paging values.</summary>
    public InstitutionParentModuleListRequestDTO? RequestDTO { get; }
}

/// <summary>
/// Represents the Host-managed request to retrieve one Institution-entitled Header Module by global Module identifier.
/// </summary>
public sealed class GetInstitutionParentModuleByIdQuery
    : IRequest<ApiResponse<InstitutionParentModuleResponseDTO>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetInstitutionParentModuleByIdQuery"/> class.
    /// </summary>
    /// <param name="moduleId">The global Module identifier exposed by the API.</param>
    /// <param name="requestDTO">The encrypted Institution and Module-scope filters.</param>
    public GetInstitutionParentModuleByIdQuery(int moduleId, InstitutionParentModuleByIdRequestDTO? requestDTO)
    {
        ModuleId = moduleId;
        RequestDTO = requestDTO;
    }

    /// <summary>Gets the global Module identifier exposed by the API.</summary>
    public int ModuleId { get; }

    /// <summary>Gets the encrypted Institution and Module-scope filters.</summary>
    public InstitutionParentModuleByIdRequestDTO? RequestDTO { get; }
}

#endregion

#region Handlers

/// <summary>
/// Retrieves Institution-entitled Header Module trees for an authenticated Host Super Admin.
/// </summary>
public sealed class GetInstitutionParentModuleHeadersQueryHandler
    : InstitutionParentModuleQueryHandlerBase,
      IRequestHandler<GetInstitutionParentModuleHeadersQuery, ApiResponse<List<InstitutionParentModuleResponseDTO>>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetInstitutionParentModuleHeadersQueryHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides Institution entitlement and Institution persistence operations.</param>
    /// <param name="commonRequestService">Validates the current Host Super Admin and Host encryption-key context.</param>
    /// <param name="idEncoderService">Encodes Institution identifiers at the Host API boundary.</param>
    public GetInstitutionParentModuleHeadersQueryHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService,
        IIdEncoderService idEncoderService)
        : base(unitOfWork, commonRequestService, idEncoderService)
    {
    }

    /// <inheritdoc />
    public async Task<ApiResponse<List<InstitutionParentModuleResponseDTO>>> Handle(
        GetInstitutionParentModuleHeadersQuery request,
        CancellationToken cancellationToken)
    {
        var dto = request?.RequestDTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
       
        var context = await ResolveInstitutionRequestAsync(dto.InstitutionId, cancellationToken);
        var headers = await UnitOfWork.InstitutionParentModuleRepository.GetHeaderTreeAsync(
            context.InstitutionId,
            dto.IsEnabled,
            cancellationToken);

        return ApiResponse<List<InstitutionParentModuleResponseDTO>>.Success(
            headers.Select(header => MapResponse(header, context.InstitutionEncryptionKey)).ToList(),
            "Institution module headers retrieved successfully.");
    }
}

/// <summary>
/// Retrieves a paged all-Institution list of Main Parent Headers for an authenticated Host Super Admin.
/// </summary>
public sealed class GetInstitutionParentModulesQueryHandler
    : InstitutionParentModuleQueryHandlerBase,
      IRequestHandler<GetInstitutionParentModulesQuery, ApiResponse<List<InstitutionParentModuleResponseDTO>>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetInstitutionParentModulesQueryHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides Institution entitlement and Institution persistence operations.</param>
    /// <param name="commonRequestService">Validates the current Host Super Admin and Host encryption-key context.</param>
    /// <param name="idEncoderService">Encodes Institution identifiers at the Host API boundary.</param>
    public GetInstitutionParentModulesQueryHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService,
        IIdEncoderService idEncoderService)
        : base(unitOfWork, commonRequestService, idEncoderService)
    {
    }

    /// <inheritdoc />
    public async Task<ApiResponse<List<InstitutionParentModuleResponseDTO>>> Handle(
        GetInstitutionParentModulesQuery request,
        CancellationToken cancellationToken)
    {
        await ValidateHostSuperAdminRequestAsync();
        var institutionEncryptionKey = await GetTrustedHostInstitutionEncryptionKeyAsync();

        var dto = request?.RequestDTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        var page = await UnitOfWork.InstitutionParentModuleRepository.GetPagedMainParentHeadersAsync(
            dto.IsActive,
            dto.PageNumber,
            dto.PageSize,
            cancellationToken);

        return ApiResponse<List<InstitutionParentModuleResponseDTO>>.SuccessPaginated(
            page.Data.Select(module => MapResponse(module, institutionEncryptionKey)).ToList(),
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            page.TotalPages,
            "Institution Parent Modules retrieved successfully.");
    }
}

/// <summary>
/// Retrieves one Institution-entitled Header Module for an authenticated Host Super Admin.
/// </summary>
public sealed class GetInstitutionParentModuleByIdQueryHandler
    : InstitutionParentModuleQueryHandlerBase,
      IRequestHandler<GetInstitutionParentModuleByIdQuery, ApiResponse<InstitutionParentModuleResponseDTO>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetInstitutionParentModuleByIdQueryHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides Institution entitlement and Institution persistence operations.</param>
    /// <param name="commonRequestService">Validates the current Host Super Admin and Host encryption-key context.</param>
    /// <param name="idEncoderService">Encodes Institution identifiers at the Host API boundary.</param>
    public GetInstitutionParentModuleByIdQueryHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService,
        IIdEncoderService idEncoderService)
        : base(unitOfWork, commonRequestService, idEncoderService)
    {
    }

    /// <inheritdoc />
    public async Task<ApiResponse<InstitutionParentModuleResponseDTO>> Handle(
        GetInstitutionParentModuleByIdQuery request,
        CancellationToken cancellationToken)
    {
        var dto = request?.RequestDTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        if (request.ModuleId <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

      
        var context = await ResolveInstitutionRequestAsync(dto.InstitutionId, cancellationToken);
        var module = await UnitOfWork.InstitutionParentModuleRepository.GetHeaderByModuleIdAsync(
            context.InstitutionId,
            request.ModuleId,
            dto.ModuleScope,
            cancellationToken);

        if (module is null)
        {
            throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);
        }

        return ApiResponse<InstitutionParentModuleResponseDTO>.Success(
            MapResponse(module, context.InstitutionEncryptionKey),
            "Institution Parent Module retrieved successfully.");
    }
}

#endregion

#region Shared Host Request Validation

/// <summary>
/// Provides shared Host Super Admin validation, Institution identifier protection, and response mapping for Institution Parent Module queries.
/// </summary>
public abstract class InstitutionParentModuleQueryHandlerBase
{
    /// <summary>Provides the existing UnitOfWork repository access.</summary>
    protected IUnitOfWork UnitOfWork { get; }

    private readonly ICommonRequestService _commonRequestService;
    private readonly IIdEncoderService _idEncoderService;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstitutionParentModuleQueryHandlerBase"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides Institution entitlement and Institution persistence operations.</param>
    /// <param name="commonRequestService">Validates the current Host request contexts.</param>
    /// <param name="idEncoderService">Encodes Institution identifiers at the Host API boundary.</param>
    protected InstitutionParentModuleQueryHandlerBase(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService,
        IIdEncoderService idEncoderService)
    {
        UnitOfWork = unitOfWork;
        _commonRequestService = commonRequestService;
        _idEncoderService = idEncoderService;
    }

    /// <summary>
    /// Validates the current Host Super Admin, obtains its trusted Institution encryption-key context, and resolves the submitted encrypted Institution identifier.
    /// </summary>
    /// <param name="encryptedInstitutionId">The encrypted Institution identifier from the request.</param>
    /// <param name="cancellationToken">A token used to cancel Institution resolution.</param>
    /// <returns>The decrypted Institution identifier and trusted encryption key.</returns>
    /// <exception cref="ValidationErrorException">Thrown when the encrypted Institution identifier is invalid.</exception>
    /// <exception cref="NotFoundException">Thrown when the Institution does not exist.</exception>
    protected async Task<InstitutionParentModuleRequestContext> ResolveInstitutionRequestAsync(
        string? encryptedInstitutionId,
        CancellationToken cancellationToken)
    {
        await _commonRequestService.ValidateHostSuperAdminRequestAsync();
        var hostKeyContext = await _commonRequestService.ValidateHostUserPermissionRequestAsync();
        var institutionId = HostInstitutionIdentifierProtector.Decrypt(
            encryptedInstitutionId,
            hostKeyContext.InstitutionEncryptionKey,
            _idEncoderService);
        var institution = await UnitOfWork.InstitutionRepository.GetHostManagedInstitutionByIdAsync(institutionId, cancellationToken);
        if (institution is null)
        {
            throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);
        }

        return new InstitutionParentModuleRequestContext(institutionId, hostKeyContext.InstitutionEncryptionKey);
    }

    /// <summary>
    /// Validates the current trusted Host request, Host user type, and current Super Admin role.
    /// </summary>
    /// <returns>A task that completes when the Host Super Admin validation succeeds.</returns>
    protected Task ValidateHostSuperAdminRequestAsync()
    {
        return _commonRequestService.ValidateHostSuperAdminRequestAsync();
    }

    /// <summary>
    /// Obtains the trusted Host token Institution encryption key without resolving a Institution identifier.
    /// </summary>
    /// <returns>The trusted Host token Institution encryption key.</returns>
    protected async Task<string> GetTrustedHostInstitutionEncryptionKeyAsync()
    {
        var hostKeyContext = await _commonRequestService.ValidateHostUserPermissionRequestAsync();
        return hostKeyContext.InstitutionEncryptionKey;
    }

    /// <summary>
     

    /// <summary>
    /// Maps one raw repository result to a Host-facing response with an encrypted Institution identifier.
    /// </summary>
    /// <param name="module">The raw Institution entitlement and Module metadata.</param>
    /// <param name="institutionEncryptionKey">The trusted Host token key.</param>
    /// <returns>The Host-facing Institution Parent Module response.</returns>
    protected InstitutionParentModuleResponseDTO MapResponse(
        InstitutionParentModuleReadModel module,
        string institutionEncryptionKey)
    {
        return new InstitutionParentModuleResponseDTO
        {
            InstitutionId = HostInstitutionIdentifierProtector.Encrypt(module.InstitutionId, institutionEncryptionKey, _idEncoderService),
            Id = module.Id,
            ModuleCode = module.ModuleCode,
            ModuleName = module.ModuleName,
            DisplayName = module.DisplayName,
            UrlPath = module.UrlPath,
            ImageIconWeb = module.ImageIconWeb,
            ImageIconMobile = module.ImageIconMobile,
            ItemPriority = module.ItemPriority,
            ParentModuleId = module.ParentModuleId,
            IsLeafNode = module.IsLeafNode,
            IsEnabled = module.IsEnabled,
            ModuleScope = module.ModuleScope,
            Children = module.Children
                .Select(child => MapResponse(child, institutionEncryptionKey))
                .ToList()
        };
    }
}

/// <summary>
/// Represents a decrypted Institution identifier and its trusted Host token encryption key.
/// </summary>
/// <param name="InstitutionId">The repository-safe numeric Institution identifier.</param>
/// <param name="InstitutionEncryptionKey">The trusted Host token encryption key.</param>
public sealed record InstitutionParentModuleRequestContext(long InstitutionId, string InstitutionEncryptionKey);

#endregion
