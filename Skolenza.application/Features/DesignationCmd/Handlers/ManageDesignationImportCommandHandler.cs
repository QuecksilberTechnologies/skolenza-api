using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.DTOS.Common;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.DesignationCmd.Handlers;

#region Command

/// <summary>Controls saved Designation imports through the existing module permission behavior.</summary>
public sealed record ManageDesignationImportCommand(
    BulkImportJobRequestDTO DTO,
    BulkImportAction Action) : IRequest<ApiResponse<object>>;

#endregion

#region Handler

public sealed class ManageDesignationImportCommandHandler(BulkImportWorkflowService workflow)
    : IRequestHandler<ManageDesignationImportCommand, ApiResponse<object>>
{
    public async Task<ApiResponse<object>> Handle(
        ManageDesignationImportCommand request,
        CancellationToken cancellationToken)
    {
        var result = await workflow.ActAsync(
            BulkImportMaster.Designation, request.Action, request.DTO, cancellationToken);
        return ApiResponse<object>.Success(result);
    }
}

#endregion
