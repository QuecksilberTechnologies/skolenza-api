// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the Host-side query contract for Institution management records.
// ================================================================

using AutoMapper;
using skolenza.application.DTOs.Institution;
using skolenza.application.Common.Helpers;
using skolenza.application.Common.Helpers.EncryptionHelper;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Features.InstitutionManagementCmd.Commands;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;

namespace skolenza.application.Features.InstitutionManagementCmd.Queries;

#region Query

/// <summary>
/// Represents the Host-side request to retrieve Institution management records.
/// </summary>
public sealed class GetAllInstitutionsQuery : IRequest<ApiResponse<List<HostInstitutionResponseDTO>>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetAllInstitutionsQuery"/> class.
    /// </summary>
    /// <param name="requestDTO">The optional Institution management filters and paging values.</param>
    public GetAllInstitutionsQuery(GetAllInstitutionsRequestDTO? requestDTO)
    {
        RequestDTO = requestDTO;
    }

    /// <summary>
    /// Gets the optional Institution management filters and paging values.
    /// </summary>
    public GetAllInstitutionsRequestDTO? RequestDTO { get; }
}

#endregion

#region Handler

/// <summary>
/// Retrieves Host-managed Institutions after current Host database permission validation.
/// </summary>
public sealed class GetAllInstitutionsQueryHandler
    : IRequestHandler<GetAllInstitutionsQuery, ApiResponse<List<HostInstitutionResponseDTO>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;
    private readonly IIdEncoderService _idEncoderService;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAllInstitutionsQueryHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides Institution and stored-function persistence operations.</param>
    /// <param name="commonRequestService">Validates the trusted Host request context.</param>
    /// <param name="idEncoderService">Protects Institution and StaffMember identifiers at the API boundary.</param>
    /// <param name="mapper">Maps the Host Institution entity to its response projection.</param>
    public GetAllInstitutionsQueryHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService,
        IIdEncoderService idEncoderService,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _commonRequestService = commonRequestService;
        _idEncoderService = idEncoderService;
        _mapper = mapper;
    }

    /// <summary>
    /// Retrieves the requested Host-visible Institution page.
    /// </summary>
    /// <param name="request">The Host Institution list request.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>A paged Host Institution response with encrypted identifiers.</returns>
    public async Task<ApiResponse<List<HostInstitutionResponseDTO>>> Handle(
        GetAllInstitutionsQuery request,
        CancellationToken cancellationToken)
    {
        var filter = request.RequestDTO ?? throw new ArgumentNullException(nameof(request.RequestDTO));
        var hostContext = await _commonRequestService.ValidateHostUserPermissionRequestAsync();

        var page = await _unitOfWork.InstitutionRepository
            .GetHostManagedInstitutionsAsync(filter, cancellationToken);
        var response = page.Data
            .Select(institution => MapInstitution(institution, hostContext.InstitutionEncryptionKey))
            .ToList();

        return ApiResponse<List<HostInstitutionResponseDTO>>.SuccessPaginated(
            response,
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            page.TotalPages,
            "Institutions retrieved successfully.");
    }

    private HostInstitutionResponseDTO MapInstitution(Institution institution, string institutionEncryptionKey)
    {
        var institutionLoginCredential = InstitutionOnboardingVerification.FindCredential(institution);

        var response = _mapper.Map<HostInstitutionResponseDTO>(institution);
        response.Id = HostInstitutionIdentifierProtector.Encrypt(
            institution.Id,
            institutionEncryptionKey,
            _idEncoderService);
        response.IsVerified = InstitutionOnboardingVerification.IsVerified(institution, institutionLoginCredential);
        response.StaffMemberId = institutionLoginCredential is null
            ? string.Empty
            : _idEncoderService.EncodeId_long(
                institutionLoginCredential.StaffMemberId,
                EncryptionSanitizer.SuperSanitize(institutionEncryptionKey));

        return response;
    }
}

#endregion
