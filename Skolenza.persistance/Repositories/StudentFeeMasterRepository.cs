using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.Common.Enums;
using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Student;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Repositories;

public sealed class StudentFeeMasterRepository(SkolenzaDbContext context)
    : IStudentFeeMasterRepository
{
    public async Task<PagedResponseDTO<StudentFeeFrequencyResponseDTO>> GetFrequenciesAsync(
        long institutionId,
        StudentFeeFrequencyFilterDTO filter,
        CancellationToken cancellationToken)
    {
        var query = context.StudentFeeFrequencyMasters.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && !item.IsSoftDeleted);
        if (filter.ActiveOnly)
        {
            query = query.Where(item => item.IsActive);
        }

        var page = await query
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.InstallmentsPerYear)
            .ThenBy(item => item.Name)
            .ThenBy(item => item.Id)
            .ToPageAsync(filter.PageNumber, filter.PageSize, cancellationToken);

        return new PagedResponseDTO<StudentFeeFrequencyResponseDTO>(
            page.Data.Select(MapFrequency).ToList(), page.TotalCount, page.PageNumber, page.PageSize)
        {
            TotalPages = page.TotalPages
        };
    }

    public async Task<StudentFeeFrequencyResponseDTO> SaveFrequencyAsync(
        long? id,
        long institutionId,
        long actorStaffMemberId,
        StudentFeeFrequencyRequestDTO request,
        CancellationToken cancellationToken)
    {
        ValidateText(request.Code, request.Name);
        if (request.InstallmentsPerYear is < 1 or > 12)
        {
            throw new ValidationErrorException("InstallmentsPerYear must be between 1 and 12.");
        }

        var code = NormalizeCode(request.Code);
        await EnsureCodeAvailableAsync(
            context.StudentFeeFrequencyMasters,
            institutionId,
            code,
            id,
            cancellationToken);

        var entity = id.HasValue
            ? await context.StudentFeeFrequencyMasters.SingleOrDefaultAsync(item =>
                item.Id == id.Value && item.InstitutionId == institutionId && !item.IsSoftDeleted,
                cancellationToken) ?? throw new KeyNotFoundException("Student fee frequency was not found.")
            : new StudentFeeFrequencyMaster
            {
                InstitutionId = institutionId,
                AddedById = actorStaffMemberId,
                AddedDateTime = DateTime.UtcNow
            };

        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Description = NormalizeOptional(request.Description);
        entity.InstallmentsPerYear = request.InstallmentsPerYear;
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsActive = request.IsActive;
        if (id.HasValue)
        {
            entity.UpdatedById = actorStaffMemberId;
            entity.UpdatedDateTime = DateTime.UtcNow;
        }
        else
        {
            context.StudentFeeFrequencyMasters.Add(entity);
        }

        await context.SaveChangesAsync(cancellationToken);
        return MapFrequency(entity);
    }

    public Task DeleteFrequencyAsync(
        long id,
        long institutionId,
        long actorStaffMemberId,
        CancellationToken cancellationToken) =>
        SoftDeleteAsync(context.StudentFeeFrequencyMasters, id, institutionId, actorStaffMemberId, cancellationToken);

    public async Task<PagedResponseDTO<StudentFeeComponentResponseDTO>> GetComponentsAsync(
        long institutionId,
        StudentFeeComponentFilterDTO filter,
        CancellationToken cancellationToken)
    {
        var query = context.StudentFeeComponentMasters.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && !item.IsSoftDeleted);
        if (filter.ActiveOnly)
        {
            query = query.Where(item => item.IsActive);
        }

        if (filter.ChargeNature.HasValue)
        {
            query = query.Where(item => item.ChargeNature == (short)filter.ChargeNature.Value);
        }

        var page = await query
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.ChargeNature)
            .ThenBy(item => item.Name)
            .ThenBy(item => item.Id)
            .ToPageAsync(filter.PageNumber, filter.PageSize, cancellationToken);

        return new PagedResponseDTO<StudentFeeComponentResponseDTO>(
            page.Data.Select(MapComponent).ToList(), page.TotalCount, page.PageNumber, page.PageSize)
        {
            TotalPages = page.TotalPages
        };
    }

    public async Task<StudentFeeComponentResponseDTO> SaveComponentAsync(
        long? id,
        long institutionId,
        long actorStaffMemberId,
        StudentFeeComponentRequestDTO request,
        CancellationToken cancellationToken)
    {
        ValidateText(request.Code, request.Name);
        if (!Enum.IsDefined(request.ChargeNature))
        {
            throw new ValidationErrorException("ChargeNature must be RecurringAnnual or OneTime.");
        }

        var code = NormalizeCode(request.Code);
        await EnsureCodeAvailableAsync(
            context.StudentFeeComponentMasters,
            institutionId,
            code,
            id,
            cancellationToken);

        var entity = id.HasValue
            ? await context.StudentFeeComponentMasters.SingleOrDefaultAsync(item =>
                item.Id == id.Value && item.InstitutionId == institutionId && !item.IsSoftDeleted,
                cancellationToken) ?? throw new KeyNotFoundException("Student fee component was not found.")
            : new StudentFeeComponentMaster
            {
                InstitutionId = institutionId,
                AddedById = actorStaffMemberId,
                AddedDateTime = DateTime.UtcNow
            };

        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Description = NormalizeOptional(request.Description);
        entity.ChargeNature = (short)request.ChargeNature;
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsActive = request.IsActive;
        if (id.HasValue)
        {
            entity.UpdatedById = actorStaffMemberId;
            entity.UpdatedDateTime = DateTime.UtcNow;
        }
        else
        {
            context.StudentFeeComponentMasters.Add(entity);
        }

        await context.SaveChangesAsync(cancellationToken);
        return MapComponent(entity);
    }

    public Task DeleteComponentAsync(
        long id,
        long institutionId,
        long actorStaffMemberId,
        CancellationToken cancellationToken) =>
        SoftDeleteAsync(context.StudentFeeComponentMasters, id, institutionId, actorStaffMemberId, cancellationToken);

    private async Task EnsureCodeAvailableAsync<TEntity>(
        DbSet<TEntity> set,
        long institutionId,
        string code,
        long? exceptId,
        CancellationToken cancellationToken)
        where TEntity : StudentInstitutionMasterEntity
    {
        var exists = await set.AnyAsync(item =>
            item.InstitutionId == institutionId &&
            item.Code == code &&
            !item.IsSoftDeleted &&
            (!exceptId.HasValue || item.Id != exceptId.Value), cancellationToken);
        if (exists)
        {
            throw new ConflictException("A fee master with this code already exists for the institution.");
        }
    }

    private async Task SoftDeleteAsync<TEntity>(
        DbSet<TEntity> set,
        long id,
        long institutionId,
        long actorStaffMemberId,
        CancellationToken cancellationToken)
        where TEntity : StudentInstitutionMasterEntity
    {
        var entity = await set.SingleOrDefaultAsync(item =>
            item.Id == id && item.InstitutionId == institutionId && !item.IsSoftDeleted,
            cancellationToken) ?? throw new KeyNotFoundException("Student fee master was not found.");

        entity.IsActive = false;
        entity.IsSoftDeleted = true;
        entity.SoftDeletedById = actorStaffMemberId;
        entity.DeletedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateText(string? code, string? name)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationErrorException("Code and name are required.");
        }
    }

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static StudentFeeFrequencyResponseDTO MapFrequency(StudentFeeFrequencyMaster entity) => new()
    {
        Id = entity.Id,
        Code = entity.Code,
        Name = entity.Name,
        Description = entity.Description,
        InstallmentsPerYear = entity.InstallmentsPerYear,
        DisplayOrder = entity.DisplayOrder,
        IsActive = entity.IsActive
    };

    private static StudentFeeComponentResponseDTO MapComponent(StudentFeeComponentMaster entity) => new()
    {
        Id = entity.Id,
        Code = entity.Code,
        Name = entity.Name,
        Description = entity.Description,
        ChargeNature = (StudentFeeComponentChargeNature)entity.ChargeNature,
        DisplayOrder = entity.DisplayOrder,
        IsActive = entity.IsActive
    };
}
