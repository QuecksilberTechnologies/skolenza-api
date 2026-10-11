using System.Reflection;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.Common.Models.Security;
using skolenza.application.Constants;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Host;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using NUnit.Framework;
using Module = skolenza.domain.Entity.Module;

namespace skolenza.automationtests.Unit;

[TestFixture, Category("HostBulkPermission")]
public sealed class HostBulkImportPermissionTests
{
    [TestCase(0, 2, 12, false, typeof(ForbiddenAccessException))]
    [TestCase(-1, 2, 12, false, typeof(UnauthorizedAccessException))]
    [TestCase(-2, 2, 12, false, typeof(UnauthorizedAccessException))]
    [TestCase(1, 1, 12, false, typeof(ForbiddenAccessException))]
    [TestCase(1, 2, 1, false, typeof(ForbiddenAccessException))]
    [TestCase(1, 2, 12, true, typeof(ForbiddenAccessException))]
    public void Denied_stale_scope_action_and_wrong_module_never_reach_queue(
        int result, short scope, int operationType, bool wrongModule, Type exception)
    {
        var workflow = Workflow(result, scope, operationType, wrongModule);
        Assert.ThrowsAsync(exception, async () => await workflow.ActAsync(BulkImportMaster.DeviceMaster,
            BulkImportAction.Confirm, new HostBulkImportJobRequestDTO { ModuleId = 81, OperationId = 23 }, default));
    }

    [TestCase(BulkImportMaster.DeviceMaster)]
    [TestCase(BulkImportMaster.InstitutionCard)]
    public async Task Template_checks_host_grant_and_returns_only_supported_headers(BulkImportMaster master)
    {
        var workflow = Workflow(1, 2, 4, master: master);
        var result = (string)await workflow.ActAsync(master, BulkImportAction.Template,
            new HostBulkImportJobRequestDTO { ModuleId = 81, OperationId = 4 }, default);
        Assert.That(result, Does.Not.Contain("InstitutionId").And.Not.Contain("ModuleId").And.Not.Contain("OperationId"));
        Assert.That(result, Does.Contain(master == BulkImportMaster.DeviceMaster ? "DeviceCode" : "CardNumber"));
    }

    [Test]
    public void Device_job_rejects_institution_assignment()
    {
        var workflow = Workflow(1, 2, 12);
        Assert.ThrowsAsync<ValidationErrorException>(async () => await workflow.ActAsync(BulkImportMaster.DeviceMaster,
            BulkImportAction.Confirm, new HostBulkImportJobRequestDTO
            { ModuleId = 81, OperationId = 23, InstitutionId = "selected-institution" }, default));
    }

    [Test]
    public void Card_job_requires_selected_encoded_institution()
    {
        var workflow = Workflow(1, 2, 12, master: BulkImportMaster.InstitutionCard);
        Assert.ThrowsAsync<ValidationErrorException>(async () => await workflow.ActAsync(BulkImportMaster.InstitutionCard,
            BulkImportAction.Confirm, new HostBulkImportJobRequestDTO { ModuleId = 81, OperationId = 23 }, default));
    }

    [TestCase(0, 23)]
    [TestCase(81, 0)]
    public void Missing_permission_identifiers_are_validation_errors(int module, int operation)
    {
        Assert.ThrowsAsync<ValidationErrorException>(async () => await Workflow(1, 2, 12).ActAsync(
            BulkImportMaster.DeviceMaster, BulkImportAction.Confirm,
            new HostBulkImportJobRequestDTO { ModuleId = module, OperationId = operation }, default));
    }

    private static HostBulkImportWorkflowService Workflow(int result, short scope, int operationType,
        bool wrongModule = false, BulkImportMaster master = BulkImportMaster.DeviceMaster)
    {
        var common = Proxy<ICommonRequestService>((method, _) => method.Name switch
        {
            nameof(ICommonRequestService.ValidateHostUserPermissionRequestAsync) => Task.FromResult(new HostUserRequestContext
            {
                HostUserId = 5, TokenHostRoleId = 3, CurrentHostRoleId = 3, UserType = "Host", InstitutionEncryptionKey = "test"
            }),
            _ => throw new AssertionException("Institution authentication must never be called.")
        });
        var store = Proxy<IStoreProcedureRepository>((method, _) => method.Name == "CheckHostUserPermissionAsync"
            ? Task.FromResult(new HostUserPermissionCheckResponseDTO { ResultCode = result })
            : throw new AssertionException("Only established Host permission lookup is allowed."));
        var modules = Proxy<IModuleRepository>((_, _) => Task.FromResult<Module?>(new Module
        {
            Id = 81, IsActive = true, ModuleScope = scope, ModuleName = "Bulk",
            ModuleCode = wrongModule ? "WRONG" : master == BulkImportMaster.DeviceMaster
                ? BulkImportConstants.HostDeviceBulkModuleCode : BulkImportConstants.HostCardBulkModuleCode
        }));
        var operations = Proxy<IOperationRepository>((_, _) => Task.FromResult<Operation?>(new Operation
        {
            Id = 23, OperationType = operationType, IsActive = true, OperationName = "Import"
        }));
        var unit = Proxy<IUnitOfWork>((method, _) => method.Name switch
        {
            "get_StoreProcedureRepository" => store,
            "get_ModuleRepository" => modules,
            "get_OperationRepository" => operations,
            _ => throw new AssertionException("Unexpected persistence access.")
        });
        var repository = Proxy<IBulkImportRepository>((_, _) => throw new AssertionException("Queue must not be accessed."));
        var ids = Proxy<IIdEncoderService>((_, _) => throw new AssertionException("Missing institution must be rejected before decoding."));
        return new HostBulkImportWorkflowService(repository, common, unit, ids);
    }

    public class TestProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Callback { get; set; } = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            return Callback(method!, args);
        }
    }

    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> callback) where T : class
    {
        var proxy = DispatchProxy.Create<T, TestProxy>();
        ((TestProxy)(object)proxy).Callback = callback;
        return proxy;
    }
}
