using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Host;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.HostDeviceCmd.Handlers;

#region Requests

public sealed record PreviewHostBulkImportQuery(BulkImportMaster Master, HostBulkImportPreviewRequestDTO DTO)
    : IRequest<ApiResponse<BulkImportPreviewResponseDTO>>;

public sealed record ManageHostBulkImportCommand(BulkImportMaster Master, BulkImportAction Action,
    HostBulkImportJobRequestDTO DTO) : IRequest<ApiResponse<object>>;

#endregion

#region Handlers

public sealed class PreviewHostBulkImportQueryHandler(HostBulkImportWorkflowService workflow)
    : IRequestHandler<PreviewHostBulkImportQuery, ApiResponse<BulkImportPreviewResponseDTO>>
{
    public async Task<ApiResponse<BulkImportPreviewResponseDTO>> Handle(PreviewHostBulkImportQuery request, CancellationToken ct)
    {
        return ApiResponse<BulkImportPreviewResponseDTO>.Success(
            await workflow.PreviewAsync(request.Master, request.DTO, ct));
    }
}

public sealed class ManageHostBulkImportCommandHandler(HostBulkImportWorkflowService workflow)
    : IRequestHandler<ManageHostBulkImportCommand, ApiResponse<object>>
{
    public async Task<ApiResponse<object>> Handle(ManageHostBulkImportCommand request, CancellationToken ct)
    {
        return ApiResponse<object>.Success(await workflow.ActAsync(request.Master, request.Action, request.DTO, ct));
    }
}

#endregion
