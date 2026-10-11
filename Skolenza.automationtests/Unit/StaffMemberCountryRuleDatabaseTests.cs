using skolenza.domain.Entity;
using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.infrastructure.Repositories;
using skolenza.persistance.Data.Context;
using skolenza.persistance.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NUnit.Framework;
using System.Text.Json;

namespace skolenza.automationtests.Unit;

/// <summary>Exercises country and institution rule resolution against disposable PostgreSQL.</summary>
[TestFixture]
[Category("StaffMemberCountryRulesDatabase")]
[NonParallelizable]
public sealed class StaffMemberCountryRuleDatabaseTests
{
    #region Fixture

    private SkolenzaDbContext CreateContext()
    {
        var connection = Environment.GetEnvironmentVariable("SKOLENZA_BULK_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection))
            Assert.Ignore("Requires disposable skolenza_bulk_test database.");

        Assert.That(new NpgsqlConnectionStringBuilder(connection).Database, Is.EqualTo("skolenza_bulk_test"));
        return new SkolenzaDbContext(new DbContextOptionsBuilder<SkolenzaDbContext>()
            .UseNpgsql(connection).Options);
    }

    #endregion

    #region Country eligibility

    [Test]
    public async Task StaffMember_list_returns_distinct_active_institution_roles_with_stable_contract()
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var staffMember = await context.StaffMembers.FirstAsync(x => !x.IsSoftDeleted && x.InstitutionId != null);
        var institutionId = staffMember.InstitutionId!.Value;
        var uniqueName = "Quality Assurance " + Guid.NewGuid().ToString("N")[..8];
        var role = new Role
        {
            InstitutionId = institutionId,
            RoleName = uniqueName,
            RoleType = 2,
            IsActive = true,
            IsSystemDefault = false,
            IsSoftDeleted = false,
            AddedById = staffMember.Id,
            AddedDateTime = DateTime.UtcNow
        };
        var systemRole = new Role
        {
            InstitutionId = institutionId,
            RoleName = "System Reviewer " + Guid.NewGuid().ToString("N")[..8],
            RoleType = 1,
            IsActive = true,
            IsSystemDefault = true,
            IsSoftDeleted = false,
            AddedById = staffMember.Id,
            AddedDateTime = DateTime.UtcNow
        };
        var inactiveRole = new Role
        {
            InstitutionId = institutionId,
            RoleName = "Inactive Reviewer " + Guid.NewGuid().ToString("N")[..8],
            RoleType = 2,
            IsActive = false,
            IsSystemDefault = false,
            IsSoftDeleted = false,
            AddedById = staffMember.Id,
            AddedDateTime = DateTime.UtcNow
        };
        context.Roles.AddRange(role, systemRole, inactiveRole);
        await context.SaveChangesAsync();
        context.UserRoles.AddRange(
            CreateUserRole(staffMember.Id, role.Id, staffMember.Id, true),
            CreateUserRole(staffMember.Id, role.Id, staffMember.Id, true),
            CreateUserRole(staffMember.Id, systemRole.Id, staffMember.Id, true),
            CreateUserRole(staffMember.Id, inactiveRole.Id, staffMember.Id, true));
        await context.SaveChangesAsync();

        var repository = new BaseStaffMemberRepository(
            context,
            null!,
            NullLogger<BaseStaffMemberRepository>.Instance,
            null!,
            null!);
        var response = await repository.GetAllInfo(
            institutionId,
            staffMember.Id,
            new GetAllStaffMemberInfoRequestDTO { PageNumber = 1, PageSize = 10 },
            staffMember.Id,
            1);

        var row = response.Data.Single();
        var assignedRole = row.AssignedRoles.Single(x => x.Id == role.Id);
        var assignedSystemRole = row.AssignedRoles.Single(x => x.Id == systemRole.Id);
        Assert.Multiple(() =>
        {
            Assert.That(row.AssignedRoles.Count(x => x.Id == role.Id), Is.EqualTo(1));
            Assert.That(assignedRole.Name, Is.EqualTo(uniqueName));
            Assert.That(assignedRole.Code, Is.EqualTo(uniqueName.ToUpperInvariant().Replace(' ', '_')));
            Assert.That(assignedRole.Type, Is.EqualTo("CUSTOM"));
            Assert.That(assignedSystemRole.Type, Is.EqualTo("SYSTEM"));
            Assert.That(row.AssignedRoles, Has.None.Matches<AssignedStaffMemberRoleDTO>(x => x.Id == inactiveRole.Id));
        });
        await transaction.RollbackAsync();
    }

