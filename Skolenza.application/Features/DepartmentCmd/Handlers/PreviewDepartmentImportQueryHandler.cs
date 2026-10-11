// ================================================================
// Company : Quecksilber Technologies
// Purpose : Previews Department imports through the existing permission pipeline.
// ================================================================

using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.DTOS.Common;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.DepartmentCmd.Handlers;

#region Query

/// <summary>Requests a saved-draft Department import preview using the existing module permission context.</summary>
public sealed record PreviewDepartmentImportQuery(BulkImportPreviewRequestDTO DTO)
    : IRequest<ApiResponse<BulkImportPreviewResponseDTO>>;

#endregion

#region Handler

/// <summary>Validates source data without creating masters, assigning roles or granting permissions.</summary>
public sealed class PreviewDepartmentImportQueryHandler(BulkImportWorkflowService previewService)
    : IRequestHandler<PreviewDepartmentImportQuery, ApiResponse<BulkImportPreviewResponseDTO>>
{
    public async Task<ApiResponse<BulkImportPreviewResponseDTO>> Handle(
        PreviewDepartmentImportQuery request,
        CancellationToken cancellationToken)
    {
        var preview = await previewService.PreviewAsync(
            BulkImportMaster.Department,
            request.DTO,
            cancellationToken);
        return ApiResponse<BulkImportPreviewResponseDTO>.Success(
            preview,
            "Draft saved. Review and confirm to queue import. No master records have been created.");
    }
}

#endregion
