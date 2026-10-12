using Microsoft.EntityFrameworkCore;
using skolenza.application.DTOS.Staff;
using skolenza.application.Interfaces.IRepositories;
using skolenza.persistance.Data.Context;

namespace skolenza.persistance.Repositories;

/// <summary>Reads the global Staff Category catalogue used by every Institution.</summary>
public sealed class StaffCategoryRepository(SkolenzaDbContext context) : IStaffCategoryRepository
{
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