    [Test]
    public void StaffMember_list_role_contract_serializes_assigned_roles_and_empty_array()
    {
        var emptyResponse = new GetAllStaffMemberInfoResponseDTO();
        var assignedResponse = new GetAllStaffMemberInfoResponseDTO
        {
            AssignedRoles =
            [
                new AssignedStaffMemberRoleDTO
                {
                    Id = 1,
                    Code = "SUPER_ADMIN",
                    Name = "Super Admin",
                    Type = "SYSTEM"
                }
            ]
        };
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var emptyJson = JsonSerializer.Serialize(emptyResponse, options);
        var assignedJson = JsonSerializer.Serialize(assignedResponse, options);

        Assert.Multiple(() =>
        {
            Assert.That(emptyJson, Does.Contain("\"assignedRoles\":[]"));
            Assert.That(assignedJson, Does.Contain("\"id\":1"));
            Assert.That(assignedJson, Does.Contain("\"code\":\"SUPER_ADMIN\""));
            Assert.That(assignedJson, Does.Contain("\"name\":\"Super Admin\""));
            Assert.That(assignedJson, Does.Contain("\"type\":\"SYSTEM\""));
        });
    }

    private static UserRole CreateUserRole(
        long staffMemberId,
        int roleId,
        long actorId,
        bool isActive)
    {
        return new UserRole
        {
            StaffMemberId = staffMemberId,
            RoleId = roleId,
            IsActive = isActive,
            IsSoftDeleted = false,
            AddedById = actorId,
            AddedDateTime = DateTime.UtcNow
        };
    }

    [Test]
    public async Task Institution_role_loaded_for_update_is_detached_and_persists_changed_name()
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var staffMember = await context.StaffMembers.FirstAsync(x => !x.IsSoftDeleted && x.InstitutionId != null);
        var role = new Role
        {
            InstitutionId = staffMember.InstitutionId!.Value,
            RoleName = "Role update test " + Guid.NewGuid().ToString("N"),
            RoleType = 2,
            IsActive = true,
            IsSoftDeleted = false,
            AddedById = staffMember.Id,
            AddedDateTime = DateTime.UtcNow
        };
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        var repository = new RoleRepository(context, NullLogger<RoleRepository>.Instance, null!, null!);
        var loaded = await repository.GetByIdForInstitutionAsync(role.Id, role.InstitutionId!.Value);
        Assert.That(loaded, Is.Not.Null);
        Assert.That(context.Entry(loaded!).State, Is.EqualTo(EntityState.Detached));

        loaded.RoleName += " changed";
        loaded.UpdatedById = staffMember.Id;
        loaded.UpdatedDateTime = DateTime.UtcNow;
        Assert.That(await repository.UpdateAsync(loaded), Is.True);

