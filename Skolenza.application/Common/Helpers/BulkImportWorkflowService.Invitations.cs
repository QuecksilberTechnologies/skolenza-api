using skolenza.application.Common.Enums;
using skolenza.application.Common.Models.Security;
using skolenza.application.Constants;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Token;
using skolenza.application.Exceptions;

namespace skolenza.application.Common.Helpers;

public sealed partial class BulkImportWorkflowService
{
    #region Explicit StaffMember Invitations

    private async Task<BulkImportJobResponseDTO> SendStaffMemberInvitationsAsync(
        BulkImportJobRequestDTO request, CommonDecodedResult actor, CancellationToken cancellationToken)
    {
        if (tokenService is null || emailService is null || idEncoderService is null || configuration is null)
        {
            throw new InvalidOperationException("StaffMember invitation services are not configured.");
        }
        var baseUrl = configuration["FrontEndWebURL:BaseUrl"]?.TrimEnd('/');
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            throw new ValidationErrorException("Configure the front-end password setup URL before sending invitations.");
        }
        if (request.RowNumbers is { Count: > 100 } || request.RowNumbers?.Any(row => row <= 0) == true)
        {
            throw new ValidationErrorException("Select at most 100 positive source row numbers.");
        }
        var attempted = new HashSet<int>();
        while (attempted.Count < 100)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var invitation = await repository.ClaimStaffMemberInvitationAsync(request, actor, attempted, cancellationToken);
            if (invitation is null)
            {
                break;
            }
            attempted.Add(invitation.RowNumber);
            var sending = false;
            BulkImportInvitationStatus status;
            try
            {
                var now = DateTime.UtcNow;
                var token = await tokenService.GenerateInstitutionToken(new GetTokenInfoDTO
                {
                    StaffMemberId = idEncoderService.EncodeId_long(invitation.StaffMemberId, actor.Claims.InstitutionEncriptionKey),
                    Email = invitation.Email,
                    FullName = invitation.FullName,
                    TokenPurpose = idEncoderService.EncodeId_int(ConstantValues.SetPassword, string.Empty),
                    IssuedAt = now,
                    Expiry = now.AddMinutes(30),
                    IsFirstLogin = true,
                    ClientType = "Web"
                });
                sending = true;
                var sent = await emailService.SendTemplatedEmailAsync(ConstantValues.WelcomeEmail,
                    invitation.Email, actor.InstitutionId, new Dictionary<string, string>
                    {
                        ["UserName"] = invitation.FullName,
                        ["VerificationUrl"] = $"{baseUrl}/auth/set-password?token={Uri.EscapeDataString(token)}",
                        ["LinkExpiryMinutes"] = "30"
                    });
                status = sent ? BulkImportInvitationStatus.Sent : BulkImportInvitationStatus.Failed;
            }
            catch (Exception)
            {
                // Never expose SMTP credentials or token contents in import reports.
                status = sending ? BulkImportInvitationStatus.DeliveryUnknown : BulkImportInvitationStatus.Failed;
            }
            // Persist the outcome even when the HTTP caller disconnected after dispatch.
            await repository.CompleteStaffMemberInvitationAsync(request, actor, invitation, status, CancellationToken.None);
        }
        return await repository.ActAsync(BulkImportMaster.StaffMember, BulkImportAction.Get, request, actor, cancellationToken);
    }

    #endregion
}
