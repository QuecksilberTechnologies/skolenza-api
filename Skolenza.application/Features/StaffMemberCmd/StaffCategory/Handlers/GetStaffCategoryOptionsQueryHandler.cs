using MediatR;
using skolenza.application.DTOS.Staff;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;

namespace skolenza.application.Features.StaffMemberCmd.StaffCategory.Handlers;

public sealed record GetStaffCategoryOptionsQuery
    : IRequest<ApiResponse<IReadOnlyList<StaffCategoryOptionDTO>>>;

public sealed class GetStaffCategoryOptionsQueryHandler(IStaffCategoryRepository repository)
    : IRequestHandler<GetStaffCategoryOptionsQuery,
        ApiResponse<IReadOnlyList<StaffCategoryOptionDTO>>>
{
    public async Task<ApiResponse<IReadOnlyList<StaffCategoryOptionDTO>>> Handle(
        GetStaffCategoryOptionsQuery request,
        CancellationToken cancellationToken)
    {
        var options = await repository.GetActiveOptionsAsync(cancellationToken);
        return ApiResponse<IReadOnlyList<StaffCategoryOptionDTO>>.Success(
            options,
            "Staff categories retrieved successfully.");
    }
}
