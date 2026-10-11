using skolenza.application.Common.Enums;
using skolenza.application.Common.Models.Security;
using skolenza.application.Constants;
using skolenza.application.DTOS.Common;
using skolenza.application.Exceptions;
using skolenza.domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Repositories;

public sealed partial class BulkImportRepository
{
    #region Invitation Claims And Results

    public async Task<StaffMemberImportInvitationDTO?> ClaimStaffMemberInvitationAsync(
        BulkImportJobRequestDTO request, CommonDecodedResult actor, IReadOnlyCollection<int> attemptedRows,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var job = await LockOwnedStaffMemberJobAsync(request.JobId, actor, cancellationToken);
        if (job.Status is not ((int)BulkImportJobStatus.Completed) and not ((int)BulkImportJobStatus.CompletedWithErrors))
        {
            throw new ConflictException("Review the completed StaffMember import before sending invitations.");
        }
        await EnsureWorkerPermissionAsync(job, cancellationToken);
        var preview = Deserialize(job);
        StaffMemberImportInvitationDTO? invitation = null;
        foreach (var row in preview.Rows.Where(row => row.Status == BulkImportRowStatus.Created &&
                     !attemptedRows.Contains(row.RowNumber) &&
                     (request.RowNumbers is null || request.RowNumbers.Contains(row.RowNumber)) &&
                     row.InvitationStatus is BulkImportInvitationStatus.Pending or BulkImportInvitationStatus.Failed))
        {
            var login = await context.LoginCredentials.AsNoTracking()
                .Where(item => item.InstitutionId == actor.InstitutionId && item.StaffMemberId == row.ImportedStaffMemberId &&
                    item.IsActive && item.IsSoftDeleted != true && item.StaffMember.IsActive && !item.StaffMember.IsSoftDeleted &&
                    item.HasFirstLogin && (item.Password == null || item.Password == ""))
                .Select(item => new { item.StaffMemberId, item.LoginId, item.StaffMember.FirstName, item.StaffMember.LastName })
                .SingleOrDefaultAsync(cancellationToken);
            if (login is null)
            {
                row.InvitationStatus = BulkImportInvitationStatus.NotRequired;
                row.InvitationError = "Account is inactive, unavailable or already has a password.";
                continue;
            }
            row.InvitationStatus = BulkImportInvitationStatus.Sending;
            row.InvitationAttemptId = Guid.NewGuid();
            row.InvitationAttemptedAtUtc = DateTime.UtcNow;
            row.InvitationError = null;
            invitation = new StaffMemberImportInvitationDTO(row.RowNumber, row.InvitationAttemptId.Value,
                login.StaffMemberId, login.LoginId, $"{login.FirstName} {login.LastName}".Trim());
            break;
        }
        await SaveProgress(job, preview, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return invitation;
    }

    public async Task CompleteStaffMemberInvitationAsync(BulkImportJobRequestDTO request, CommonDecodedResult actor,
        StaffMemberImportInvitationDTO invitation, BulkImportInvitationStatus status, CancellationToken cancellationToken)
    {
        if (status is not (BulkImportInvitationStatus.Sent or BulkImportInvitationStatus.Failed or BulkImportInvitationStatus.DeliveryUnknown))
        {
            throw new ValidationErrorException("Invalid invitation delivery result.");
        }
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var job = await LockOwnedStaffMemberJobAsync(request.JobId, actor, cancellationToken);
        var preview = Deserialize(job);
        var row = preview.Rows.SingleOrDefault(item => item.RowNumber == invitation.RowNumber);
        if (row?.InvitationAttemptId != invitation.AttemptId || row.InvitationStatus != BulkImportInvitationStatus.Sending)
        {
            throw new ConflictException("Invitation attempt is no longer current.");
        }
        row.InvitationStatus = status;
        row.InvitationError = status switch
        {
            BulkImportInvitationStatus.Failed => "Email service reported failure. Retry invitations without importing again.",
            BulkImportInvitationStatus.DeliveryUnknown => "Delivery could not be confirmed. Check the email log before any further send.",
            _ => null
        };
        await SaveProgress(job, preview, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<BulkImportJob> LockOwnedStaffMemberJobAsync(Guid id, CommonDecodedResult actor, CancellationToken token)
    {
        var jobs = await context.Set<BulkImportJob>().FromSqlInterpolated($"""
            SELECT * FROM skolenza."BulkImportJob" WHERE "Id" = {id}
            AND "InstitutionId" = {actor.InstitutionId} AND "ActorId" = {actor.LoggedInStaffMemberId}
            AND "Master" = {(int)BulkImportMaster.StaffMember} FOR UPDATE
            """).AsNoTracking().ToListAsync(token);
        return jobs.SingleOrDefault() ?? throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);
    }

    #endregion
}
