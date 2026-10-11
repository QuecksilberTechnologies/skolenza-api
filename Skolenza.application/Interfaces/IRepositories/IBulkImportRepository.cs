using skolenza.application.Common.Enums;
using skolenza.application.Common.Models.Security;
using skolenza.application.DTOS.Common;

namespace skolenza.application.Interfaces.IRepositories;

public interface IBulkImportRepository
{
    Task<BulkImportPreviewResponseDTO> PreviewHostAsync(
        BulkImportMaster master, BulkImportTableDTO table, string? mappingJson,
        long institutionId, HostUserRequestContext host, CancellationToken cancellationToken);
    Task<BulkImportPreviewResponseDTO> PreviewStaffMembersAsync(
        BulkImportTableDTO table, string? mappingJson, CommonDecodedResult actor,
        CancellationToken cancellationToken);
    Task<BulkImportPreviewResponseDTO> PreviewPolicyAsync(
        BulkImportMaster master, BulkImportTableDTO table, string? mappingJson,
        CommonDecodedResult actor, CancellationToken cancellationToken);

    Task<BulkImportPreviewResponseDTO> SaveDraftAsync(
        BulkImportPreviewResponseDTO preview,
        BulkImportPreviewRequestDTO request,
        CommonDecodedResult actor,
        CancellationToken cancellationToken);

    Task<BulkImportJobResponseDTO> ActAsync(
        BulkImportMaster master,
        BulkImportAction action,
        BulkImportJobRequestDTO request,
        CommonDecodedResult actor,
        CancellationToken cancellationToken);

    Task<List<BulkImportJobResponseDTO>> ListAsync(
        BulkImportMaster master,
        BulkImportJobRequestDTO request,
        CommonDecodedResult actor,
        CancellationToken cancellationToken);

    Task<bool> ProcessNextBatchAsync(CancellationToken cancellationToken);

    Task<StaffMemberImportInvitationDTO?> ClaimStaffMemberInvitationAsync(
        BulkImportJobRequestDTO request, CommonDecodedResult actor, IReadOnlyCollection<int> attemptedRows,
        CancellationToken cancellationToken);

    Task CompleteStaffMemberInvitationAsync(BulkImportJobRequestDTO request, CommonDecodedResult actor,
        StaffMemberImportInvitationDTO invitation, BulkImportInvitationStatus status, CancellationToken cancellationToken);
}
