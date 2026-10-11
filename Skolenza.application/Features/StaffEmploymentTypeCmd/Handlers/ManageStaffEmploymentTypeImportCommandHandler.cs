using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.DTOS.Common;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.StaffEmploymentTypeCmd.Handlers;

#region Command

/// <summary>Controls saved StaffEmploymentType imports through the existing module permission behavior.</summary>
public sealed record ManageStaffEmploymentTypeImportCommand(
    BulkImportJobRequestDTO DTO,
    BulkImportAction Action) : IRequest<ApiResponse<object>>;

#endregion

#region Handler

public sealed class ManageStaffEmploymentTypeImportCommandHandler(BulkImportWorkflowService workflow)
    : IRequestHandler<ManageStaffEmploymentTypeImportCommand, ApiResponse<object>>
{
    public async Task<ApiResponse<object>> Handle(
        ManageStaffEmploymentTypeImportCommand request,
        CancellationToken cancellationToken)
    {
        var result = await workflow.ActAsync(
            BulkImportMaster.StaffEmploymentType, request.Action, request.DTO, cancellationToken);
        return ApiResponse<object>.Success(result);
    }
}

#endregion
