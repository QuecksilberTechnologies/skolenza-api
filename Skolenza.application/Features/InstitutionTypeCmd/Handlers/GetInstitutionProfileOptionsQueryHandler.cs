using MediatR;
using skolenza.application.DTOs.InstitutionType;
using skolenza.application.Constants;
using skolenza.application.Wrappers;

namespace skolenza.application.Features.InstitutionTypeCmd.Handlers;

public sealed record GetInstitutionCategoryOptionsQuery
    : IRequest<ApiResponse<IReadOnlyList<InstitutionOptionDTO>>>;

public sealed record GetInstitutionCampusTypeOptionsQuery
    : IRequest<ApiResponse<IReadOnlyList<InstitutionOptionDTO>>>;

public sealed class GetInstitutionCategoryOptionsQueryHandler
    : IRequestHandler<GetInstitutionCategoryOptionsQuery, ApiResponse<IReadOnlyList<InstitutionOptionDTO>>>
{
    public Task<ApiResponse<IReadOnlyList<InstitutionOptionDTO>>> Handle(
        GetInstitutionCategoryOptionsQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<InstitutionOptionDTO> options = AppConstants.InstitutionCategories
            .Select(option => new InstitutionOptionDTO(
                option.Id,
                option.Code,
                option.Name,
                option.Description))
            .ToList();

        return Task.FromResult(ApiResponse<IReadOnlyList<InstitutionOptionDTO>>.Success(
            options,
            "Institution categories retrieved successfully."));
    }
}

public sealed class GetInstitutionCampusTypeOptionsQueryHandler
    : IRequestHandler<GetInstitutionCampusTypeOptionsQuery, ApiResponse<IReadOnlyList<InstitutionOptionDTO>>>
{
    public Task<ApiResponse<IReadOnlyList<InstitutionOptionDTO>>> Handle(
        GetInstitutionCampusTypeOptionsQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<InstitutionOptionDTO> options = AppConstants.InstitutionCampusTypes
            .Select(option => new InstitutionOptionDTO(
                option.Id,
                option.Code,
                option.Name,
                option.Description))
            .ToList();

        return Task.FromResult(ApiResponse<IReadOnlyList<InstitutionOptionDTO>>.Success(
            options,
            "Institution campus types retrieved successfully."));
    }
}
