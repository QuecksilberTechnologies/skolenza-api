using System.Reflection;
using System.Text.Json;
using AutoMapper;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.Common.Models;
using skolenza.application.Common.Models.Security;
using skolenza.application.Constants;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Host;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Mappings;
using skolenza.domain.Entity;
using skolenza.infrastructure.EncryptionService;
using skolenza.persistance.Data.Context;
using skolenza.persistance.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using NUnit.Framework;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using skolenza.application.Features.HostDeviceCmd.Handlers;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace skolenza.automationtests.Unit;

/// <summary>Real PostgreSQL queue, business rows, encryption and persisted Host permission function.</summary>
[TestFixture, Category("HostBulkDatabase"), NonParallelizable]
public sealed class HostBulkImportDatabaseTests
{
    private SkolenzaDbContext _db = null!;
    private BulkImportRepository _repo = null!;
    private HostUserRequestContext _host = null!;
    private readonly AesEncryptionService _encryption = new();
    private readonly List<Guid> _jobs = new();
    private readonly List<long> _grants = new();
    private long _institution;
    private string _prefix = null!;
    private int _operation;
    private Dictionary<BulkImportMaster, int> _modules = null!;

    [SetUp]
    public async Task Setup()
    {
        var connection = Environment.GetEnvironmentVariable("SKOLENZA_BULK_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection))
        {
            Assert.Ignore("Requires isolated skolenza_bulk_test with AddHostBulkImport and SeedHostBulkImportModules applied.");
        }
        var parsed = new NpgsqlConnectionStringBuilder(connection!);
        Assert.That(parsed.Database, Is.EqualTo("skolenza_bulk_test"));
        Assert.That(parsed.Host, Is.AnyOf("127.0.0.1", "localhost"));
        _db = new SkolenzaDbContext(new DbContextOptionsBuilder<SkolenzaDbContext>().UseNpgsql(connection).Options);
        var mapper = new MapperConfiguration(config => config.AddProfile<MappingProfile>()).CreateMapper();
        _repo = new BulkImportRepository(_db, mapper,
            new StoreProcedureRepository(_db, NullLogger<StoreProcedureRepository>.Instance, mapper),
            Options.Create(new BulkImportOptions { BatchSize = 1 }), encryption: _encryption);
        _prefix = "HostBulkTest-" + Guid.NewGuid().ToString("N")[..10];
        var user = await _db.Set<HostUser>().FirstAsync(x => x.IsActive && !x.IsSoftDeleted);
        _host = new HostUserRequestContext
        {
            HostUserId = user.Id, TokenHostRoleId = user.HostRoleId, CurrentHostRoleId = user.HostRoleId,
            UserType = "Host", InstitutionEncryptionKey = "isolated-test-key"
        };
        _institution = await _db.Set<Institution>().Where(x => x.IsActive && x.IsSoftDeleted != true).Select(x => x.Id).FirstAsync();
        _operation = await _db.Operations.Where(x => x.IsActive && x.OperationType == 12).Select(x => x.Id).FirstAsync();
        _modules = new();
        foreach (var pair in new[] { (BulkImportMaster.DeviceMaster, BulkImportConstants.HostDeviceBulkModuleCode),
            (BulkImportMaster.InstitutionCard, BulkImportConstants.HostCardBulkModuleCode),
            (BulkImportMaster.HostModule, BulkImportConstants.HostModuleBulkModuleCode),
            (BulkImportMaster.HostSubModule, BulkImportConstants.HostSubModuleBulkModuleCode),
            (BulkImportMaster.HostOperation, BulkImportConstants.HostOperationBulkModuleCode),
            (BulkImportMaster.HostModuleOperation, BulkImportConstants.HostModuleOperationBulkModuleCode) })
        {
            var module = await _db.Modules.SingleAsync(x => x.ModuleCode == pair.Item2);
            _modules[pair.Item1] = module.Id;
            var grant = new HostRoleModuleAndPermission
            {
                HostRoleId = user.HostRoleId, ModuleId = module.Id, OperationId = _operation,
                IsActive = true, AddedById = user.Id, AddedDateTime = DateTime.UtcNow
            };
            _db.Add(grant);
            await _db.SaveChangesAsync();
            _grants.Add(grant.Id);
        }
    }

    [TearDown]
    public async Task Cleanup()
    {
        if (_db is null || _prefix is null)
        {
            return;
        }
        _db.ChangeTracker.Clear();
        await _db.Set<BulkImportJob>().Where(x => _jobs.Contains(x.Id)).ExecuteDeleteAsync();
        await _db.DeviceMasters.Where(x => x.DeviceCode.StartsWith(_prefix)).ExecuteDeleteAsync();
        await _db.InstitutionCardMasters.Where(x => x.CardReference != null && x.CardReference.StartsWith(_prefix)).ExecuteDeleteAsync();
        var testModules = await _db.Modules.Where(x => x.ModuleCode != null && x.ModuleCode.StartsWith(_prefix))
            .Select(x => x.Id).ToListAsync();
        var testOperations = await _db.Operations.Where(x => x.OperationName.StartsWith(_prefix))
            .Select(x => x.Id).ToListAsync();
        await _db.ModuleOperationMappings.Where(x => testModules.Contains(x.ModuleId) || testOperations.Contains(x.OperationId))
            .ExecuteDeleteAsync();
        await _db.Set<HostRoleModuleAndPermission>().Where(x => _grants.Contains(x.Id)).ExecuteDeleteAsync();
        await _db.Modules.Where(x => testModules.Contains(x.Id)).ExecuteDeleteAsync();
        await _db.Operations.Where(x => testOperations.Contains(x.Id)).ExecuteDeleteAsync();
        await _db.DisposeAsync();
        _db = null!;
        _jobs.Clear();
        _grants.Clear();
    }

    [Test]
    public async Task Device_confirm_is_idempotent_worker_inserts_catalogue_only_and_reports_existing()
    {
        var assignments = await _db.InstitutionDevices.CountAsync();
        var input = DeviceInput();
        var preview = await Draft(BulkImportMaster.DeviceMaster, input);
        Assert.That(preview.CanCommit, Is.True);
        await Act(preview, BulkImportAction.Confirm);
        await Act(preview, BulkImportAction.Confirm);
        await _repo.ProcessNextBatchAsync(default);
        var job = await Act(preview, BulkImportAction.Get);
        Assert.Multiple(() =>
        {
            Assert.That(job.Status, Is.EqualTo(BulkImportJobStatus.Completed));
            Assert.That(job.CreatedCount, Is.EqualTo(1));
            Assert.That(job.Preview!.Rows[0].HostRecordId, Is.GreaterThan(0));
        });
        Assert.That(await _db.InstitutionDevices.CountAsync(), Is.EqualTo(assignments));
        var entity = await _db.DeviceMasters.SingleAsync(x => x.DeviceCode == _prefix);
        Assert.That(entity.IsOccupied, Is.False);
        var repeated = await Draft(BulkImportMaster.DeviceMaster, input);
        Assert.That(repeated.ExistingCount, Is.EqualTo(1));
        await Act(repeated, BulkImportAction.Confirm);
        await _repo.ProcessNextBatchAsync(default);
        Assert.That((await Act(repeated, BulkImportAction.Get)).ExistingCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Card_snapshot_is_encrypted_public_results_masked_and_procurement_reconciles()
    {
        var number = "000" + DateTime.UtcNow.Ticks.ToString()[^13..];
        var preview = await Draft(BulkImportMaster.InstitutionCard,
            $"CardNumber,CardReference,UnitPurchasePriceExcludingTax,CgstAmount,PurchaseInvoiceDate\n{number},{_prefix},100,9,2026-09-12");
        var publicJson = JsonSerializer.Serialize(preview);
        Assert.That(publicJson, Does.Not.Contain(number).And.Not.Contain("CardCiphertext").And.Not.Contain("CardLookupHash"));
        var stored = await _db.Set<BulkImportJob>().AsNoTracking().SingleAsync(x => x.Id == preview.JobId);
        Assert.That(stored.PreviewJson, Does.Not.Contain(number));
        Assert.That(stored.PreviewJson, Does.Contain("CardCiphertext"));
        await Act(preview, BulkImportAction.Confirm);
        await _repo.ProcessNextBatchAsync(default);
        var job = await Act(preview, BulkImportAction.Get);
        Assert.That(job.CreatedCount, Is.EqualTo(1), JsonSerializer.Serialize(job));
        var entity = await _db.InstitutionCardMasters.SingleAsync(x => x.CardReference == _prefix);
        Assert.Multiple(() =>
        {
            Assert.That(entity.InstitutionId, Is.EqualTo(_institution));
            Assert.That(entity.LandedCost, Is.EqualTo(109));
            Assert.That(_encryption.Decrypt(entity.CardNumberEncrypted, _host.InstitutionEncryptionKey), Is.EqualTo(number));
            Assert.That(entity.Id, Is.EqualTo(job.Preview!.Rows[0].HostRecordId));
            Assert.That(JsonSerializer.Serialize(job), Does.Not.Contain(number).And.Not.Contain("CardCiphertext"));
            Assert.That(System.Text.Encoding.UTF8.GetString(skolenza.api.Common.BulkImportReport.Create(job)), Does.Not.Contain(number));
        });
    }

    [Test]
    public async Task Duplicate_upload_rows_and_missing_mapping_block_confirmation()
    {
        var input = DeviceInput();
        var preview = await Draft(BulkImportMaster.DeviceMaster, input + "\n" + input.Split('\n')[1]);
        Assert.That(preview.InvalidCount, Is.EqualTo(1));
        Assert.ThrowsAsync<ValidationErrorException>(async () => await Act(preview, BulkImportAction.Confirm));
        var missing = await Draft(BulkImportMaster.DeviceMaster, "Unknown\nModel");
        Assert.That(missing.Errors, Has.Count.EqualTo(6));
        Assert.That(missing.CanCommit, Is.False);
    }

    [Test]
    public async Task Job_ownership_cannot_cross_host_or_institution_and_cancel_prevents_insertion()
    {
        var preview = await Draft(BulkImportMaster.DeviceMaster, DeviceInput());
        var foreign = Owner(preview.Master);
        foreign.LoggedInStaffMemberId++;
        Assert.ThrowsAsync<NotFoundException>(async () => await _repo.ActAsync(preview.Master,
            BulkImportAction.Get, Request(preview), foreign, default));
        foreign = Owner(preview.Master);
        foreign.InstitutionId = _institution;
        Assert.ThrowsAsync<NotFoundException>(async () => await _repo.ActAsync(preview.Master,
            BulkImportAction.Get, Request(preview), foreign, default));
        await Act(preview, BulkImportAction.Confirm);
        await Act(preview, BulkImportAction.Cancel);
        await _repo.ProcessNextBatchAsync(default);
        Assert.That((await Act(preview, BulkImportAction.Get)).Status, Is.EqualTo(BulkImportJobStatus.Cancelled));
        Assert.That(await _db.DeviceMasters.AnyAsync(x => x.DeviceCode == _prefix), Is.False);
    }

    [Test]
    public async Task Worker_rechecks_revoked_host_permission_then_retry_succeeds()
    {
        var preview = await Draft(BulkImportMaster.DeviceMaster, DeviceInput());
        await Act(preview, BulkImportAction.Confirm);
        await _db.Set<HostRoleModuleAndPermission>().Where(x => _grants.Contains(x.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false));
        await _repo.ProcessNextBatchAsync(default);
        Assert.That((await Act(preview, BulkImportAction.Get)).Status, Is.EqualTo(BulkImportJobStatus.Failed));
        Assert.That(await _db.DeviceMasters.AnyAsync(x => x.DeviceCode == _prefix), Is.False);
        await _db.Set<HostRoleModuleAndPermission>().Where(x => _grants.Contains(x.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, true));
        await Act(preview, BulkImportAction.Retry);
        await _repo.ProcessNextBatchAsync(default);
        Assert.That((await Act(preview, BulkImportAction.Get)).CreatedCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Cancellation_after_one_batch_keeps_only_committed_rows()
    {
        var first = DeviceInput();
        var second = first.Split('\n')[1].Replace(_prefix, _prefix + "-second");
        var preview = await Draft(BulkImportMaster.DeviceMaster, first + "\n" + second);
        await Act(preview, BulkImportAction.Confirm);
        await _repo.ProcessNextBatchAsync(default);
        Assert.That((await Act(preview, BulkImportAction.Get)).Status, Is.EqualTo(BulkImportJobStatus.Running));
        await Act(preview, BulkImportAction.Cancel);
        await _repo.ProcessNextBatchAsync(default);
        var result = await Act(preview, BulkImportAction.Get);
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(BulkImportJobStatus.Cancelled));
            Assert.That(result.CreatedCount, Is.EqualTo(1));
            Assert.That(result.ProcessedRows, Is.EqualTo(1));
        });
        Assert.That(await _db.DeviceMasters.CountAsync(x => x.DeviceCode.StartsWith(_prefix)), Is.EqualTo(1));
    }

    [Test]
    public async Task Changed_institution_fails_card_row_then_retry_uses_protected_snapshot()
    {
        var number = "001" + DateTime.UtcNow.Ticks.ToString()[^13..];
        var preview = await Draft(BulkImportMaster.InstitutionCard, $"CardNumber,CardReference\n{number},{_prefix}");
        await Act(preview, BulkImportAction.Confirm);
        try
        {
            await _db.Set<Institution>().Where(x => x.Id == _institution).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false));
            await _repo.ProcessNextBatchAsync(default);
            var failed = await Act(preview, BulkImportAction.Get);
            Assert.That(failed.Status, Is.EqualTo(BulkImportJobStatus.CompletedWithErrors));
            Assert.That(failed.FailedCount, Is.EqualTo(1));
        }
        finally
        {
            await _db.Set<Institution>().Where(x => x.Id == _institution).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, true));
        }
        await Act(preview, BulkImportAction.Retry);
        await _repo.ProcessNextBatchAsync(default);
        Assert.That((await Act(preview, BulkImportAction.Get)).CreatedCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Invalid_card_private_values_are_masked_and_field_lengths_block_preview()
    {
        var number = "12345678901234567890";
        var card = await Draft(BulkImportMaster.InstitutionCard, $"CardNumber\n{number}");
        Assert.That(card.InvalidCount, Is.EqualTo(1));
        var saved = await _db.Set<BulkImportJob>().AsNoTracking().SingleAsync(x => x.Id == card.JobId);
        Assert.That(saved.PreviewJson, Does.Not.Contain(number));
        var device = await Draft(BulkImportMaster.DeviceMaster,
            DeviceInput().Replace("Test device", new string('d', 1001)));
        Assert.That(device.InvalidCount, Is.EqualTo(1));
        Assert.That(device.CanCommit, Is.False);
    }

    [Test]
    public async Task Catalogue_imports_create_parent_child_operation_and_mapping_then_skip_replay()
    {
        var operationType = 800000 + Math.Abs(_prefix.GetHashCode() % 100000);
        var parent = await Complete(BulkImportMaster.HostModule,
            $"ModuleCode,ModuleName,PageName,ModuleScope,IsActive\n{_prefix},{_prefix},{_prefix.ToLowerInvariant()},2,true");
        var childCode = _prefix + "-child";
        var child = await Complete(BulkImportMaster.HostSubModule,
            $"ParentModuleCode,ModuleCode,ModuleName,PageName,ModuleScope,IsActive\n{_prefix},{childCode},{childCode},{childCode.ToLowerInvariant()},2,true");
        var operation = await Complete(BulkImportMaster.HostOperation,
            $"OperationName,OperationType,IsActive\n{_prefix},{operationType},true");
        var mapping = await Complete(BulkImportMaster.HostModuleOperation,
            $"ModuleCode,OperationType,IsOperational,IsActive\n{childCode},{operationType},true,true");

        Assert.Multiple(() =>
        {
            Assert.That(parent.CreatedCount, Is.EqualTo(1));
            Assert.That(child.CreatedCount, Is.EqualTo(1));
            Assert.That(operation.CreatedCount, Is.EqualTo(1));
            Assert.That(mapping.CreatedCount, Is.EqualTo(1));
        });
        var parentEntity = await _db.Modules.SingleAsync(x => x.ModuleCode == _prefix);
        var childEntity = await _db.Modules.SingleAsync(x => x.ModuleCode == childCode);
        var operationEntity = await _db.Operations.SingleAsync(x => x.OperationType == operationType);
        Assert.Multiple(() =>
        {
            Assert.That(parentEntity.ParentModuleId, Is.Null);
            Assert.That(parentEntity.PageName, Is.EqualTo(_prefix.ToLowerInvariant()));
            Assert.That(childEntity.ParentModuleId, Is.EqualTo(parentEntity.Id));
            Assert.That(operationEntity.OperationName, Is.EqualTo(_prefix));
            Assert.That(_db.ModuleOperationMappings.Any(x => x.ModuleId == childEntity.Id &&
                x.OperationId == operationEntity.Id), Is.True);
        });
        var replay = await Draft(BulkImportMaster.HostOperation,
            $"OperationName,OperationType,IsActive\n{_prefix},{operationType},true");
        Assert.That(replay.ExistingCount, Is.EqualTo(1));
    }

    private async Task<BulkImportJobResponseDTO> Complete(BulkImportMaster master, string input)
    {
        var preview = await Draft(master, input);
        Assert.That(preview.CanCommit, Is.True, string.Join("; ", preview.Errors.Concat(preview.Rows.SelectMany(x => x.Errors))));
        await Act(preview, BulkImportAction.Confirm);
        await _repo.ProcessNextBatchAsync(default);
        return await Act(preview, BulkImportAction.Get);
    }

    private string DeviceInput()
    {
        var type = Enum.GetValues<skolenza.domain.Entity.DeviceType>().First();
        return $"SNo,DeviceCode,DeviceName,CompanyName,ModelNo,DeviceType\n{_prefix},{_prefix},Test device,{_prefix},Test model,{type}";
    }

    [TestCase(BulkImportMaster.DeviceMaster)]
    [TestCase(BulkImportMaster.InstitutionCard)]
    public async Task Authenticated_HTTP_upload_confirm_worker_report_roundtrip(BulkImportMaster master)
    {
        var mapper = new MapperConfiguration(config => config.AddProfile<MappingProfile>()).CreateMapper();
        var store = new StoreProcedureRepository(_db, NullLogger<StoreProcedureRepository>.Instance, mapper);
        var common = Proxy<ICommonRequestService>((method, _) => method.Name == "ValidateHostUserPermissionRequestAsync"
            ? Task.FromResult(_host) : throw new AssertionException(method.Name));
        var modules = Proxy<IModuleRepository>((_, args) => _db.Modules.SingleOrDefaultAsync(x => x.Id == Convert.ToInt32(args![0])));
        var operations = Proxy<IOperationRepository>((_, args) => _db.Operations.SingleOrDefaultAsync(x => x.Id == (int)args![0]!));
        var unit = Proxy<IUnitOfWork>((method, _) => method.Name switch
        {
            "get_ModuleRepository" => modules,
            "get_OperationRepository" => operations,
            "get_StoreProcedureRepository" => store,
            _ => throw new AssertionException(method.Name)
        });
        var ids = Proxy<IIdEncoderService>((method, args) =>
            method.Name == "DecodeId_long" && (string)args![0]! == "selected-institution"
                ? _institution : throw new AssertionException("Unexpected institution identifier."));
        var workflow = new HostBulkImportWorkflowService(_repo, common, unit, ids);
        var mediator = Proxy<IMediator>((_, args) => args![0] switch
        {
            PreviewHostBulkImportQuery query => new PreviewHostBulkImportQueryHandler(workflow).Handle(query, default),
            ManageHostBulkImportCommand command => new ManageHostBulkImportCommandHandler(workflow).Handle(command, default),
            _ => throw new AssertionException("Unexpected request.")
        });
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("host-bulk-isolated-http-test-key-32-bytes"));
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(mediator);
        builder.Services.AddControllers().AddApplicationPart(typeof(skolenza.api.Controllers.HostDevice.DeviceMasterBulkImportController).Assembly);
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false, ValidateAudience = false, ValidateLifetime = true,
                ValidateIssuerSigningKey = true, IssuerSigningKey = key
            };
        });
        builder.Services.AddAuthorization();
        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        var prefix = master == BulkImportMaster.DeviceMaster
            ? "/api/DeviceMaster/import/" : "/api/InstitutionCardMaster/import/";
        Assert.That((await client.GetAsync(prefix + "template")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        var token = new JwtSecurityToken(claims: new[] { new Claim("sub", _host.HostUserId.ToString()) },
            expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        var jobId = Guid.NewGuid();
        _jobs.Add(jobId);
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(jobId.ToString()), "RequestId");
        form.Add(new StringContent(_modules[master].ToString()), "ModuleId");
        form.Add(new StringContent(_operation.ToString()), "OperationId");
        var input = master == BulkImportMaster.DeviceMaster ? DeviceInput()
            : $"CardNumber,CardReference\n000{DateTime.UtcNow.Ticks.ToString()[^13..]},{_prefix}";
        if (master == BulkImportMaster.InstitutionCard)
        {
            form.Add(new StringContent("selected-institution"), "InstitutionId");
        }
        form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(input)), "File", "import.csv");
        var upload = await client.PostAsync(prefix + "preview", form);
        Assert.That(upload.StatusCode, Is.EqualTo(HttpStatusCode.OK), await upload.Content.ReadAsStringAsync());
        var preview = (await upload.Content.ReadFromJsonAsync<ApiResponse<BulkImportPreviewResponseDTO>>())!.Data!;
        Assert.That(preview.CanCommit, Is.True);
        var request = new HostBulkImportJobRequestDTO
        {
            JobId = jobId, ModuleId = _modules[master], OperationId = _operation,
            InstitutionId = master == BulkImportMaster.InstitutionCard ? "selected-institution" : null
        };
        var confirm = await client.PostAsJsonAsync(prefix + "confirm", request);
        Assert.That(confirm.StatusCode, Is.EqualTo(HttpStatusCode.OK), await confirm.Content.ReadAsStringAsync());
        _db.ChangeTracker.Clear();
        await _repo.ProcessNextBatchAsync(default);
        var query = $"?JobId={jobId}&ModuleId={request.ModuleId}&OperationId={request.OperationId}&InstitutionId={request.InstitutionId}";
        var jobResponse = await client.GetFromJsonAsync<ApiResponse<BulkImportJobResponseDTO>>(prefix + "job" + query);
        Assert.That(jobResponse!.Data!.CreatedCount, Is.EqualTo(1));
        var report = await client.GetStringAsync(prefix + "report" + query);
        Assert.That(report, Does.Contain("Created").And.Contain(_prefix));
        var inserted = master == BulkImportMaster.DeviceMaster
            ? await _db.DeviceMasters.CountAsync(x => x.DeviceCode == _prefix)
            : await _db.InstitutionCardMasters.CountAsync(x => x.CardReference == _prefix && x.InstitutionId == _institution);
        Assert.That(inserted, Is.EqualTo(1));
        await app.StopAsync();
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

    private CommonDecodedResult Owner(BulkImportMaster master) => new()
    {
        Success = true, LoggedInStaffMemberId = _host.HostUserId, RoleId = (int)_host.TokenHostRoleId,
        InstitutionId = master == BulkImportMaster.InstitutionCard ? _institution : 0
    };

    private async Task<BulkImportPreviewResponseDTO> Draft(BulkImportMaster master, string text)
    {
        var request = new BulkImportPreviewRequestDTO
        {
            RequestId = Guid.NewGuid(), ModuleId = _modules[master], OperationId = _operation, PastedText = text
        };
        _jobs.Add(request.RequestId.Value);
        var table = await BulkImportTableReader.ReadAsync(request, default);
        var preview = await _repo.PreviewHostAsync(master, table, null, Owner(master).InstitutionId, _host, default);
        return await _repo.SaveDraftAsync(preview, request, Owner(master), default);
    }

    private BulkImportJobRequestDTO Request(BulkImportPreviewResponseDTO preview) => new()
    {
        JobId = preview.JobId!.Value, ModuleId = _modules[preview.Master], OperationId = _operation
    };

    private Task<BulkImportJobResponseDTO> Act(BulkImportPreviewResponseDTO preview, BulkImportAction action)
    {
        _db.ChangeTracker.Clear();
        return _repo.ActAsync(preview.Master, action, Request(preview), Owner(preview.Master), default);
    }
}
