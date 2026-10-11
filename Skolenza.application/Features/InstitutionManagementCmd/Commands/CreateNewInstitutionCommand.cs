// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Dispatches Host-side Institution onboarding through the established transactional creation flow.
// ================================================================

using skolenza.application.DTOs.Registration;
using skolenza.application.DTOs.Institution;
using skolenza.application.Features.RegistrationCmd.Handlers;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.InstitutionManagementCmd.Commands;

/// <summary>Represents the Host-side request to onboard a new Institution.</summary>
public sealed record CreateNewInstitutionCommand(NewInstitutionCreationRequestDTO RequestDTO)
    : IRequest<ApiResponse<InstitutionCreateResponseDTO>>;

/// <summary>Routes Host-side onboarding to the established transactional Institution creation handler.</summary>
public sealed class CreateNewInstitutionCommandHandler
    : IRequestHandler<CreateNewInstitutionCommand, ApiResponse<InstitutionCreateResponseDTO>>
{
    private readonly ISender _sender;

    public CreateNewInstitutionCommandHandler(ISender sender) => _sender = sender;

    public Task<ApiResponse<InstitutionCreateResponseDTO>> Handle(
        CreateNewInstitutionCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.RequestDTO;
        var pattern = dto.StaffMemberCodePattern;
        var legacyRequest = new NewInstitutionCreationBridgeRequestDTO
        {
            ModuleId = dto.ModuleId,
            OperationId = dto.OperationId,
            SubscriptionPlanId = dto.SubscriptionPlanId,
            InstitutionTypeId = dto.InstitutionTypeId,
            CompanyName = dto.CompanyName,
            InstitutionCode = dto.InstitutionCode,
            CompanyEmailDomain = dto.CompanyEmailDomain,
            GenderId = dto.GenderId,
            InstitutionEmail = dto.InstitutionEmail,
            ContactPersonName = dto.ContactPersonName,
            ContactNumber = dto.ContactNumber,
            CountryId = dto.CountryId,
            Prefix = pattern.Prefix,
            IncludeYear = pattern.IncludeYear,
            IncludeMonth = pattern.IncludeMonth,
            IncludeDepartment = pattern.IncludeDepartment,
            Separator = pattern.Separator,
            RunningNumberLength = pattern.RunningNumberLength,
            Profile = dto.Profile,
            InitialLocation = dto.InitialLocation,
        };

        return _sender.Send(new CreateInstitutionCommand(legacyRequest), cancellationToken);
    }
}

internal sealed class NewInstitutionCreationBridgeRequestDTO : InstitutionCreationRequestDTO, INewInstitutionOnboardingConfiguration
{
    public NewInstitutionProfileRequestDTO Profile { get; set; } = new();
    public NewCampusRequestDTO InitialLocation { get; set; } = new();
}
