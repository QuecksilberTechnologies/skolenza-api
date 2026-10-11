// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Reports future Institution deletion dependency groups without changing data.
// ================================================================

using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOs.Institution;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.InstitutionManagementCmd.Queries;

/// <summary>Represents the Host-side read-only request for Institution deletion dependency information.</summary>
public sealed record GetInstitutionDeleteDependencyInfoQuery(string EncryptedInstitutionId, PermissionRequestDTO PermissionRequest)
    : IRequest<ApiResponse<InstitutionDeleteDependencyInfoResponseDTO>>;

/// <summary>Validates a Host request and reports the transactional groups reserved for a future Institution deletion cascade.</summary>
public sealed class GetInstitutionDeleteDependencyInfoQueryHandler
    : IRequestHandler<GetInstitutionDeleteDependencyInfoQuery, ApiResponse<InstitutionDeleteDependencyInfoResponseDTO>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;
    private readonly IIdEncoderService _idEncoderService;

    public GetInstitutionDeleteDependencyInfoQueryHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService,
        IIdEncoderService idEncoderService)
    {
        _unitOfWork = unitOfWork;
        _commonRequestService = commonRequestService;
        _idEncoderService = idEncoderService;
    }

    public async Task<ApiResponse<InstitutionDeleteDependencyInfoResponseDTO>> Handle(
        GetInstitutionDeleteDependencyInfoQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.PermissionRequest);

        var hostContext = await _commonRequestService.ValidateHostUserPermissionRequestAsync();
        var institutionId = HostInstitutionIdentifierProtector.Decrypt(
            request.EncryptedInstitutionId,
            hostContext.InstitutionEncryptionKey,
            _idEncoderService);
        var institution = await _unitOfWork.InstitutionRepository
            .GetHostManagedInstitutionByIdAsync(institutionId, cancellationToken);
        if (institution is null)
        {
            throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);
        }

        return ApiResponse<InstitutionDeleteDependencyInfoResponseDTO>.Success(
            new InstitutionDeleteDependencyInfoResponseDTO
            {
                InstitutionId = HostInstitutionIdentifierProtector.Encrypt(institution.Id, hostContext.InstitutionEncryptionKey, _idEncoderService),
                Message = "Institution deletion is not executed. A future cascade must process the listed transactional data groups.",
                TransactionalDataGroups = new List<string>
                {
                    "Leave and leave-policy records",
                    "Attendance and work-arrangement records",
                    "Payroll and salary records",
                    "Tickets and ticket-history records",
                    "Assets and asset-assignment records",
                    "StaffMember, login, role, and permission records",
                    "Institution configuration, modules, operations, and subscription records"
                }
            },
            "Institution deletion dependency information retrieved successfully.");
    }
}
