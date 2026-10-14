using Microsoft.EntityFrameworkCore;
using skolenza.application.DTOS.Staff;
using skolenza.application.Interfaces.IRepositories;
using skolenza.persistance.Data.Context;
using skolenza.domain.Entity;

namespace skolenza.persistance.Repositories;

/// <summary>Reads the global Staff Category catalogue used by every Institution.</summary>
public sealed class StaffCategoryRepository(SkolenzaDbContext context) : IStaffCategoryRepository
{
    public async Task<IReadOnlyList<HostStaffCategoryResponseDTO>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await context.StaffCategories.AsNoTracking()
            .OrderBy(category => category.DisplayOrder).ThenBy(category => category.Id)
            .Select(category => new HostStaffCategoryResponseDTO(category.Id, category.Code, category.Name,
                category.Description, category.DisplayOrder, category.IsActive, category.IsSystemDefined))
            .ToListAsync(cancellationToken);
    }

    public async Task<HostStaffCategoryResponseDTO> SaveAsync(
        SaveHostStaffCategoryRequestDTO request,
        CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await context.StaffCategories.AnyAsync(category => category.Code == code && category.Id != request.Id,
                cancellationToken))
        {
            throw new skolenza.application.Exceptions.ConflictException("A Staff Category with this code already exists.");
        }

        StaffCategory entity;
        if (request.Id.HasValue)
        {
            entity = await context.StaffCategories.SingleOrDefaultAsync(category => category.Id == request.Id.Value,
                    cancellationToken)
                ?? throw new skolenza.application.Exceptions.NotFoundException("Staff Category was not found.");
            entity.UpdatedDateTime = DateTime.UtcNow;
        }
        else
        {
            entity = new StaffCategory
            {
                Code = code,
                Name = request.Name.Trim(),
                AddedDateTime = DateTime.UtcNow,
                IsSystemDefined = false
            };
            context.StaffCategories.Add(entity);
        }

        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsActive = request.IsActive;
        await context.SaveChangesAsync(cancellationToken);
        return new HostStaffCategoryResponseDTO(entity.Id, entity.Code, entity.Name, entity.Description,
            entity.DisplayOrder, entity.IsActive, entity.IsSystemDefined);
    }

    public async Task<IReadOnlyList<StaffCategoryOptionDTO>> GetActiveOptionsAsync(
        CancellationToken cancellationToken)
    {
        return await context.StaffCategories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Id)
            .Select(category => new StaffCategoryOptionDTO(
                category.Id,
                category.Code,
                category.Name,
                category.Description))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> IsActiveAsync(short staffCategoryId, CancellationToken cancellationToken)
    {
        return context.StaffCategories
            .AsNoTracking()
            .AnyAsync(category => category.Id == staffCategoryId && category.IsActive, cancellationToken);
    }
}
