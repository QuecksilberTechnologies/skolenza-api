using skolenza.application.Common.Enums;
using skolenza.application.Common.Models.Security;
using skolenza.application.Constants;
using skolenza.application.DTOS.Common;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Interfaces.ITokenService;
using skolenza.application.Interfaces.IEmail;
using skolenza.application.Interfaces.IEncryptionService;
using Microsoft.Extensions.Configuration;

namespace skolenza.application.Common.Helpers;

/// <summary>Coordinates authenticated durable imports behind existing module permission behaviors.</summary>
public sealed partial class BulkImportWorkflowService(
    BulkImportPreviewService previewService,
    IBulkImportRepository repository,
    ICommonRequestService commonRequestService,
    IUnitOfWork unitOfWork,
    ITokenService? tokenService = null,
    IEmailService? emailService = null,
    IIdEncoderService? idEncoderService = null,
    IConfiguration? configuration = null)
{
    public async Task<BulkImportPreviewResponseDTO> PreviewAsync(
        BulkImportMaster master,
        BulkImportPreviewRequestDTO request,
        CancellationToken cancellationToken)
    {
        var actor = await ValidateAsync(request.OperationId, false);
        if (master == BulkImportMaster.RetiredWorkTitle)
        {
            throw new ConflictException(BulkImportConstants.RetiredWorkTitleMessage);
        }
        var preview = master == BulkImportMaster.StaffMember
            ? await repository.PreviewStaffMembersAsync(
                await BulkImportTableReader.ReadAsync(request, cancellationToken),
                request.ColumnMappingJson, actor, cancellationToken)
            : master is BulkImportMaster.PolicyType or BulkImportMaster.PolicyDefinition or BulkImportMaster.PolicyAssignment
                ? await repository.PreviewPolicyAsync(master,
                    await BulkImportTableReader.ReadAsync(request, cancellationToken),
                    request.ColumnMappingJson, actor, cancellationToken)
            : await previewService.PreviewAsync(master, request, cancellationToken);
        return await repository.SaveDraftAsync(preview, request, actor, cancellationToken);
    }

    public async Task<object> ActAsync(
        BulkImportMaster master,
        BulkImportAction action,
        BulkImportJobRequestDTO request,
        CancellationToken cancellationToken)
    {
        var actor = await ValidateAsync(request.OperationId,
            action is BulkImportAction.Get or BulkImportAction.List or BulkImportAction.Template);
        if (master == BulkImportMaster.RetiredWorkTitle)
        {
            throw new ConflictException(BulkImportConstants.RetiredWorkTitleMessage);
        }
        if (action == BulkImportAction.SendInvitations)
        {
            if (master != BulkImportMaster.StaffMember)
            {
                throw new ValidationErrorException("Invitations are supported only for StaffMember imports.");
            }
            return await SendStaffMemberInvitationsAsync(request, actor, cancellationToken);
        }
        if (action == BulkImportAction.Template)
        {
            return master switch
            {
                BulkImportMaster.StaffMember => string.Join(",", BulkImportConstants.StaffMemberColumns) + "\r\n",
                BulkImportMaster.Department => string.Join(",", BulkImportConstants.DepartmentColumns) + "\r\n",
                BulkImportMaster.StaffEmploymentType => string.Join(",", BulkImportConstants.StaffEmploymentTypeColumns) + "\r\n",
                BulkImportMaster.PolicyType => string.Join(",", BulkImportConstants.PolicyTypeColumns) + "\r\n",
                BulkImportMaster.PolicyDefinition => string.Join(",", BulkImportConstants.PolicyDefinitionColumns) + "\r\n",
                BulkImportMaster.PolicyAssignment => string.Join(",", BulkImportConstants.PolicyAssignmentColumns) + "\r\n",
                _ => string.Join(",", BulkImportConstants.RoleColumns) + "\r\n"
            };
        }
        if (action == BulkImportAction.List)
        {
            return await repository.ListAsync(master, request, actor, cancellationToken);
        }
        return await repository.ActAsync(master, action, request, actor, cancellationToken);
    }

    private async Task<CommonDecodedResult> ValidateAsync(int operationId, bool read)
    {
        var actor = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!actor.Success || actor.InstitutionId <= 0 || actor.LoggedInStaffMemberId <= 0 || actor.RoleId <= 0)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }
        // The existing pipeline checks the actual grant. This binds the endpoint to its action type.
        var operation = await unitOfWork.OperationRepository.GetOperationByIdAsync(operationId);
        var allowedType = operation?.OperationType == (int)OperationType.Add ||
            operation?.OperationType == (int)OperationType.Import ||
            (read && operation?.OperationType == (int)OperationType.View);
        if (operation?.IsActive != true || !allowedType)
        {
            throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
        }
        return actor;
    }
}
