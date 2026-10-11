// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Persists and retrieves institution-owned asset categories.
// ================================================================

using skolenza.application.DTOS.AssetDTO.category;
using skolenza.application.DTOS.Pagination;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace skolenza.persistance.Repositories;

/// <summary>
/// Provides database operations for asset categories.
/// </summary>
public class AssetCategoryRepository : IAssetCategoryRepository
{
    private readonly SkolenzaDbContext _context;
    private readonly ILogger<AssetCategoryRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetCategoryRepository"/> class.
    /// </summary>
    public AssetCategoryRepository(
        SkolenzaDbContext context,
        ILogger<AssetCategoryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Create

    /// <inheritdoc />
    public async Task<AssetCategory?> CreateAsync(
        AssetCategory entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (string.IsNullOrWhiteSpace(entity.CategoryName))
        {
            throw new ArgumentException("Category name is required.", nameof(entity));
        }

        var isDuplicate = await _context.AssetCategories.AnyAsync(
            category => category.InstitutionId == entity.InstitutionId
                && category.CategoryName.ToLower() == entity.CategoryName.ToLower()
                && !category.IsSoftDeleted,
            cancellationToken);
        if (isDuplicate)
        {
            throw new InvalidOperationException("An asset category with the same name already exists.");
        }

        await _context.AssetCategories.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Asset Category {AssetCategoryId} was created for institution {InstitutionId}.",
            entity.Id,
            entity.InstitutionId);
        return entity;
    }

    #endregion

    #region Update

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(AssetCategory entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return await _context.SaveChangesAsync(cancellationToken) > 0;
    }

    #endregion

    #region Delete

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(
        long id,
        long institutionId,
        long staffMemberId,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdForInstitutionAsync(id, institutionId, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.IsSoftDeleted = true;
        entity.IsActive = false;
        entity.SoftDeletedById = staffMemberId;
        entity.SoftDeletedDateTime = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    #endregion

    #region Queries

    /// <inheritdoc />
    public async Task<AssetCategory?> GetByIdForInstitutionAsync(
        long id,
        long institutionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.AssetCategories.FirstOrDefaultAsync(
            category => category.Id == id
                && category.InstitutionId == institutionId
                && !category.IsSoftDeleted,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PagedResponseDTO<GetCategoryResponseDTO>> GetAllAsync(
        long institutionId,
        GetCategoryReqestDTO dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var pageNumber = dto.PageNumber <= 0 ? 1 : dto.PageNumber;
        var pageSize = dto.PageSize <= 0 ? 10 : dto.PageSize;

        IQueryable<AssetCategory> query = _context.AssetCategories
            .AsNoTracking()
            .Where(category => category.InstitutionId == institutionId && !category.IsSoftDeleted);

        if (dto.Id > 0)
        {
            query = query.Where(category => category.Id == dto.Id);
        }

        if (dto.IsActive.HasValue)
        {
            query = query.Where(category => category.IsActive == dto.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        query = dto.SortOrder?.ToLowerInvariant() == "asc"
            ? query.OrderBy(category => category.Id)
            : query.OrderByDescending(category => category.Id);

        var data = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(category => new GetCategoryResponseDTO
            {
                Id = category.Id,
                CategoryName = category.CategoryName,
                Remark = category.Remark,
                IsActive = category.IsActive,
                HasMultipleUser = category.HasMultipleUser
            })
            .ToListAsync(cancellationToken);

        return new PagedResponseDTO<GetCategoryResponseDTO>(data, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Retrieves active asset categories for existing internal callers.
    /// </summary>
    public async Task<List<AssetCategory>> GetAllAssetCategoryAsync(long institutionId, bool isActive)
    {
        return await _context.AssetCategories
            .Where(category => category.InstitutionId == institutionId
                && category.IsActive == isActive
                && !category.IsSoftDeleted)
            .OrderByDescending(category => category.Id)
            .ToListAsync();
    }

    #endregion
}
