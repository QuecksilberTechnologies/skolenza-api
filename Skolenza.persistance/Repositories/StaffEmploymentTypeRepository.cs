using AutoMapper;
using skolenza.application.Constants;
using GetStaffEmploymentTypeResponseDTO = skolenza.application.DTOs.StaffType.GetStaffEmploymentTypeResponseDTO;
using skolenza.application.DTOS.Staff.Type;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace skolenza.persistance.Repositories;

/// <summary>Owns institution StaffEmploymentType reads and writes; legacy shared catalogue rows are never institution options.</summary>
public sealed class StaffEmploymentTypeRepository(SkolenzaDbContext context, IMapper mapper) : IStaffEmploymentTypeRepository
{
    #region Institution reads
    public Task<StaffEmploymentType?> GetStaffEmploymentTypeByIdAsync(long institutionId, int staffEmploymentTypeId,
        CancellationToken cancellationToken)
    {
        return context.StaffEmploymentTypes.AsNoTracking().SingleOrDefaultAsync(item =>
            item.Id == staffEmploymentTypeId && item.InstitutionId == institutionId && item.IsSoftDeleted != true,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<StaffEmploymentType?> GetForUpdateAsync(long institutionId, int staffEmploymentTypeId,
        CancellationToken cancellationToken)
    {
        return context.StaffEmploymentTypes.SingleOrDefaultAsync(item =>
            item.Id == staffEmploymentTypeId &&
            item.InstitutionId == institutionId &&
            item.IsSoftDeleted != true,
            cancellationToken);
    }

    public async Task<List<GetStaffEmploymentTypeResponseDTO>> GetAllAsync(long institutionId,
        CancellationToken cancellationToken)
    {
        if (institutionId <= 0)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }
        var rows = await context.StaffEmploymentTypes.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && item.IsSoftDeleted != true)
            .OrderBy(item => item.TypeName).ThenBy(item => item.Id).ToListAsync(cancellationToken);
        return mapper.Map<List<GetStaffEmploymentTypeResponseDTO>>(rows);
    }

    /// <inheritdoc />
    public Task<bool> NameExistsAsync(long institutionId, string typeName,
        int? excludeStaffEmploymentTypeId, CancellationToken cancellationToken)
    {
        var normalizedName = typeName.Trim().ToLower();
        return context.StaffEmploymentTypes.AsNoTracking().AnyAsync(item =>
            item.InstitutionId == institutionId &&
            item.IsSoftDeleted != true &&
            item.TypeName != null &&
            item.TypeName.Trim().ToLower() == normalizedName &&
            (!excludeStaffEmploymentTypeId.HasValue || item.Id != excludeStaffEmploymentTypeId.Value),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasDeletionDependenciesAsync(long institutionId, int staffEmploymentTypeId,
        CancellationToken cancellationToken)
    {
        if (await context.StaffMembers.AsNoTracking().AnyAsync(staffMember =>
                staffMember.InstitutionId == institutionId &&
                staffMember.StaffEmploymentTypeId == staffEmploymentTypeId &&
                !staffMember.IsSoftDeleted,
                cancellationToken))
        {
            return true;
        }

        if (await context.StaffMembersChangedTypeHistories.AsNoTracking().AnyAsync(history =>
                (history.OldStaffEmploymentTypeId == staffEmploymentTypeId || history.NewStaffEmploymentTypeId == staffEmploymentTypeId) &&
                history.StaffMember.InstitutionId == institutionId &&
                !history.StaffMember.IsSoftDeleted,
                cancellationToken))
        {
            return true;
        }

        // StaffEmploymentTypeBasicMenu has no soft-delete state. Preserving an existing mapping
        // is required for a future StaffEmploymentType restore, so every row blocks deletion.
        return await context.StaffEmploymentTypeBasicMenus.AsNoTracking().AnyAsync(mapping =>
            mapping.StaffEmploymentTypeId == staffEmploymentTypeId,
            cancellationToken);
    }
    #endregion

    #region Creation
    public async Task<GetStaffEmploymentTypeResponseDTO> CreateAsync(long institutionId, long actorId,
        CreateStaffEmploymentTypeDTO request, CancellationToken cancellationToken)
    {
        if (institutionId <= 0 || actorId <= 0)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }
        var name = request.TypeName?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 255 ||
            request.Description?.Length > 255 || request.Remark?.Length > 255)
        {
            throw new ValidationErrorException("TypeName is required; type name, description and remark must not exceed 255 characters.");
        }
        if (await context.StaffEmploymentTypes.AnyAsync(item => item.InstitutionId == institutionId &&
            item.IsSoftDeleted != true && item.TypeName!.Trim().ToLower() == name.ToLower(), cancellationToken))
        {
            throw new ConflictException("StaffEmploymentType already exists in this institution.");
        }
        var entity = mapper.Map<StaffEmploymentType>(request);
        entity.TypeName = name;
        entity.InstitutionId = institutionId;
        entity.AddedById = actorId;
        entity.AddedDateTime = DateTime.UtcNow;
        entity.IsSoftDeleted = false;
        context.StaffEmploymentTypes.Add(entity);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException error) when (error.InnerException is PostgresException postgres &&
            postgres.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            context.Entry(entity).State = EntityState.Detached;
            throw new ConflictException("StaffEmploymentType already exists in this institution.");
        }
        return mapper.Map<GetStaffEmploymentTypeResponseDTO>(entity);
    }

    /// <inheritdoc />
    public async Task<GetStaffEmploymentTypeResponseDTO> UpdateAsync(StaffEmploymentType entity,
        CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
        return mapper.Map<GetStaffEmploymentTypeResponseDTO>(entity);
    }

    /// <inheritdoc />
    public async Task<bool> SoftDeleteAsync(StaffEmploymentType entity,
        CancellationToken cancellationToken)
    {
        entity.IsSoftDeleted = true;
        entity.IsActive = false;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Creates or resolves the institution-owned Permanent type used by the initial Institution Administrator.</summary>
    public async Task<int> EnsureOnboardingTypeAsync(long institutionId, long actorId, CancellationToken cancellationToken)
    {
        // Caller is the institution-creation transaction. The platform constant identifies the global template only.
        var template = await context.StaffEmploymentTypes.AsNoTracking().SingleAsync(item =>
            item.Id == ConstantValues.PermanentStaffEmploymentType && item.InstitutionId == null, cancellationToken);
        var existing = await context.StaffEmploymentTypes.SingleOrDefaultAsync(item => item.InstitutionId == institutionId &&
            item.TypeName == template.TypeName && item.IsSoftDeleted != true, cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }
        var created = await CreateAsync(institutionId, actorId, new CreateStaffEmploymentTypeDTO
        {
            TypeName = template.TypeName!,
            Description = template.Description,
            Remark = template.Remark,
            IsActive = true
        }, cancellationToken);
        return created.Id;
    }
    #endregion
}
