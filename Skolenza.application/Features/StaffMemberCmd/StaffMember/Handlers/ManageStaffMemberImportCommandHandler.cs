using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.DTOS.Common;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.StaffMemberCmd.StaffMemberBase.Handlers;

#region Command

/// <summary>Controls saved StaffMember imports through the existing module permission behavior.</summary>
public sealed record ManageStaffMemberImportCommand(
    BulkImportJobRequestDTO DTO,
    BulkImportAction Action) : IRequest<ApiResponse<object>>;

#endregion

#region Handler

public sealed class ManageStaffMemberImportCommandHandler(BulkImportWorkflowService workflow)
    : IRequestHandler<ManageStaffMemberImportCommand, ApiResponse<object>>
{
    public async Task<ApiResponse<object>> Handle(
        ManageStaffMemberImportCommand request,
        CancellationToken cancellationToken)
    {
        var result = await workflow.ActAsync(
            BulkImportMaster.StaffMember, request.Action, request.DTO, cancellationToken);
        return ApiResponse<object>.Success(result);
    }
}

#endregion

