using skolenza.application.Common.Enums;
using skolenza.application.Common.Models.Security;
using skolenza.application.Constants;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Host;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IRepositories;

namespace skolenza.application.Common.Helpers;

/// <summary>Host import orchestration using the established persisted permission validator.</summary>
public sealed class HostBulkImportWorkflowService(
    IBulkImportRepository repository, ICommonRequestService common,
    IUnitOfWork unitOfWork, IIdEncoderService ids)
{
    #region Preview and actions

    public async Task<BulkImportPreviewResponseDTO> PreviewAsync(
        BulkImportMaster master, HostBulkImportPreviewRequestDTO request, CancellationToken ct)
    {
        var host = await AuthorizeAsync(master, request.ModuleId, request.OperationId, false, ct);
        var actor = Owner(master, request.InstitutionId, host);
        var table = await BulkImportTableReader.ReadAsync(request, ct);
        var preview = await repository.PreviewHostAsync(master, table, request.ColumnMappingJson,
            actor.InstitutionId, host, ct);
        return await repository.SaveDraftAsync(preview, request, actor, ct);
    }

    public async Task<object> ActAsync(BulkImportMaster master, BulkImportAction action,
        HostBulkImportJobRequestDTO request, CancellationToken ct)
    {
        if (action == BulkImportAction.SendInvitations || !Enum.IsDefined(action))
        {
            throw new ValidationErrorException("Unsupported Host import action.");
        }
        var read = action is BulkImportAction.Get or BulkImportAction.List or BulkImportAction.Template;
        var host = await AuthorizeAsync(master, request.ModuleId, request.OperationId, read, ct);
        if (action == BulkImportAction.Template)
        {
            var columns = master switch
            {
                BulkImportMaster.DeviceMaster => HostBulkImportTableMapper.DeviceColumns,
                BulkImportMaster.InstitutionCard => HostBulkImportTableMapper.CardColumns,
                BulkImportMaster.HostModule => HostBulkImportTableMapper.ModuleColumns,
                BulkImportMaster.HostSubModule => HostBulkImportTableMapper.SubModuleColumns,
                BulkImportMaster.HostOperation => HostBulkImportTableMapper.OperationColumns,
                _ => HostBulkImportTableMapper.ModuleOperationColumns
            };
            return string.Join(",", columns) + "\r\n";
        }
        var actor = Owner(master, request.InstitutionId, host);
        if (action == BulkImportAction.List)
        {
            return await repository.ListAsync(master, request, actor, ct);
        }
        return await repository.ActAsync(master, action, request, actor, ct);
    }

    #endregion

    #region Host authorization and ownership

    private async Task<HostUserRequestContext> AuthorizeAsync(
        BulkImportMaster master, int moduleId, int operationId, bool read, CancellationToken ct)
    {
        var expected = master switch
        {
            BulkImportMaster.DeviceMaster => BulkImportConstants.HostDeviceBulkModuleCode,
            BulkImportMaster.InstitutionCard => BulkImportConstants.HostCardBulkModuleCode,
            BulkImportMaster.HostModule => BulkImportConstants.HostModuleBulkModuleCode,
            BulkImportMaster.HostSubModule => BulkImportConstants.HostSubModuleBulkModuleCode,
            BulkImportMaster.HostOperation => BulkImportConstants.HostOperationBulkModuleCode,
            BulkImportMaster.HostModuleOperation => BulkImportConstants.HostModuleOperationBulkModuleCode,
            _ => throw new ValidationErrorException("Unsupported Host import target.")
        };
        var host = await HostRuntimePermissionValidator.ValidateAsync(
            common, unitOfWork.StoreProcedureRepository, moduleId, operationId, ct);
        var module = await unitOfWork.ModuleRepository.GetModuleByIdAsync(moduleId);
        var operation = await unitOfWork.OperationRepository.GetOperationByIdAsync(operationId);
        if (module?.ModuleCode != expected || module.ModuleScope != 2 || !module.IsActive ||
            operation?.IsActive != true ||
            (operation.OperationType != (int)OperationType.Import &&
             !(read && operation.OperationType == (int)OperationType.View)))
        {
            throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
        }
        return host;
    }

    private CommonDecodedResult Owner(BulkImportMaster master, string? institutionId, HostUserRequestContext host)
    {
        if (master != BulkImportMaster.InstitutionCard && !string.IsNullOrWhiteSpace(institutionId))
        {
            throw new ValidationErrorException("This Host catalogue import does not accept a InstitutionId.");
        }
        // Shared queue stores an actor ID. Host-only master values partition the actor domain;
        // worker authorization uses Host permission lookup, never InstitutionStaffMember permissions.
        return new CommonDecodedResult
        {
            Success = true,
            LoggedInStaffMemberId = host.HostUserId,
            RoleId = checked((int)host.TokenHostRoleId),
            InstitutionId = master == BulkImportMaster.InstitutionCard
                ? HostInstitutionIdentifierProtector.Decrypt(institutionId, host.InstitutionEncryptionKey, ids) : 0
        };
    }

    #endregion
}
