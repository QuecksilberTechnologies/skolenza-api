using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.DTOS.Common;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.RoleCmd.Handlers;

#region Command

/// <summary>Controls saved Role imports through the existing module permission behavior.</summary>
public sealed record ManageRoleImportCommand(
    BulkImportJobRequestDTO DTO,
    BulkImportAction Action) : IRequest<ApiResponse<object>>;

#endregion

#region Handler

public sealed class ManageRoleImportCommandHandler(BulkImportWorkflowService workflow)
    : IRequestHandler<ManageRoleImportCommand, ApiResponse<object>>
{
    public async Task<ApiResponse<object>> Handle(
        ManageRoleImportCommand request,
        CancellationToken cancellationToken)
    {
        var result = await workflow.ActAsync(
            BulkImportMaster.Role, request.Action, request.DTO, cancellationToken);
        return ApiResponse<object>.Success(result);
    }
}

#endregion
