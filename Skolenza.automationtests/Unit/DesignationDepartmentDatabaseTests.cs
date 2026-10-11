using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using skolenza.persistance.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

/// <summary>Actual PostgreSQL repository checks; require an isolated restored test database.</summary>
[TestFixture]
[Category("BulkImportDatabase")]
[NonParallelizable]
public sealed class DesignationDepartmentDatabaseTests
{
    private SkolenzaDbContext _context = null!;
    private IDbContextTransaction _transaction = null!;
    private DesignationRepository _repository = null!;
    private long _institutionId;
    private int _it;
    private int _hr;

    [SetUp]
    public async Task Setup()
    {
        var connection = Environment.GetEnvironmentVariable("SKOLENZA_BULK_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection))
        {
            Assert.Ignore("Set SKOLENZA_BULK_TEST_CONNECTION to an isolated skolenza_bulk_test database restored from the backup.");
        }
        var builder = new NpgsqlConnectionStringBuilder(connection);
        Assert.That(builder.Database, Is.EqualTo("skolenza_bulk_test"), "Only the isolated bulk test database is allowed.");
        _context = new SkolenzaDbContext(new DbContextOptionsBuilder<SkolenzaDbContext>()
            .UseNpgsql(connection).Options);
        _transaction = await _context.Database.BeginTransactionAsync();
        _institutionId = await _context.Institutions.Select(institution => institution.Id).FirstAsync();
        var prefix = "BulkTest-" + Guid.NewGuid().ToString("N");
        var it = new Department { InstitutionId = _institutionId, DepartmentName = prefix + "-IT", IsActive = true };
        var hr = new Department { InstitutionId = _institutionId, DepartmentName = prefix + "-HR", IsActive = true };
        _context.Departments.AddRange(it, hr);
        await _context.SaveChangesAsync();
        _it = it.Id;
        _hr = hr.Id;
        _repository = new DesignationRepository(_context, NullLogger<DesignationRepository>.Instance, null!);
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
    public async Task Manager_can_exist_in_IT_and_HR_but_not_twice_in_IT()
    {
        var first = await _repository.CreateAsync(NewDesignation(_it, "Manager"));
        var second = await _repository.CreateAsync(NewDesignation(_hr, "Manager"));
        var duplicate = await _repository.CreateAsync(NewDesignation(_it, " manager "));
        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(duplicate, Is.Null);
        });
    }

    [Test]
    public async Task Updating_or_moving_into_an_existing_department_name_is_rejected()
    {
        await _repository.CreateAsync(NewDesignation(_it, "Manager"));
        var hr = await _repository.CreateAsync(NewDesignation(_hr, "Manager"));
        Assert.That(await _repository.CheckDuplicateValueAsync(_institutionId, _hr, "Manager", hr!.Id), Is.False);
        hr.DepartmentId = _it;
        Assert.That(await _repository.UpdateDesignationAsync(hr), Is.False);
    }

    [Test]
    public async Task Inactive_and_other_institution_departments_are_rejected()
    {
        var department = await _context.Departments.SingleAsync(item => item.Id == _it);
        department.IsActive = false;
        await _context.SaveChangesAsync();
        Assert.That(await _repository.CreateAsync(NewDesignation(_it, "Manager")), Is.Null);
        var wrongInstitution = NewDesignation(_hr, "Manager");
        wrongInstitution.InstitutionId = long.MaxValue;
        Assert.That(await _repository.CreateAsync(wrongInstitution), Is.Null);
    }

    [Test]
    public async Task Soft_deleted_designation_does_not_reserve_its_name()
    {
        var designation = await _repository.CreateAsync(NewDesignation(_it, "Manager"));
        designation!.IsSoftDeleted = true;
        await _context.SaveChangesAsync();
        Assert.That(await _repository.CreateAsync(NewDesignation(_it, "Manager")), Is.Not.Null);
    }

    [Test]
    public async Task Database_unique_index_rejects_duplicate_even_when_repository_is_bypassed()
    {
        await _repository.CreateAsync(NewDesignation(_it, "Manager"));
        _context.Designations.Add(NewDesignation(_it, " MANAGER "));
        var error = Assert.ThrowsAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
        Assert.That((error!.InnerException as PostgresException)?.SqlState, Is.EqualTo(PostgresErrorCodes.UniqueViolation));
    }

    [Test]
    public void Database_rejects_designation_without_department()
    {
        var error = Assert.ThrowsAsync<PostgresException>(async () =>
            await _context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO skolenza.\"Designation\" (\"InstitutionId\", \"DesignationName\") VALUES ({_institutionId}, 'Missing department')"));
        Assert.That(error!.SqlState, Is.EqualTo(PostgresErrorCodes.NotNullViolation));
    }

    private Designation NewDesignation(int departmentId, string name)
    {
        return new Designation
        {
            InstitutionId = _institutionId,
            DepartmentId = departmentId,
            DesignationName = name,
            IsActive = true
        };
    }
}
