// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Persists and retrieves institution-owned asset statuses.
// ================================================================

using skolenza.application.DTOS.AssetDTO.status;
using skolenza.application.DTOS.Pagination;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace skolenza.persistance.Repositories;

/// <summary>
/// Provides database operations for asset statuses.
/// </summary>
public class AssetStatusRepository : IAssetStatusRepository
{
    private readonly SkolenzaDbContext _context;
    private readonly ILogger<AssetStatusRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetStatusRepository"/> class.
    /// </summary>
    public AssetStatusRepository(
        SkolenzaDbContext context,
        ILogger<AssetStatusRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Create

    /// <inheritdoc />
    public async Task<AssetStatus?> CreateAsync(
        AssetStatus entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await _context.AssetStatuses.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Asset Status {AssetStatusId} was created for institution {InstitutionId}.",
            entity.Id,
            entity.InstitutionId);
        return entity;
    }

    #endregion

    #region Update

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(AssetStatus entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var affected = await _context.SaveChangesAsync(cancellationToken);
        if (affected > 0)
        {
            _logger.LogInformation(
                "Asset Status {AssetStatusId} was updated for institution {InstitutionId}.",
                entity.Id,
                entity.InstitutionId);
            return true;
        }

        return false;
    }

    #endregion

    #region Delete

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(
        int id,
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
        entity.UpdatedById = staffMemberId;
        entity.UpdatedDateTime = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    #endregion

    #region Queries

    /// <inheritdoc />
    public async Task<AssetStatus?> GetByIdForInstitutionAsync(
        int id,
        long institutionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.AssetStatuses.FirstOrDefaultAsync(
            status => status.Id == id
                && status.InstitutionId == institutionId
                && status.IsSoftDeleted != true,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AssetStatus?> GetByIdAsync(int? id)
    {
        return await _context.AssetStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(status => status.Id == id && status.IsSoftDeleted != true);
    }

    /// <inheritdoc />
    public async Task<PagedResponseDTO<GetStatusResponseDTO>> GetAllAsync(
        long institutionId,
        GetStatusRequestDTO dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var pageNumber = dto.PageNumber <= 0 ? 1 : dto.PageNumber;
        var pageSize = dto.PageSize <= 0 ? 10 : dto.PageSize;

        IQueryable<AssetStatus> query = _context.AssetStatuses
            .AsNoTracking()
            .Where(status => status.InstitutionId == institutionId && status.IsSoftDeleted != true);

        if (dto.IsActive)
        {
            query = query.Where(status => status.IsActive == true);
        }

        if (dto.Id > 0)
        {
            query = query.Where(status => status.Id == dto.Id);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var data = await query
            .OrderByDescending(status => status.AddedDateTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(status => new GetStatusResponseDTO
            {
                Id = status.Id,
                StatusName = status.StatusName,
                Description = status.Description,
                IsActive = status.IsActive ?? false,
                ColorKey = status.ColorKey
            })
            .ToListAsync(cancellationToken);

        return new PagedResponseDTO<GetStatusResponseDTO>(data, totalCount, pageNumber, pageSize);
    }

    #endregion
}
