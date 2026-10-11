// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the Host-side query contract for retrieving one Institution.
// ================================================================

using skolenza.application.DTOs.Institution;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;

namespace skolenza.application.Features.InstitutionManagementCmd.Queries;

#region Query

/// <summary>
/// Represents the Host-side request to retrieve one Institution for details or editing.
/// </summary>
public sealed class GetInstitutionByIdQuery : IRequest<ApiResponse<HostInstitutionDetailResponseDTO>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetInstitutionByIdQuery"/> class.
    /// </summary>
    /// <param name="requestDTO">The Institution identifier request.</param>
    public GetInstitutionByIdQuery(GetInstitutionByIdRequestDTO requestDTO)
    {
        RequestDTO = requestDTO;
    }

    /// <summary>
    /// Gets the Institution identifier request.
    /// </summary>
    public GetInstitutionByIdRequestDTO RequestDTO { get; }
}

#endregion

#region Handler

/// <summary>
/// Retrieves one Host-managed Institution after validating current Host database permission.
/// </summary>
public sealed class GetInstitutionByIdQueryHandler
    : IRequestHandler<GetInstitutionByIdQuery, ApiResponse<HostInstitutionDetailResponseDTO>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;
    private readonly IIdEncoderService _idEncoderService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetInstitutionByIdQueryHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides Institution and stored-function persistence operations.</param>
    /// <param name="commonRequestService">Validates the trusted Host request context.</param>
    /// <param name="encryptionService">Protects Institution identifiers at the API boundary.</param>
    public GetInstitutionByIdQueryHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService,
        IIdEncoderService idEncoderService)
    {
        _unitOfWork = unitOfWork;
        _commonRequestService = commonRequestService;
        _idEncoderService = idEncoderService;
    }

    /// <summary>
    /// Retrieves the requested Host-visible Institution.
    /// </summary>
    /// <param name="request">The encrypted Institution identifier request.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The complete safe Host Institution detail response with an encrypted identifier.</returns>
    /// <exception cref="NotFoundException">Thrown when the decrypted Institution is unavailable or soft deleted.</exception>
    public async Task<ApiResponse<HostInstitutionDetailResponseDTO>> Handle(
        GetInstitutionByIdQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request?.RequestDTO);
        var dto = request.RequestDTO;
        var hostContext = await _commonRequestService.ValidateHostUserPermissionRequestAsync();
        var institutionId = HostInstitutionIdentifierProtector.Decrypt(
            dto.InstitutionId,
            hostContext.InstitutionEncryptionKey,
            _idEncoderService);
        var institution = await _unitOfWork.InstitutionRepository
            .GetHostManagedInstitutionDetailAsync(institutionId, cancellationToken);

        if (institution is null)
        {
            throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);
        }

        institution.Id = HostInstitutionIdentifierProtector.Encrypt(
            institutionId,
            hostContext.InstitutionEncryptionKey,
            _idEncoderService);

        return ApiResponse<HostInstitutionDetailResponseDTO>.Success(
            institution,
            "Institution retrieved successfully.");
    }
}

#endregion
