using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.DTOS.Common;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.DepartmentCmd.Handlers;

#region Command

/// <summary>Controls saved Department imports through the existing module permission behavior.</summary>
public sealed record ManageDepartmentImportCommand(
    BulkImportJobRequestDTO DTO,
    BulkImportAction Action) : IRequest<ApiResponse<object>>;

#endregion

#region Handler

public sealed class ManageDepartmentImportCommandHandler(BulkImportWorkflowService workflow)
    : IRequestHandler<ManageDepartmentImportCommand, ApiResponse<object>>
{
    public async Task<ApiResponse<object>> Handle(
        ManageDepartmentImportCommand request,
        CancellationToken cancellationToken)
    {
        var result = await workflow.ActAsync(
            BulkImportMaster.Department, request.Action, request.DTO, cancellationToken);
        return ApiResponse<object>.Success(result);
    }
}

#endregion
