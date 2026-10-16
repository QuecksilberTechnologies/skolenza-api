using AutoMapper;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.Common.Models;
using skolenza.application.Common.Models.Security;
using skolenza.application.Constants;
using skolenza.application.DTOS.Common;
using skolenza.application.Exceptions;
using skolenza.application.Mappings;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using skolenza.persistance.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StaffMemberImportDatabase")]
[NonParallelizable]
public sealed class StaffMemberImportDatabaseTests
{
    private string _connection = null!;
    private string _prefix = null!;
    private IMapper _mapper = null!;
    private CommonDecodedResult _actor = null!;
    private StaffMemberCodePattern _pattern = null!;
    private SubscriptionPlan _plan = null!;
    private Dictionary<string, string> _values = null!;
    private readonly List<Guid> _jobs = new();
    private int _module;
    private int _operation;

    #region Isolated Fixture

    [SetUp]
    public async Task Setup()
    {
        _connection = Environment.GetEnvironmentVariable("SKOLENZA_BULK_TEST_CONNECTION")!;
        if (string.IsNullOrWhiteSpace(_connection))
            Assert.Ignore("Requires disposable skolenza_bulk_test database.");
        Assert.That(new NpgsqlConnectionStringBuilder(_connection).Database, Is.EqualTo("skolenza_bulk_test"));
        _prefix = "emp-import-" + Guid.NewGuid().ToString("N")[..10];
        _mapper = new MapperConfiguration(config => config.AddProfile<MappingProfile>()).CreateMapper();
        await using var context = Context();
        _pattern = await context.StaffMemberCodePatterns.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.InstitutionId).FirstAsync();
        var admin = await (from staffMember in context.StaffMembers join role in context.UserRoles on staffMember.Id equals role.StaffMemberId
            where staffMember.InstitutionId == _pattern.InstitutionId && !staffMember.IsSoftDeleted && staffMember.IsActive && role.IsActive && role.RoleId != null
            select new { staffMember.Id, RoleId = role.RoleId!.Value }).FirstAsync();
        _actor = new CommonDecodedResult { Success = true, InstitutionId = _pattern.InstitutionId, LoggedInStaffMemberId = admin.Id, RoleId = admin.RoleId };
        _module = await context.Modules.Where(item => item.ModuleCode == BulkImportConstants.StaffMemberModuleCode).Select(item => item.Id).SingleAsync();
        _operation = await context.Operations.Where(item => item.OperationType == (int)OperationType.Add && item.IsActive).Select(item => item.Id).FirstAsync();
        var permissions = new StoreProcedureRepository(context, NullLogger<StoreProcedureRepository>.Instance, _mapper);
        Assert.That((await permissions.CheckInstitutionStaffMemberPermissionAsync(_actor.InstitutionId, _actor.LoggedInStaffMemberId,
            _actor.RoleId, _module, _operation)).ResultCode, Is.EqualTo(1), "Fixture needs a real EMP_LIST Add grant.");
        _plan = await context.InstitutionSubscriptions.AsNoTracking().Where(item => item.InstitutionId == _actor.InstitutionId && item.IsActive)
            .Select(item => item.SubscriptionPlan).SingleAsync();
        var position = await context.StaffPositionMasters.Where(item => item.InstitutionId == _actor.InstitutionId && item.IsActive && !item.IsSoftDeleted && item.DepartmentId != null).FirstAsync();
        var staffEmploymentType = await context.StaffEmploymentTypes.Where(item => item.InstitutionId == _actor.InstitutionId && item.IsActive == true && item.IsSoftDeleted != true).FirstAsync();
        _values = StaffMemberImportTests.ValidRow().Values;
        _values["OfficialEmail"] = _prefix + "@example.invalid";
        _values["StaffPositionMasterId"] = position.Id.ToString();
        _values["InstitutionStaffCategoryMasterId"] = position.InstitutionStaffCategoryMasterId.ToString()!;
        _values["DepartmentId"] = position.DepartmentId.ToString();
        _values["StaffEmploymentTypeId"] = staffEmploymentType.Id.ToString();
        _values["CountryId"] = (await context.Countries.Where(item => item.IsActive == true).Select(item => item.Id).FirstAsync()).ToString();
        _values["GenderId"] = (await context.Genders.Select(item => item.Id).FirstAsync()).ToString();
        _values["RoleId"] = (await context.Roles.Where(item => item.InstitutionId == _actor.InstitutionId && item.IsActive && item.IsSoftDeleted != true)
            .Select(item => item.Id).FirstAsync()).ToString();
    }

    [TearDown]
    public async Task Cleanup()
    {
        if (_prefix is null || _pattern is null)
            return;
        await using var context = Context();
        var ids = await context.StaffMembers.Where(item => item.OfficialEmail!.StartsWith(_prefix)).Select(item => item.Id).ToListAsync();
        await context.StaffMemberContacts.Where(item => ids.Contains(item.StaffMemberId)).ExecuteDeleteAsync();
        await context.StaffMemberImages.Where(item => ids.Contains(item.StaffMemberId)).ExecuteDeleteAsync();
        await context.LoginCredentials.Where(item => ids.Contains(item.StaffMemberId)).ExecuteDeleteAsync();
        await context.UserRoles.Where(item => item.StaffMemberId.HasValue && ids.Contains(item.StaffMemberId.Value)).ExecuteDeleteAsync();
        await context.StaffMembers.Where(item => ids.Contains(item.Id)).ExecuteDeleteAsync();
        await context.Set<BulkImportJob>().Where(item => _jobs.Contains(item.Id)).ExecuteDeleteAsync();
        context.StaffMemberCodePatterns.Update(_pattern);
        context.SubscriptionPlans.Update(_plan);
        await context.SaveChangesAsync();
        _jobs.Clear();
    }

    private SkolenzaDbContext Context() => new(new DbContextOptionsBuilder<SkolenzaDbContext>().UseNpgsql(_connection).Options);

    private BulkImportRepository Repository(SkolenzaDbContext context)
    {
        var staffMemberRepository = new BaseStaffMemberRepository(context, _mapper, NullLogger<BaseStaffMemberRepository>.Instance, null!, null!);
        return new BulkImportRepository(context, _mapper,
            new StoreProcedureRepository(context, NullLogger<StoreProcedureRepository>.Instance, _mapper),
            Options.Create(new BulkImportOptions { BatchSize = 20 }), staffMemberRepository);
    }

    private BulkImportTableDTO Table(params Dictionary<string, string>[] rows)
    {
        var fields = rows.SelectMany(row => row.Keys).Distinct().ToList();
        return new BulkImportTableDTO
        {
            Columns = fields,
            Rows = rows.Select((row, index) => new BulkImportSourceRowDTO
                { RowNumber = index + 2, Values = fields.Select(field => row.GetValueOrDefault(field, string.Empty)).ToList() }).ToList()
        };
    }

    private async Task<BulkImportPreviewResponseDTO> Draft(params Dictionary<string, string>[] rows)
    {
        await using var context = Context();
        var repository = Repository(context);
        var preview = await repository.PreviewStaffMembersAsync(Table(rows), null, _actor, CancellationToken.None);
        var request = new BulkImportPreviewRequestDTO { ModuleId = _module, OperationId = _operation, PastedText = Guid.NewGuid().ToString() };
        preview = await repository.SaveDraftAsync(preview, request, _actor, CancellationToken.None);
        _jobs.Add(preview.JobId!.Value);
        return preview;
    }

    private async Task<BulkImportJobResponseDTO> Act(BulkImportPreviewResponseDTO preview, BulkImportAction action, CommonDecodedResult? actor = null)
    {
        await using var context = Context();
        return await Repository(context).ActAsync(BulkImportMaster.StaffMember, action,
            new BulkImportJobRequestDTO { JobId = preview.JobId!.Value, ModuleId = _module, OperationId = _operation }, actor ?? _actor, CancellationToken.None);
    }

    private async Task Work()
    {
        await using var context = Context();
        Assert.That(await Repository(context).ProcessNextBatchAsync(CancellationToken.None), Is.True);
    }

    #endregion

    #region End To End And Validation

    [Test]
    public async Task Invitations_are_claimed_once_failed_delivery_retries_and_public_report_hides_internal_ids()
    {
        var preview = await Draft(_values);
        await Act(preview, BulkImportAction.Confirm);
        await Work();
        var request = new BulkImportJobRequestDTO { JobId = preview.JobId!.Value, ModuleId = _module, OperationId = _operation };
        await using var first = Context();
        await using var second = Context();
        var invitation = await Repository(first).ClaimStaffMemberInvitationAsync(request, _actor, Array.Empty<int>(), CancellationToken.None);
        Assert.That(invitation, Is.Not.Null);
        Assert.That(await Repository(second).ClaimStaffMemberInvitationAsync(request, _actor, Array.Empty<int>(), CancellationToken.None), Is.Null);
        await Repository(first).CompleteStaffMemberInvitationAsync(request, _actor, invitation!, BulkImportInvitationStatus.Failed, CancellationToken.None);
        var retried = await Repository(second).ClaimStaffMemberInvitationAsync(request, _actor, Array.Empty<int>(), CancellationToken.None);
        Assert.That(retried, Is.Not.Null);
        Assert.That(retried!.AttemptId, Is.Not.EqualTo(invitation!.AttemptId));
        Assert.ThrowsAsync<ConflictException>(async () => await Repository(first).CompleteStaffMemberInvitationAsync(
            request, _actor, invitation, BulkImportInvitationStatus.Sent, CancellationToken.None));
        await Repository(second).CompleteStaffMemberInvitationAsync(request, _actor, retried, BulkImportInvitationStatus.Sent, CancellationToken.None);
        Assert.That(await Repository(first).ClaimStaffMemberInvitationAsync(request, _actor, Array.Empty<int>(), CancellationToken.None), Is.Null);
        var report = await Act(preview, BulkImportAction.Get);
        Assert.That(report.Preview!.Rows[0].InvitationStatus, Is.EqualTo(BulkImportInvitationStatus.Sent));
        var json = System.Text.Json.JsonSerializer.Serialize(report);
        Assert.That(json, Does.Not.Contain("ImportedStaffMemberId").And.Not.Contain("InvitationAttemptId"));
        Assert.That(report.CreatedCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Uncertain_invitation_delivery_is_not_automatically_retried()
    {
        var preview = await Draft(_values);
        await Act(preview, BulkImportAction.Confirm);
        await Work();
        var request = new BulkImportJobRequestDTO { JobId = preview.JobId!.Value, ModuleId = _module, OperationId = _operation };
        await using var context = Context();
        var repository = Repository(context);
        var invitation = await repository.ClaimStaffMemberInvitationAsync(request, _actor, Array.Empty<int>(), CancellationToken.None);
        await repository.CompleteStaffMemberInvitationAsync(request, _actor, invitation!, BulkImportInvitationStatus.DeliveryUnknown, CancellationToken.None);
        Assert.That(await repository.ClaimStaffMemberInvitationAsync(request, _actor, Array.Empty<int>(), CancellationToken.None), Is.Null);
        Assert.That((await Act(preview, BulkImportAction.Get)).Preview!.Rows[0].InvitationStatus, Is.EqualTo(BulkImportInvitationStatus.DeliveryUnknown));
    }

    [Test]
    public async Task Invitation_claim_before_completion_or_from_another_institution_is_rejected()
    {
        var preview = await Draft(_values);
        var request = new BulkImportJobRequestDTO { JobId = preview.JobId!.Value, ModuleId = _module, OperationId = _operation };
        await using var context = Context();
        Assert.ThrowsAsync<ConflictException>(async () => await Repository(context).ClaimStaffMemberInvitationAsync(
            request, _actor, Array.Empty<int>(), CancellationToken.None));
        var other = new CommonDecodedResult { Success = true, InstitutionId = _actor.InstitutionId + 1, LoggedInStaffMemberId = _actor.LoggedInStaffMemberId, RoleId = _actor.RoleId };
        Assert.ThrowsAsync<NotFoundException>(async () => await Repository(context).ClaimStaffMemberInvitationAsync(
            request, other, Array.Empty<int>(), CancellationToken.None));
    }

    [Test]
    public async Task Confirmed_staffMember_creates_account_role_image_and_primary_address_without_email()
    {
        _values["MobileNumber"] = "+910000000000";
        _values["ContactNumber"] = "+910000000001";
        _values["Address"] = "Flat 12, Example Street";
        var preview = await Draft(_values);
        Assert.That(preview.CanCommit, Is.True, string.Join(";", preview.Errors.Concat(preview.Rows.SelectMany(row => row.Errors))));
        await using (var before = Context())
            Assert.That(await before.StaffMembers.AnyAsync(item => item.OfficialEmail == _values["OfficialEmail"]), Is.False);
        await Act(preview, BulkImportAction.Confirm);
        await Work();
        var report = await Act(preview, BulkImportAction.Get);
        Assert.That(report.CreatedCount, Is.EqualTo(1), System.Text.Json.JsonSerializer.Serialize(report));
        await using var context = Context();
        var staffMember = await context.StaffMembers.SingleAsync(item => item.OfficialEmail == _values["OfficialEmail"]);
        Assert.That(staffMember.EmployementCode, Is.EqualTo(preview.Rows[0].ProposedStaffMemberCode));
        Assert.That(staffMember.DateOfOnBoarding!.Value.Year, Is.EqualTo(2020));
        Assert.That(staffMember.MobileNumber, Is.EqualTo(_values["MobileNumber"]));
        Assert.That(await context.UserRoles.CountAsync(item => item.StaffMemberId == staffMember.Id), Is.EqualTo(1));
        Assert.That(await context.StaffMemberImages.CountAsync(item => item.StaffMemberId == staffMember.Id), Is.EqualTo(1));
        Assert.That(await context.StaffMemberContacts.Where(item => item.StaffMemberId == staffMember.Id).Select(item => item.Address).SingleAsync(), Is.EqualTo(_values["Address"]));
        Assert.That(await context.LoginCredentials.Where(item => item.StaffMemberId == staffMember.Id).Select(item => item.Password).SingleAsync(), Is.Null);
        await Act(preview, BulkImportAction.Confirm);
        Assert.That((await Act(preview, BulkImportAction.Get)).CreatedCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Preserved_0145_and_0200_generate_0201_and_reserve_approved_range()
    {
        var first = new Dictionary<string, string>(_values);
        var second = new Dictionary<string, string>(_values) { ["OfficialEmail"] = _prefix + "-two@example.invalid" };
        var third = new Dictionary<string, string>(_values) { ["OfficialEmail"] = _prefix + "-three@example.invalid" };
        var date = new DateTime(2020, 3, 15);
        first["StaffMemberCode"] = StaffMemberCodePatternFormatter.Format(_pattern, date, int.Parse(_values["DepartmentId"]), 145);
        second["StaffMemberCode"] = StaffMemberCodePatternFormatter.Format(_pattern, date, int.Parse(_values["DepartmentId"]), 200);
        var preview = await Draft(first, second, third);
        Assert.That(preview.CanCommit, Is.True, string.Join(";", preview.Errors.Concat(preview.Rows.SelectMany(row => row.Errors))));
        Assert.That(preview.Rows[0].ProposedStaffMemberCode, Is.EqualTo(first["StaffMemberCode"]));
        Assert.That(preview.Rows[1].ProposedStaffMemberCode, Is.EqualTo(second["StaffMemberCode"]));
        Assert.That(preview.Rows[2].ProposedStaffMemberCode, Does.EndWith("0201"));
        await Act(preview, BulkImportAction.Confirm);
        await Work();
        Assert.That((await Act(preview, BulkImportAction.Get)).CreatedCount, Is.EqualTo(3));
    }

    [Test]
    public async Task Initial_admin_counts_toward_capacity_and_worker_rechecks_changed_limit()
    {
        var preview = await Draft(_values);
        Assert.That(preview.CanCommit, Is.True);
        await Act(preview, BulkImportAction.Confirm);
        await using (var context = Context())
        {
            var count = await context.StaffMembers.CountAsync(item => item.InstitutionId == _actor.InstitutionId && !item.IsSoftDeleted);
            await context.SubscriptionPlans.Where(item => item.Id == _plan.Id).ExecuteUpdateAsync(setters => setters.SetProperty(item => item.MaxUsers, count));
        }
        await Work();
        var report = await Act(preview, BulkImportAction.Get);
        Assert.That(report.CreatedCount, Is.Zero);
        Assert.That(report.FailedCount, Is.EqualTo(1));
        Assert.That(report.Preview!.Rows[0].Errors, Has.Some.Contains("capacity"));
    }

    [Test]
    public async Task Duplicate_email_and_cross_institution_reference_block_confirmation()
    {
        var duplicate = await Draft(_values, new Dictionary<string, string>(_values));
        Assert.That(duplicate.InvalidCount, Is.EqualTo(2));
        Assert.ThrowsAsync<ValidationErrorException>(async () => await Act(duplicate, BulkImportAction.Confirm));
        await using var context = Context();
        _values["StaffEmploymentTypeId"] = (await context.StaffEmploymentTypes.Where(item => item.InstitutionId != _actor.InstitutionId).Select(item => item.Id).FirstAsync()).ToString();
        var foreign = await Draft(_values);
        Assert.That(foreign.CanCommit, Is.False);
        Assert.That(foreign.Rows[0].Errors, Has.Some.Contains("this institution"));
    }

    [Test]
    public async Task Stale_counter_blocks_confirmation_and_other_institution_cannot_read_job()
    {
        var preview = await Draft(_values);
        await using var context = Context();
        await context.StaffMemberCodePatterns.Where(item => item.Id == _pattern.Id).ExecuteUpdateAsync(setters => setters.SetProperty(item => item.LastUsedNumber, item => item.LastUsedNumber + 1));
        Assert.ThrowsAsync<ConflictException>(async () => await Act(preview, BulkImportAction.Confirm));
        var other = new CommonDecodedResult { Success = true, InstitutionId = _actor.InstitutionId + 1, LoggedInStaffMemberId = _actor.LoggedInStaffMemberId, RoleId = _actor.RoleId };
        Assert.ThrowsAsync<NotFoundException>(async () => await Act(preview, BulkImportAction.Get, other));
    }

    #endregion
}
