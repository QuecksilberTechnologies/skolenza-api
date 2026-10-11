using System.Collections.Concurrent;
using System.Data.Common;
using AutoMapper;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.Common.Models;
using skolenza.application.Common.Models.Security;
using skolenza.application.Constants;
using skolenza.application.DTOs.Department;
using skolenza.application.DTOs.Designation;
using skolenza.application.DTOs.Role;
using skolenza.application.DTOS.Common;
using skolenza.application.Exceptions;
using skolenza.application.Mappings;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using skolenza.persistance.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using skolenza.application.Interfaces.IRepositories;
using skolenza.infrastructure.BackgroundJob;
using Npgsql;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

/// <summary>End-to-end repository jobs using real PostgreSQL, existing mappings and the real permission function.</summary>
[TestFixture]
[Category("BulkImportDatabase")]
[NonParallelizable]
public sealed class DurableBulkImportDatabaseTests
{
    private string _connection = null!;
    private string _prefix = null!;
    private readonly ConcurrentBag<Guid> _jobs = new();
    private IMapper _mapper = null!;
    private CommonDecodedResult _actor = null!;
    private Dictionary<BulkImportMaster, int> _modules = null!;
    private int _operation;

    [SetUp]
    public async Task Setup()
    {
        _connection = Environment.GetEnvironmentVariable("SKOLENZA_BULK_TEST_CONNECTION")!;
        if (string.IsNullOrWhiteSpace(_connection))
        {
            Assert.Ignore("Requires the isolated skolenza_bulk_test backup clone with both bulk migrations applied.");
        }
        Assert.That(new NpgsqlConnectionStringBuilder(_connection).Database, Is.EqualTo("skolenza_bulk_test"));
        _prefix = "BulkJobTest-" + Guid.NewGuid().ToString("N")[..12];
        _mapper = new MapperConfiguration(config => config.AddProfile<MappingProfile>()).CreateMapper();
        await using var context = Context();
        _operation = await context.Operations.Where(item => item.OperationType == (int)OperationType.Add && item.IsActive)
            .OrderBy(item => item.Id).Select(item => item.Id).FirstAsync();
        _modules = new Dictionary<BulkImportMaster, int>();
        foreach (var pair in new[]
        {
            (BulkImportMaster.Department, "DEPARTMENT"),
            (BulkImportMaster.Designation, "DESIGNATION"),
            (BulkImportMaster.Role, "ROLE")
        })
        {
            _modules[pair.Item1] = await context.Modules.Where(item => item.ModuleCode == pair.Item2)
                .Select(item => item.Id).SingleAsync();
        }
        var candidates = await (from staffMember in context.StaffMembers
            join role in context.UserRoles on staffMember.Id equals role.StaffMemberId
            where staffMember.InstitutionId != null && staffMember.IsActive && !staffMember.IsSoftDeleted && role.IsActive && role.RoleId != null
            select new { InstitutionId = staffMember.InstitutionId!.Value, StaffMemberId = staffMember.Id, RoleId = role.RoleId!.Value }).ToListAsync();
        var permissions = new StoreProcedureRepository(context, NullLogger<StoreProcedureRepository>.Instance, _mapper);
        foreach (var candidate in candidates)
        {
            var granted = true;
            foreach (var moduleId in _modules.Values)
            {
                var result = await permissions.CheckInstitutionStaffMemberPermissionAsync(candidate.InstitutionId,
                    candidate.StaffMemberId, candidate.RoleId, moduleId, _operation);
                granted &= result.ResultCode == 1;
            }
            if (granted)
            {
                _actor = new CommonDecodedResult
                {
                    Success = true,
                    InstitutionId = candidate.InstitutionId,
                    LoggedInStaffMemberId = candidate.StaffMemberId,
                    RoleId = candidate.RoleId
                };
                break;
            }
        }
        Assert.That(_actor, Is.Not.Null, "The isolated backup must contain a permitted institution admin fixture.");
    }

