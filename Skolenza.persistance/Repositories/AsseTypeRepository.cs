// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Persists and retrieves institution-owned asset types.
// ================================================================

using skolenza.application.DTOS.AssetDTO.type;
using skolenza.application.DTOS.Pagination;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace skolenza.persistance.Repositories;

/// <summary>
/// Provides database operations for asset types.
/// </summary>
public class AssetTypeRepository : IAssetTypeRepository
{
    private readonly SkolenzaDbContext _context;
    private readonly ILogger<AssetTypeRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetTypeRepository"/> class.
    /// </summary>
    /// <param name="context">The workforce database context.</param>
    /// <param name="logger">The repository logger.</param>
    public AssetTypeRepository(
        SkolenzaDbContext context,
        ILogger<AssetTypeRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Create

    /// <inheritdoc />
    public async Task<AssetType?> CreateAsync(AssetType entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (string.IsNullOrWhiteSpace(entity.TypeName))
        {
            throw new ArgumentException("TypeName is required.", nameof(entity));
        }

        var duplicateExists = await _context.AssetTypes.AnyAsync(
            type => type.InstitutionId == entity.InstitutionId
                && type.TypeName.ToLower() == entity.TypeName.ToLower()
                && !type.IsSoftDeleted,
            cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException(
                $"Asset Type '{entity.TypeName}' already exists for this institution.");
        }

        await _context.AssetTypes.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Asset Type {AssetTypeId} was created for institution {InstitutionId}.",
            entity.Id,
            entity.InstitutionId);

        return entity;
    }

    #endregion

    #region Update

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(AssetType entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var affected = await _context.SaveChangesAsync(cancellationToken);

        if (affected > 0)
        {
            _logger.LogInformation(
                "Asset Type {AssetTypeId} was updated for institution {InstitutionId}.",
                entity.Id,
                entity.InstitutionId);
            return true;
        }

        _logger.LogInformation(
            "No changes were detected for Asset Type {AssetTypeId}.",
            entity.Id);
        return false;
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
        entity.DeletedDateTime = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Asset Type {AssetTypeId} was soft-deleted for institution {InstitutionId}.",
            id,
            institutionId);

        return true;
    }

    #endregion

    #region Queries

    /// <inheritdoc />
    public async Task<AssetType?> GetByIdForInstitutionAsync(
        long id,
        long institutionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.AssetTypes.FirstOrDefaultAsync(
            type => type.Id == id
                && type.InstitutionId == institutionId
                && !type.IsSoftDeleted,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PagedResponseDTO<GetTypeResponseDTO>> GetAllAsync(
        long institutionId,
        GetTypeRequestDTO dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var pageNumber = dto.PageNumber <= 0 ? 1 : dto.PageNumber;
        var pageSize = dto.PageSize <= 0 ? 10 : dto.PageSize;

        IQueryable<AssetType> query = _context.AssetTypes
            .AsNoTracking()
            .Where(type => type.InstitutionId == institutionId && !type.IsSoftDeleted);

        if (dto.TypeId is > 0)
        {
            query = query.Where(type => type.Id == dto.TypeId.Value);
        }

        if (dto.CategoryId is > 0)
        {
            query = query.Where(type => type.AssetCategoryId == dto.CategoryId.Value);
        }

        if (dto.IsActive.HasValue)
        {
            query = query.Where(type => type.IsActive == dto.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = dto.SortOrder?.ToLowerInvariant() == "asc"
            ? query.OrderBy(type => type.Id)
            : query.OrderByDescending(type => type.AddedDateTime);

        var data = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(type => new GetTypeResponseDTO
            {
                Id = type.Id,
                InstitutionId = type.InstitutionId,
                AssetCategoryId = type.AssetCategoryId,
                CategoryName = type.AssetCategory != null
                    ? type.AssetCategory.CategoryName
                    : null,
                TypeName = type.TypeName,
                Description = type.Description,
                IsActive = type.IsActive ?? false,
                AddedById = type.AddedById,
                AddedDateTime = type.AddedDateTime,
                UpdatedById = type.UpdatedById,
                UpdatedDateTime = type.UpdatedDateTime
            })
            .ToListAsync(cancellationToken);

        return new PagedResponseDTO<GetTypeResponseDTO>(
            data,
            totalCount,
            pageNumber,
            pageSize);
    }

    #endregion
}
