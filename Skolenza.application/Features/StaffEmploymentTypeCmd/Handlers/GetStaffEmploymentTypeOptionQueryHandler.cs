using skolenza.application.Constants;
using skolenza.application.DTOS.Staff.Type;
using skolenza.application.DTOS.StaffType;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.StaffEmploymentTypeCmd.Handlers;

#region Query and handler
public sealed record GetStaffEmploymentTypeOptionQuery(GetStaffEmploymentTypeRequestDTO DTO)
    : IRequest<ApiResponse<List<GetStaffEmploymentTypeResponseOptionDTO>>>;

/// <summary>Reads active institution types after the existing module permission pipeline.</summary>
public sealed class GetStaffEmploymentTypeOptionQueryHandler(
    IUnitOfWork unitOfWork, ICommonRequestService commonRequestService)
    : IRequestHandler<GetStaffEmploymentTypeOptionQuery, ApiResponse<List<GetStaffEmploymentTypeResponseOptionDTO>>>
{
    public async Task<ApiResponse<List<GetStaffEmploymentTypeResponseOptionDTO>>> Handle(
        GetStaffEmploymentTypeOptionQuery request, CancellationToken cancellationToken)
    {
        var actor = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!actor.Success || actor.InstitutionId <= 0)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }
        var rows = await unitOfWork.StaffEmploymentTypeRepository.GetAllAsync(actor.InstitutionId, cancellationToken);
        var options = rows.Where(row => row.IsActive == true)
            .Select(row => new GetStaffEmploymentTypeResponseOptionDTO { Id = row.Id, TypeName = row.TypeName }).ToList();
        return ApiResponse<List<GetStaffEmploymentTypeResponseOptionDTO>>.Success(options,
            AppConstants.SuccessMessages.StaffEmploymentTypesRetrieved);
    }
}
#endregion
