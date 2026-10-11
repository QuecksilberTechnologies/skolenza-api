using MediatR;
using skolenza.application.Constants;
using skolenza.application.DTOS.Staff;
using skolenza.application.Wrappers;

namespace skolenza.application.Features.StaffEmploymentTypeCmd.Handlers;

public sealed record GetStaffEmploymentTypeOptionsQuery
    : IRequest<ApiResponse<IReadOnlyList<StaffEmploymentTypeOptionDTO>>>;

public sealed class GetStaffEmploymentTypeOptionsQueryHandler
    : IRequestHandler<GetStaffEmploymentTypeOptionsQuery,
        ApiResponse<IReadOnlyList<StaffEmploymentTypeOptionDTO>>>
{
    public Task<ApiResponse<IReadOnlyList<StaffEmploymentTypeOptionDTO>>> Handle(
        GetStaffEmploymentTypeOptionsQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<StaffEmploymentTypeOptionDTO> options = AppConstants.StaffEmploymentTypes
            .Select(option => new StaffEmploymentTypeOptionDTO(
                option.Id,
                option.Code,
                option.Name,
                option.Description))
            .ToList();

        return Task.FromResult(
            ApiResponse<IReadOnlyList<StaffEmploymentTypeOptionDTO>>.Success(
                options,
                "Staff employment types retrieved successfully."));
    }
}