        var savedName = await context.Roles.AsNoTracking()
            .Where(x => x.Id == role.Id)
            .Select(x => x.RoleName)
            .SingleAsync();
        Assert.That(savedName, Does.EndWith(" changed"));
        await transaction.RollbackAsync();
    }

    [Test]
    public async Task Verified_bank_cannot_be_reopened_and_foreign_institution_bulk_update_is_rejected()
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var staffMember = await context.StaffMembers.FirstAsync(x => !x.IsSoftDeleted && x.InstitutionId != null);
        var bank = new StaffMemberBankDetail
        {
            StaffMemberId = staffMember.Id,
            BankName = "Profile verification test",
            BranchName = "Test",
            AccountNumber = "TEST" + Guid.NewGuid().ToString("N")[..12],
            AccountType = "saving",
            IsEditAllowed = true,
            IsInfoVerified = false,
            IsActive = true,
            AddedById = staffMember.Id,
            AddedDateTime = DateTime.UtcNow
        };
        context.Add(bank);
        await context.SaveChangesAsync();
        var repository = new BaseStaffMemberRepository(context, null!,
            NullLogger<BaseStaffMemberRepository>.Instance, null!, null!);

        await repository.UpdateVerificationStatusByTabAsync(2, staffMember.Id, staffMember.Id, true, CancellationToken.None);
        await repository.UpdateEditableStatusByEntityAsync(2, staffMember.Id, staffMember.Id, true, CancellationToken.None);
        var saved = await context.StaffMemberBankDetails.AsNoTracking().SingleAsync(x => x.Id == bank.Id);
        Assert.That(saved.IsInfoVerified, Is.True);
        Assert.That(saved.IsEditAllowed, Is.False);

        Assert.That(await repository.UpdateSectionVerifyStatusAsync(2, staffMember.Id, long.MaxValue,
            false, true, staffMember.Id, CancellationToken.None), Is.False);
        saved = await context.StaffMemberBankDetails.AsNoTracking().SingleAsync(x => x.Id == bank.Id);
        Assert.That(saved.IsInfoVerified, Is.True);
        await transaction.RollbackAsync();
    }

    [Test]
    public async Task Operational_defaults_are_locked_until_enabled_and_can_be_locked_again()
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var staffMember = await context.StaffMembers.FirstAsync(x => !x.IsSoftDeleted && x.InstitutionId != null);
        var institutionId = staffMember.InstitutionId!.Value;
        var repository = new BaseStaffMemberRepository(context, null!,
            NullLogger<BaseStaffMemberRepository>.Instance, null!, null!);
        const string module = "EMP_DEVICES";
        await context.Set<InstitutionStaffMemberSectionDefault>()
            .Where(x => x.InstitutionId == institutionId && x.ModuleCode == module).ExecuteDeleteAsync();

        Assert.That(await repository.IsOperationalSectionEditAllowedAsync(institutionId, module, CancellationToken.None), Is.False);
        await repository.SetOperationalSectionDefaultAsync(institutionId, module, true, staffMember.Id, CancellationToken.None);
        Assert.That(await repository.IsOperationalSectionEditAllowedAsync(institutionId, module, CancellationToken.None), Is.True);
        Assert.That(await repository.IsOperationalSectionEditAllowedAsync(long.MaxValue, module, CancellationToken.None), Is.False);
        await repository.SetOperationalSectionDefaultAsync(institutionId, module, false, staffMember.Id, CancellationToken.None);
        Assert.That(await repository.IsOperationalSectionEditAllowedAsync(institutionId, module, CancellationToken.None), Is.False);
        Assert.That(await context.Set<InstitutionStaffMemberSectionDefault>().CountAsync(x => x.InstitutionId == institutionId && x.ModuleCode == module), Is.EqualTo(1));
        await transaction.RollbackAsync();
    }

    [Test]
    public async Task Identity_catalogue_rejects_foreign_institution_and_inactive_rule()
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var staffMember = await context.StaffMembers.FirstAsync(x => !x.IsSoftDeleted && x.InstitutionId != null);
        var rule = await context.CountryIdentityRules.Include(x => x.IdentityCategoryDocument)
            .ThenInclude(x => x.IdentityCategory).FirstAsync(x => x.IsActive &&
                x.IdentityCategoryDocument.IsActive && x.IdentityCategoryDocument.IdentityCategory.IsActive);
        staffMember.CountryId = rule.CountryId;
        await context.SaveChangesAsync();
        var repository = new StaffMemberIdentityRepository(context, null!,
            NullLogger<StaffMemberIdentityRepository>.Instance, null!, null!);

        Assert.That(await repository.IsDocumentAllowedAsync(staffMember.Id, staffMember.InstitutionId!.Value,
            rule.IdentityCategoryDocumentId, CancellationToken.None), Is.True);
        Assert.That(await repository.IsDocumentAllowedAsync(staffMember.Id, long.MaxValue,
            rule.IdentityCategoryDocumentId, CancellationToken.None), Is.False);

        await context.CountryIdentityRules.Where(x => x.CountryId == rule.CountryId &&
                x.IdentityCategoryDocumentId == rule.IdentityCategoryDocumentId)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.IsActive, false));

        Assert.That(await repository.IsDocumentAllowedAsync(staffMember.Id, staffMember.InstitutionId.Value,
            rule.IdentityCategoryDocumentId, CancellationToken.None), Is.False);
        await transaction.RollbackAsync();
    }

    #endregion

    #region Compliance precedence

    [Test]
    public async Task Institution_rule_beats_global_rule_and_expired_rule_is_ignored()
    {
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var institutionId = await context.StaffMembers.Where(x => !x.IsSoftDeleted && x.InstitutionId != null)
            .Select(x => x.InstitutionId!.Value).FirstAsync();
        var countryId = await context.Countries.Select(x => x.Id).FirstAsync();
        var type = new ComplianceTypeMaster
        {
            Name = "StaffMember rule test " + Guid.NewGuid().ToString("N"),
            CountryId = countryId,
            IsActive = true
        };
        context.Add(type);
        await context.SaveChangesAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var global = Rule(type.Id, countryId, null, today.AddDays(-2), null, 999);
        var institution = Rule(type.Id, countryId, institutionId, today.AddDays(-2), null, 1);
        var expired = Rule(type.Id, countryId, institutionId, today.AddDays(-10), today.AddDays(-1), 1000);
        context.AddRange(global, institution, expired);
        await context.SaveChangesAsync();
        var repository = new CompilanceRuleRepository(context, NullLogger<CompilanceRuleRepository>.Instance);

        var resolved = await repository.GetRuleAsync(type.Id, countryId, null, institutionId, today);
        Assert.That(resolved!.Id, Is.EqualTo(institution.Id));
        var fallback = await repository.GetRuleAsync(type.Id, countryId, null, null, today);
        Assert.That(fallback!.Id, Is.EqualTo(global.Id));
        await transaction.RollbackAsync();
    }

    private static ComplianceRule Rule(int type, int country, long? institution, DateOnly start,
        DateOnly? end, int priority)
    {
        return new ComplianceRule
        {
            ComplianceTypeId = type,
            CountryId = country,
            InstitutionId = institution,
            EffectiveFrom = start,
            EffectiveTo = end,
            Priority = priority,
            RuleJson = "{}",
            IsActive = true,
            IsSoftDeleted = false
        };
    }

    #endregion
}