    [TearDown]
    public async Task Cleanup()
    {
        if (string.IsNullOrWhiteSpace(_connection) || _prefix is null)
        {
            return;
        }
        await using var context = Context();
        var ids = _jobs.ToArray();
        await context.Set<BulkImportJob>().Where(job => ids.Contains(job.Id)).ExecuteDeleteAsync();
        await context.Designations.Where(item => item.DesignationName.StartsWith(_prefix)).ExecuteDeleteAsync();
        await context.Departments.Where(item => item.DepartmentName.StartsWith(_prefix)).ExecuteDeleteAsync();
        await context.Roles.Where(item => item.RoleName!.StartsWith(_prefix)).ExecuteDeleteAsync();
        await context.StaffEmploymentTypes.Where(item => item.TypeName!.StartsWith(_prefix)).ExecuteDeleteAsync();
        _jobs.Clear();
        _actor = null!;
    }

    [Test]
    public async Task Hosted_worker_processes_confirmed_job_using_fresh_scopes()
    {
        var preview = await Draft(BulkImportMaster.Department, $"DepartmentName\n{_prefix}-IT");
        await Act(preview, BulkImportAction.Confirm);
        var services = new ServiceCollection();
        services.AddScoped(_ => Context());
        services.AddSingleton(_mapper);
        services.AddScoped<IStoreProcedureRepository>(provider => new StoreProcedureRepository(
            provider.GetRequiredService<SkolenzaDbContext>(), NullLogger<StoreProcedureRepository>.Instance, _mapper));
        var settings = Options.Create(new BulkImportOptions
        {
            WorkerEnabled = true, BatchSize = 1, PollIntervalSeconds = 1, BatchTimeoutSeconds = 10
        });
        services.AddSingleton<IOptions<BulkImportOptions>>(settings);
        services.AddScoped<IBulkImportRepository, BulkImportRepository>();
        await using var provider = services.BuildServiceProvider();
        using var worker = new BulkImportWorker(provider.GetRequiredService<IServiceScopeFactory>(),
            settings, NullLogger<BulkImportWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        try
        {
            for (var attempt = 0; attempt < 50; attempt++)
            {
                var result = await Act(preview, BulkImportAction.Get);
                if (result.Status == BulkImportJobStatus.Completed)
                {
                    Assert.That(result.CreatedCount, Is.EqualTo(1));
                    return;
                }
                await Task.Delay(100);
            }
            Assert.Fail("Hosted worker did not complete the queued job.");
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public async Task Confirm_and_restart_process_all_rows_exactly_once()
    {
        var preview = await Draft(BulkImportMaster.Department, $"DepartmentName\n{_prefix}-IT\n{_prefix}-HR\n{_prefix}-Sales");
        Assert.That(preview.CanCommit, Is.True);
        await Act(preview, BulkImportAction.Confirm);
        await Act(preview, BulkImportAction.Confirm);
        await Tick();
        var running = await Act(preview, BulkImportAction.Get);
        Assert.That(running.CreatedCount, Is.EqualTo(1));
        // Every tick uses a fresh scope/context, like resuming after a server restart.
        await Drain(preview);
        var completed = await Act(preview, BulkImportAction.Confirm);
        Assert.Multiple(() =>
        {
            Assert.That(completed.Status, Is.EqualTo(BulkImportJobStatus.Completed));
            Assert.That(completed.CreatedCount, Is.EqualTo(3));
            Assert.That(completed.Preview!.CanCommit, Is.False);
        });
        await using var context = Context();
        Assert.That(await context.Departments.CountAsync(item => item.DepartmentName.StartsWith(_prefix)), Is.EqualTo(3));
    }

    [Test]
    public async Task Active_role_import_reactivates_current_record_without_overwriting_fields()
    {
        var roleName = $"{_prefix}-Inactive";
        await using (var arrange = Context())
        {
            arrange.Roles.Add(new Role
            {
                InstitutionId = _actor.InstitutionId,
                RoleName = roleName,
                RoleType = ConstantValues.RoleTypeStaffMember,
                Remark = "keep-existing-remark",
                IsActive = false,
                IsSoftDeleted = false,
                IsSystemDefault = false,
                AddedById = _actor.LoggedInStaffMemberId,
                AddedDateTime = DateTime.UtcNow
            });
            await arrange.SaveChangesAsync();
        }

        var preview = await Draft(BulkImportMaster.Role,
            $"RoleName,RoleType,Remark,IsActive\n{roleName},{ConstantValues.RoleTypeStaffMember},uploaded-remark,true");
        Assert.That(preview.Rows.Single().WillReactivate, Is.True);
        await Act(preview, BulkImportAction.Confirm);
        await Drain(preview);

        var report = await Act(preview, BulkImportAction.Get);
        await using var verify = Context();
        var role = await verify.Roles.SingleAsync(item =>
            item.InstitutionId == _actor.InstitutionId && item.RoleName == roleName && item.IsSoftDeleted != true);
        Assert.Multiple(() =>
        {
            Assert.That(report.ReactivatedCount, Is.EqualTo(1));
            Assert.That(report.CreatedCount, Is.Zero);
            Assert.That(role.IsActive, Is.True);
            Assert.That(role.Remark, Is.EqualTo("keep-existing-remark"));
        });
    }

    [Test]
    public async Task Same_request_id_reuses_saved_preview_and_rejects_changed_input()
    {
        var id = Guid.NewGuid();
        var text = $"DepartmentName\n{_prefix}-IT";
        var first = await Draft(BulkImportMaster.Department, text, id);
        var repeated = await Draft(BulkImportMaster.Department, text, id);
        Assert.That(repeated.JobId, Is.EqualTo(first.JobId));
        Assert.ThrowsAsync<ConflictException>(async () =>
            await Draft(BulkImportMaster.Department, $"DepartmentName\n{_prefix}-HR", id));
    }

    [Test]
    public async Task Scheduled_job_waits_and_cancel_prevents_inserts()
    {
        var preview = await Draft(BulkImportMaster.Department, $"DepartmentName\n{_prefix}-IT");
        var scheduled = await Act(preview, BulkImportAction.Confirm, DateTimeOffset.UtcNow.AddHours(1));
        Assert.That(scheduled.Status, Is.EqualTo(BulkImportJobStatus.Queued));
        Assert.That(await Tick(), Is.False);
        var cancelled = await Act(preview, BulkImportAction.Cancel);
        Assert.That(cancelled.Status, Is.EqualTo(BulkImportJobStatus.Cancelled));
        Assert.That(await Tick(), Is.False);
    }

    [Test]
    public async Task Cancellation_after_first_batch_keeps_only_committed_rows()
    {
        var preview = await Draft(BulkImportMaster.Department,
            $"DepartmentName\n{_prefix}-IT\n{_prefix}-HR\n{_prefix}-Sales");
        await Act(preview, BulkImportAction.Confirm);
        await Tick();
        var cancelled = await Act(preview, BulkImportAction.Cancel);
        Assert.Multiple(() =>
        {
            Assert.That(cancelled.Status, Is.EqualTo(BulkImportJobStatus.Cancelled));
            Assert.That(cancelled.CreatedCount, Is.EqualTo(1));
        });
        Assert.That(await Tick(), Is.False);
    }

    [Test]
    public async Task Completed_job_cannot_be_cancelled()
    {
        var preview = await Draft(BulkImportMaster.Department,
            $"DepartmentName\n{_prefix}-Completed");
        await Act(preview, BulkImportAction.Confirm);
        await Drain(preview);

        var completed = await Act(preview, BulkImportAction.Get);
        Assert.That(completed.Status, Is.EqualTo(BulkImportJobStatus.Completed));
        Assert.ThrowsAsync<ConflictException>(async () =>
            await Act(preview, BulkImportAction.Cancel));
    }

    [Test]
    public async Task Past_schedule_is_rejected_without_enqueuing()
    {
        var preview = await Draft(BulkImportMaster.Department, $"DepartmentName\n{_prefix}-IT");
        Assert.ThrowsAsync<ValidationErrorException>(async () =>
            await Act(preview, BulkImportAction.Confirm, DateTimeOffset.UtcNow.AddMinutes(-1)));
        Assert.That((await Act(preview, BulkImportAction.Get)).Status, Is.EqualTo(BulkImportJobStatus.Draft));
    }

    [Test]
    public async Task Invalid_preview_cannot_be_confirmed()
    {
        var preview = await Draft(BulkImportMaster.Designation, $"DesignationName,DepartmentName\n{_prefix}-Manager,Unknown");
        Assert.That(preview.CanCommit, Is.False);
        Assert.ThrowsAsync<ValidationErrorException>(async () => await Act(preview, BulkImportAction.Confirm));
    }

    [Test]
    public async Task Foreign_institution_or_actor_cannot_read_or_confirm_a_job()
    {
        var preview = await Draft(BulkImportMaster.Department, $"DepartmentName\n{_prefix}-IT");
        await using var context = Context();
        var foreign = new CommonDecodedResult
        {
            InstitutionId = long.MaxValue, LoggedInStaffMemberId = _actor.LoggedInStaffMemberId, RoleId = _actor.RoleId
        };
        Assert.ThrowsAsync<NotFoundException>(async () => await Repo(context).ActAsync(preview.Master,
            BulkImportAction.Get, Request(preview), foreign, CancellationToken.None));
        foreign.InstitutionId = _actor.InstitutionId;
        foreign.LoggedInStaffMemberId = long.MaxValue;
        Assert.ThrowsAsync<NotFoundException>(async () => await Repo(context).ActAsync(preview.Master,
            BulkImportAction.Confirm, Request(preview), foreign, CancellationToken.None));
    }

    [Test]
    public async Task Parallel_workers_do_not_duplicate_imported_records()
    {
        var preview = await Draft(BulkImportMaster.Department, $"DepartmentName\n{_prefix}-IT\n{_prefix}-HR");
        await Act(preview, BulkImportAction.Confirm);
        await Task.WhenAll(Tick(), Tick());
        await Drain(preview);
        var done = await Act(preview, BulkImportAction.Get);
        Assert.That(done.CreatedCount, Is.EqualTo(2));
    }

    [Test]
    public async Task Crash_before_cursor_commit_rolls_back_business_insert_then_recovers()
    {
        var preview = await Draft(BulkImportMaster.Department, $"DepartmentName\n{_prefix}-IT");
        await Act(preview, BulkImportAction.Confirm);
        await using (var interrupted = Context(new InterruptProgress()))
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await Repo(interrupted).ProcessNextBatchAsync(CancellationToken.None));
        }
        await using (var check = Context())
        {
            Assert.That(await check.Departments.CountAsync(item => item.DepartmentName.StartsWith(_prefix)), Is.Zero);
        }
        await Drain(preview);
        Assert.That((await Act(preview, BulkImportAction.Get)).CreatedCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Permission_rechecked_by_real_function_and_restored_on_retry()
    {
        var preview = await Draft(BulkImportMaster.Department, $"DepartmentName\n{_prefix}-IT");
        await Act(preview, BulkImportAction.Confirm);
        await using (var context = Context())
        {
            await context.Set<BulkImportJob>().Where(job => job.Id == preview.JobId)
                .ExecuteUpdateAsync(update => update.SetProperty(job => job.RoleId, int.MaxValue));
        }
        await Tick();
        Assert.That((await Act(preview, BulkImportAction.Get)).Status, Is.EqualTo(BulkImportJobStatus.Failed));
        await Act(preview, BulkImportAction.Retry);
        await Drain(preview);
        Assert.That((await Act(preview, BulkImportAction.Get)).CreatedCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Designation_dependency_change_fails_then_retries_without_duplicate()
    {
        var parent = await Draft(BulkImportMaster.Department, $"DepartmentName\n{_prefix}-IT\n{_prefix}-HR");
        await Act(parent, BulkImportAction.Confirm);
        await Drain(parent);
        var preview = await Draft(BulkImportMaster.Designation,
            $"DesignationName,DepartmentName\n{_prefix}-Manager,{_prefix}-IT\n{_prefix}-Manager,{_prefix}-HR");
        await Act(preview, BulkImportAction.Confirm);
        await using (var context = Context())
        {
            await context.Departments.Where(item => item.DepartmentName == _prefix + "-HR")
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.IsActive, false));
        }
        await Drain(preview);
        var partial = await Act(preview, BulkImportAction.Get);
        Assert.That(partial.Status, Is.EqualTo(BulkImportJobStatus.CompletedWithErrors));
        Assert.That(partial.CreatedCount, Is.EqualTo(1));
        await using (var context = Context())
        {
            await context.Departments.Where(item => item.DepartmentName == _prefix + "-HR")
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.IsActive, true));
        }
        await Act(preview, BulkImportAction.Retry);
        await Drain(preview);
        Assert.That((await Act(preview, BulkImportAction.Get)).CreatedCount, Is.EqualTo(2));
    }

    [Test]
    public async Task Roles_are_created_without_grants_or_assignments_and_new_upload_skips_existing()
    {
        var text = $"RoleName,RoleType\n{_prefix}-HR,{ConstantValues.RoleTypeStaffMember}";
        var preview = await Draft(BulkImportMaster.Role, text);
        await Act(preview, BulkImportAction.Confirm);
        await Drain(preview);
        await using (var context = Context())
        {
            var role = await context.Roles.SingleAsync(item => item.RoleName == _prefix + "-HR");
            Assert.That(role.IsSystemDefault, Is.False);
            Assert.That(await context.UserRoles.AnyAsync(item => item.RoleId == role.Id), Is.False);
            Assert.That(await context.RoleModuleAndPermissions.AnyAsync(item => item.RoleId == role.Id), Is.False);
        }
        var again = await Draft(BulkImportMaster.Role, text);
        Assert.That(again.ExistingCount, Is.EqualTo(1));
        await Act(again, BulkImportAction.Confirm);
        await Drain(again);
        Assert.That((await Act(again, BulkImportAction.Get)).CreatedCount, Is.Zero);
    }

    [Test]
    public async Task History_returns_summaries_and_excludes_other_actors_and_masters()
    {
        var draft = await Draft(BulkImportMaster.Department, $"DepartmentName\n{_prefix}-History");
        await using var context = Context();
        var request = new BulkImportJobRequestDTO { PageNumber = 1, PageSize = 100 };
        var own = await Repo(context).ListAsync(BulkImportMaster.Department, request, _actor, CancellationToken.None);
        var saved = own.Single(item => item.JobId == draft.JobId);
        Assert.That(saved.Preview, Is.Null);
        Assert.That(saved.TotalRows, Is.EqualTo(1));
        var foreignActor = new CommonDecodedResult
        {
            Success = true,
            InstitutionId = _actor.InstitutionId,
            LoggedInStaffMemberId = long.MaxValue,
            RoleId = _actor.RoleId
        };
        var foreign = await Repo(context).ListAsync(
            BulkImportMaster.Department, request, foreignActor, CancellationToken.None);
        var otherMaster = await Repo(context).ListAsync(
            BulkImportMaster.Role, request, _actor, CancellationToken.None);
        Assert.That(foreign.Any(item => item.JobId == draft.JobId), Is.False);
        Assert.That(otherMaster.Any(item => item.JobId == draft.JobId), Is.False);
    }

    [Test]
    public async Task StaffEmploymentType_import_uses_real_permissions_and_preserves_existing_matches()
    {
        await using var setup = Context();
        var module = await setup.Modules.SingleAsync(item => item.ModuleCode == BulkImportConstants.StaffEmploymentTypeModuleCode);
        _modules[BulkImportMaster.StaffEmploymentType] = module.Id;
        var grant = new RoleModuleAndPermission
        {
            Id = await setup.RoleModuleAndPermissions.MaxAsync(item => item.Id) + 1,
            RoleId = _actor.RoleId, ModuleId = module.Id, OperationId = _operation,
            IsActive = true, HasAccess = true, IsOperational = true,
            AddedById = _actor.LoggedInStaffMemberId, AddedDateTime = DateTime.UtcNow, Remark = _prefix
        };
        var enabled = new InstitutionEnabledModule
        {
            Id = await setup.InstitutionEnabledModules.MaxAsync(item => item.Id) + 1,
            InstitutionId = _actor.InstitutionId, ModuleId = module.Id, ParentModuleId = module.ParentModuleId,
            IsEnabled = true, IsLeafNode = true, AddedById = _actor.LoggedInStaffMemberId, AddedDateTime = DateTime.UtcNow
        };
        setup.RoleModuleAndPermissions.Add(grant);
        setup.InstitutionEnabledModules.Add(enabled);
        await setup.SaveChangesAsync();
        try
        {
            var preview = await Draft(BulkImportMaster.StaffEmploymentType,
                $"TypeName,Description,Remark,IsActive\n{_prefix}-Seasonal,Seasonal work,HR defined,true");
            await Act(preview, BulkImportAction.Confirm);
            await Drain(preview);
            var result = await Act(preview, BulkImportAction.Get);
            Assert.That(result.Status, Is.EqualTo(BulkImportJobStatus.Completed), result.Error);
            Assert.That(result.CreatedCount, Is.EqualTo(1));
            var created = await setup.StaffEmploymentTypes.AsNoTracking().SingleAsync(item => item.TypeName == _prefix + "-Seasonal");
            Assert.That(created.InstitutionId, Is.EqualTo(_actor.InstitutionId));
            Assert.That(created.Description, Is.EqualTo("Seasonal work"));
            var again = await Draft(BulkImportMaster.StaffEmploymentType, $"TypeName\n{_prefix}-Seasonal");
            await Act(again, BulkImportAction.Confirm);
            await Drain(again);
            Assert.That((await Act(again, BulkImportAction.Get)).CreatedCount, Is.Zero);
            Assert.That((await Act(again, BulkImportAction.Get)).ExistingCount, Is.EqualTo(1));
        }
        finally
        {
            await setup.RoleModuleAndPermissions.Where(item => item.Id == grant.Id).ExecuteDeleteAsync();
            await setup.InstitutionEnabledModules.Where(item => item.Id == enabled.Id).ExecuteDeleteAsync();
        }
    }

    [TestCase("Development")]
    [TestCase("Production")]
    public async Task Environment_migration_runner_is_rerunnable_without_duplicate_types_or_module_rows(string environment)
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "database-scripts", "ApplyBulkImportMigrations.ps1")))
        {
            root = root.Parent;
        }
        Assert.That(root, Is.Not.Null);
        var psql = Environment.GetEnvironmentVariable("SKOLENZA_TEST_PSQL_PATH")
            ?? "C:/Program Files/PostgreSQL/18/bin/psql.exe";
        Assert.That(File.Exists(psql), Is.True, "Set SKOLENZA_TEST_PSQL_PATH to the installed psql executable.");
        await using var context = Context();
        var typeCount = await context.StaffEmploymentTypes.CountAsync();
        var moduleCount = await context.Modules.CountAsync();
        var start = new System.Diagnostics.ProcessStartInfo("pwsh")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in new[] { "-NoProfile", "-File", Path.Combine(root!.FullName,
            "database-scripts", "ApplyBulkImportMigrations.ps1"), "-Environment", environment, "-PsqlPath", psql })
        {
            start.ArgumentList.Add(argument);
        }
        // Even the Production selection is redirected explicitly to the isolated fixture database.
        start.Environment["ConnectionStrings__DefaultConnection"] = _connection;
        using var process = System.Diagnostics.Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(60));
        Assert.That(process.ExitCode, Is.Zero, await errors);
        Assert.That(await output, Does.Contain("Bulk migrations complete"));
        Assert.That(await context.StaffEmploymentTypes.CountAsync(), Is.EqualTo(typeCount));
        Assert.That(await context.Modules.CountAsync(), Is.EqualTo(moduleCount));
    }

    private SkolenzaDbContext Context(IInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<SkolenzaDbContext>().UseNpgsql(_connection);
        if (interceptor is not null)
        {
            builder.AddInterceptors(interceptor);
        }
        return new SkolenzaDbContext(builder.Options);
    }

    private BulkImportRepository Repo(SkolenzaDbContext context)
    {
        return new BulkImportRepository(context, _mapper,
            new StoreProcedureRepository(context, NullLogger<StoreProcedureRepository>.Instance, _mapper),
            Options.Create(new BulkImportOptions { BatchSize = 1 }));
    }

    private async Task<BulkImportPreviewResponseDTO> Draft(BulkImportMaster master, string input, Guid? id = null)
    {
        var request = new BulkImportPreviewRequestDTO
        {
            RequestId = id ?? Guid.NewGuid(), PastedText = input,
            ModuleId = _modules[master], OperationId = _operation
        };
        _jobs.Add(request.RequestId.Value);
        await using var context = Context();
        var table = await BulkImportTableReader.ReadAsync(request, CancellationToken.None);
        var departments = await context.Departments.Where(item => item.InstitutionId == _actor.InstitutionId && !item.IsSoftDeleted)
            .Select(item => new GetDepartmentResponseDTO { Id = item.Id, DepartmentName = item.DepartmentName, IsActive = item.IsActive }).ToListAsync();
        var roles = await context.Roles.Where(item => item.InstitutionId == _actor.InstitutionId && item.IsSoftDeleted != true)
            .Select(item => new GetRoleResponseDTO { Id = item.Id, RoleName = item.RoleName!, RoleType = item.RoleType, IsActive = item.IsActive }).ToListAsync();
        var preview = BulkImportPreviewService.Build(master, null, table, departments,
            Array.Empty<GetDesignationResponseDTO>(), roles);
        return await Repo(context).SaveDraftAsync(preview, request, _actor, CancellationToken.None);
    }

    private BulkImportJobRequestDTO Request(BulkImportPreviewResponseDTO preview)
    {
        return new BulkImportJobRequestDTO
        {
            JobId = preview.JobId!.Value, ModuleId = _modules[preview.Master], OperationId = _operation
        };
    }

    private async Task<BulkImportJobResponseDTO> Act(BulkImportPreviewResponseDTO preview,
        BulkImportAction action, DateTimeOffset? scheduled = null)
    {
        await using var context = Context();
        var request = Request(preview);
        request.ScheduledAtUtc = scheduled;
        return await Repo(context).ActAsync(preview.Master, action, request, _actor, CancellationToken.None);
    }

    private async Task<bool> Tick()
    {
        await using var context = Context();
        return await Repo(context).ProcessNextBatchAsync(CancellationToken.None);
    }

    private async Task Drain(BulkImportPreviewResponseDTO preview)
    {
        for (var index = 0; index < 10; index++)
        {
            var job = await Act(preview, BulkImportAction.Get);
            if (job.Status is BulkImportJobStatus.Completed or BulkImportJobStatus.CompletedWithErrors or BulkImportJobStatus.Failed)
            {
                return;
            }
            await Tick();
        }
        Assert.Fail("Import did not finish within the bounded fixture ticks.");
    }

    private sealed class InterruptProgress : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("UPDATE", StringComparison.OrdinalIgnoreCase) &&
                command.CommandText.Contains("BulkImportJob", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Simulated worker crash before durable cursor commit.");
            }
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
