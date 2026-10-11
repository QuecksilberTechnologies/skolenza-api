using AutoMapper;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Staff.Type;
using skolenza.application.Exceptions;
using skolenza.application.Mappings;
using skolenza.persistance.Data.Context;
using skolenza.persistance.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("BulkImportDatabase")]
[NonParallelizable]
public sealed class StaffEmploymentTypeDatabaseTests
{
    private SkolenzaDbContext _context = null!;
    private IDbContextTransaction _transaction = null!;
    private StaffEmploymentTypeRepository _repository = null!;
    private long[] _institutions = null!;
    private string _name = null!;

    [SetUp]
    public async Task Setup()
    {
        var connection = Environment.GetEnvironmentVariable("SKOLENZA_BULK_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection))
        {
            Assert.Ignore("Requires the isolated skolenza_bulk_test database with institution StaffEmploymentType migration.");
        }
        Assert.That(new NpgsqlConnectionStringBuilder(connection).Database, Is.EqualTo("skolenza_bulk_test"));
        _context = new SkolenzaDbContext(new DbContextOptionsBuilder<SkolenzaDbContext>().UseNpgsql(connection).Options);
        _transaction = await _context.Database.BeginTransactionAsync();
        _repository = new StaffEmploymentTypeRepository(_context,
            new MapperConfiguration(config => config.AddProfile<MappingProfile>()).CreateMapper());
        _institutions = await _context.Institutions.OrderBy(item => item.Id).Select(item => item.Id).Take(2).ToArrayAsync();
        Assert.That(_institutions.Length, Is.EqualTo(2));
        _name = "BulkTypeTest-" + Guid.NewGuid().ToString("N");
    }

    [TearDown]
    public async Task Cleanup()
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
        }
        if (_context is not null)
        {
            await _context.DisposeAsync();
        }
    }

    [Test]
    public async Task Same_name_is_allowed_in_two_institutions_but_case_trim_duplicate_is_rejected()
    {
        var first = await _repository.CreateAsync(_institutions[0], 1, new CreateStaffEmploymentTypeDTO { TypeName = _name }, CancellationToken.None);
        var second = await _repository.CreateAsync(_institutions[1], 1, new CreateStaffEmploymentTypeDTO { TypeName = _name }, CancellationToken.None);
        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
        Assert.ThrowsAsync<ConflictException>(async () => await _repository.CreateAsync(_institutions[0], 1,
            new CreateStaffEmploymentTypeDTO { TypeName = " " + _name.ToUpperInvariant() + " " }, CancellationToken.None));
        Assert.That(await _repository.GetStaffEmploymentTypeByIdAsync(_institutions[1], first.Id, CancellationToken.None), Is.Null);
    }

    [Test]
    public async Task Institution_reads_hide_legacy_and_foreign_catalogues()
    {
        var own = await _repository.CreateAsync(_institutions[0], 1, new CreateStaffEmploymentTypeDTO { TypeName = _name }, CancellationToken.None);
        var foreign = await _repository.CreateAsync(_institutions[1], 1, new CreateStaffEmploymentTypeDTO { TypeName = _name + "-foreign" }, CancellationToken.None);
        var rows = await _repository.GetAllAsync(_institutions[0], CancellationToken.None);
        var legacy = await _context.StaffEmploymentTypes.Where(item => item.InstitutionId == null).Select(item => item.Id).ToListAsync();
        Assert.That(rows.Any(item => item.Id == own.Id), Is.True);
        Assert.That(rows.Any(item => item.Id == foreign.Id || legacy.Contains(item.Id)), Is.False);
    }

    [Test]
    public async Task Onboarding_resolves_one_institution_permanent_type_idempotently()
    {
        var before = await _context.StaffEmploymentTypes.CountAsync(item => item.InstitutionId == _institutions[0]);
        var first = await _repository.EnsureOnboardingTypeAsync(_institutions[0], 1, CancellationToken.None);
        var second = await _repository.EnsureOnboardingTypeAsync(_institutions[0], 1, CancellationToken.None);
        Assert.That(second, Is.EqualTo(first));
        Assert.That(await _context.StaffEmploymentTypes.Where(item => item.Id == first).Select(item => item.InstitutionId).SingleAsync(),
            Is.EqualTo(_institutions[0]));
        var after = await _context.StaffEmploymentTypes.CountAsync(item => item.InstitutionId == _institutions[0]);
        Assert.That(after - before, Is.InRange(0, 1), "Must not seed six defaults.");
    }

    [Test]
    public async Task Cross_institution_staffMember_assignment_is_rejected_by_database()
    {
        var staffMember = await _context.StaffMembers.AsNoTracking().FirstAsync(item => item.InstitutionId != null);
        var otherInstitution = _institutions.First(item => item != staffMember.InstitutionId);
        var foreign = await _repository.CreateAsync(otherInstitution, 1, new CreateStaffEmploymentTypeDTO { TypeName = _name }, CancellationToken.None);
        var error = Assert.ThrowsAsync<PostgresException>(async () => await _context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE skolenza.\"StaffMember\" SET \"StaffEmploymentTypeId\"={foreign.Id} WHERE \"Id\"={staffMember.Id}"));
        Assert.That(error!.SqlState, Is.EqualTo(PostgresErrorCodes.CheckViolation));
    }

    [Test]
    public async Task Migrated_staffMember_references_keep_institution_ownership_and_legacy_templates_remain()
    {
        var invalid = await (from staffMember in _context.StaffMembers
            join type in _context.StaffEmploymentTypes on staffMember.StaffEmploymentTypeId equals type.Id
            where staffMember.InstitutionId != null && staffMember.InstitutionId != type.InstitutionId
            select staffMember.Id).CountAsync();
        Assert.That(invalid, Is.Zero);
        Assert.That(await _context.StaffEmploymentTypes.AnyAsync(item => item.Id == 1 && item.InstitutionId == null), Is.True);
    }

    [Test]
    public async Task Existing_type_cannot_be_moved_to_another_institution()
    {
        var own = await _repository.CreateAsync(_institutions[0], 1,
            new CreateStaffEmploymentTypeDTO { TypeName = _name }, CancellationToken.None);
        var error = Assert.ThrowsAsync<PostgresException>(async () => await _context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE skolenza.\"StaffEmploymentType\" SET \"InstitutionId\"={_institutions[1]} WHERE \"Id\"={own.Id}"));
        Assert.That(error!.SqlState, Is.EqualTo(PostgresErrorCodes.CheckViolation));
    }

    [TestCase(255, true)]
    [TestCase(256, false)]
    public void Preview_respects_staffMember_type_database_text_lengths(int length, bool valid)
    {
        var preview = BulkImportPreviewService.Build(BulkImportMaster.StaffEmploymentType, null,
            new BulkImportTableDTO
            {
                Columns = new() { "TypeName", "Description", "Remark" },
                Rows = new() { new() { RowNumber = 2, Values = new() { _name, new string('a', length), new string('b', length) } } }
            }, Array.Empty<skolenza.application.DTOs.Department.GetDepartmentResponseDTO>(),
            Array.Empty<skolenza.application.DTOs.Designation.GetDesignationResponseDTO>(),
            Array.Empty<skolenza.application.DTOs.Role.GetRoleResponseDTO>());
        Assert.That(preview.IsValid, Is.EqualTo(valid));
    }
}
