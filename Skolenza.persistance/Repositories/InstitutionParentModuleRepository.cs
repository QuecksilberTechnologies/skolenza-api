// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Queries Institution-enabled Header Module entitlements and stages focused status cascades.
// ================================================================

using skolenza.application.DTOS.Module.InstitutionParentModule;
using skolenza.application.DTOS.Pagination;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace skolenza.persistance.Repositories;

/// <summary>
/// Provides Institution Parent Module reads and focused status cascades using <see cref="InstitutionEnabledModule"/> as the entitlement source.
/// </summary>
public sealed class InstitutionParentModuleRepository : IInstitutionParentModuleRepository
{
    #region Fields

    private readonly SkolenzaDbContext _context;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="InstitutionParentModuleRepository"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public InstitutionParentModuleRepository(SkolenzaDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    #endregion

    #region Institution Header Tree

    /// <inheritdoc />
    public async Task<List<InstitutionParentModuleReadModel>> GetHeaderTreeAsync(
        long institutionId,
        bool? isEnabled,
        CancellationToken cancellationToken)
    {
        var parentQuery = _context.InstitutionEnabledModules
            .AsNoTracking()
            .Where(entitlement =>
                entitlement.InstitutionId == institutionId &&
                entitlement.ParentModuleId == null &&
                entitlement.IsLeafNode != true &&
                entitlement.Module.IsModuleDisplayInUI);

        if (isEnabled.HasValue)
        {
            parentQuery = parentQuery.Where(entitlement => entitlement.IsEnabled == isEnabled.Value);
        }

        var parentModules = await parentQuery
            .OrderBy(entitlement => entitlement.Module.ItemPriority)
            .ThenBy(entitlement => entitlement.Module.ModuleName)
            .Select(ProjectReadModel)
            .ToListAsync(cancellationToken);

        if (parentModules.Count == 0)
        {
            return new List<InstitutionParentModuleReadModel>();
        }

        var parentModuleIds = parentModules.Select(module => module.Id).ToList();
        var childQuery = _context.InstitutionEnabledModules
            .AsNoTracking()
            .Where(entitlement =>
                entitlement.InstitutionId == institutionId &&
                entitlement.ParentModuleId.HasValue &&
                parentModuleIds.Contains(entitlement.ParentModuleId.Value) &&
                entitlement.IsLeafNode != true);

        if (isEnabled.HasValue)
        {
            childQuery = childQuery.Where(entitlement => entitlement.IsEnabled == isEnabled.Value);
        }

        var childModules = await childQuery
            .OrderBy(entitlement => entitlement.Module.ItemPriority)
            .ThenBy(entitlement => entitlement.Module.ModuleName)
            .Select(ProjectReadModel)
            .ToListAsync(cancellationToken);
        var childrenByParentModuleId = childModules
            .GroupBy(module => module.ParentModuleId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        foreach (var parentModule in parentModules)
        {
            parentModule.Children = childrenByParentModuleId.TryGetValue(parentModule.Id, out var children)
                ? children
                : new List<InstitutionParentModuleReadModel>();
        }

        return parentModules;
    }

    #endregion

    #region Institution Header Listing

    /// <inheritdoc />
    public async Task<PagedResponseDTO<InstitutionParentModuleReadModel>> GetPagedMainParentHeadersAsync(
        bool? isActive,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPageNumber = pageNumber > 0 ? pageNumber : 1;
        var normalizedPageSize = pageSize is > 0 and <= 100 ? pageSize : 10;
        var query = _context.InstitutionEnabledModules
            .AsNoTracking()
            .Where(entitlement =>
                entitlement.ParentModuleId == null &&
                entitlement.IsLeafNode == false);

        if (isActive.HasValue)
        {
            query = query.Where(entitlement => entitlement.IsEnabled == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var data = await query
            .OrderBy(entitlement => entitlement.Module.ItemPriority)
            .ThenBy(entitlement => entitlement.Module.ModuleName)
            .ThenBy(entitlement => entitlement.ModuleId)
            .Skip((normalizedPageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(ProjectReadModel)
            .ToListAsync(cancellationToken);

        return new PagedResponseDTO<InstitutionParentModuleReadModel>(
            data,
            totalCount,
            normalizedPageNumber,
            normalizedPageSize)
        {
            TotalPages = (int)Math.Ceiling(totalCount / (double)normalizedPageSize)
        };
    }

    #endregion

    #region Institution Header Lookup

    /// <inheritdoc />
    public Task<InstitutionParentModuleReadModel?> GetHeaderByModuleIdAsync(
        long institutionId,
        int moduleId,
        short moduleScope,
        CancellationToken cancellationToken)
    {
        return _context.InstitutionEnabledModules
            .AsNoTracking()
            .Where(entitlement =>
                entitlement.InstitutionId == institutionId &&
                entitlement.ModuleId == moduleId &&
                entitlement.IsLeafNode == false &&
                entitlement.Module.ModuleScope == moduleScope)
            .Select(ProjectReadModel)
            .FirstOrDefaultAsync(cancellationToken);
    }

    #endregion

    #region Institution Status Cascade

    /// <inheritdoc />
    public async Task<InstitutionParentModuleReadModel?> StageStatusCascadeAsync(
        long institutionId,
        int moduleId,
        bool isActive,
        long auditActorId,
        DateTime updatedDateTime,
        CancellationToken cancellationToken)
    {
        var target = await _context.InstitutionEnabledModules
            .Include(entitlement => entitlement.Module)
            .FirstOrDefaultAsync(
                entitlement =>
                    entitlement.InstitutionId == institutionId &&
                    entitlement.ModuleId == moduleId &&
                    entitlement.IsLeafNode == false,
                cancellationToken);

        if (target is null)
        {
            return null;
        }

        var institutionEntitlements = await _context.InstitutionEnabledModules
            .Where(entitlement => entitlement.InstitutionId == institutionId)
            .ToListAsync(cancellationToken);

        var childModuleIdsByParentModuleId = institutionEntitlements
            .Where(entitlement => entitlement.ParentModuleId.HasValue)
            .GroupBy(entitlement => entitlement.ParentModuleId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.Select(entitlement => entitlement.ModuleId).Distinct().ToList());
        var affectedModuleIds = new HashSet<int> { target.ModuleId };
        var pendingParentModuleIds = new Queue<int>();
        pendingParentModuleIds.Enqueue(target.ModuleId);

        while (pendingParentModuleIds.TryDequeue(out var parentModuleId))
        {
            if (!childModuleIdsByParentModuleId.TryGetValue(parentModuleId, out var childModuleIds))
            {
                continue;
            }

            foreach (var childModuleId in childModuleIds)
            {
                if (affectedModuleIds.Add(childModuleId))
                {
                    pendingParentModuleIds.Enqueue(childModuleId);
                }
            }
        }

        foreach (var entitlement in institutionEntitlements.Where(entitlement => affectedModuleIds.Contains(entitlement.ModuleId)))
        {
            if (entitlement.IsEnabled == isActive)
            {
                continue;
            }

            entitlement.IsEnabled = isActive;
            entitlement.UpdatedById = auditActorId;
            entitlement.UpdatedDateTime = updatedDateTime;
        }

        var affectedModuleIdList = affectedModuleIds.ToList();
        var operations = await _context.InstitutionEnabledOperations
            .Where(operation =>
                operation.InstitutionId == institutionId &&
                affectedModuleIdList.Contains(operation.ModuleId))
            .ToListAsync(cancellationToken);

        foreach (var operation in operations)
        {
            var requestedOperationEnabledState = isActive && operation.IsOperationUsed == true;
            if (operation.IsEnabled == requestedOperationEnabledState)
            {
                continue;
            }

            operation.IsEnabled = requestedOperationEnabledState;
            operation.UpdatedById = auditActorId;
            operation.UpdatedDateTime = updatedDateTime;
        }

        return CreateReadModel(target);
    }

    #endregion

    #region Projection

    private static readonly Expression<Func<InstitutionEnabledModule, InstitutionParentModuleReadModel>> ProjectReadModel = entitlement =>
        new InstitutionParentModuleReadModel
        {
            InstitutionId = entitlement.InstitutionId,
            Id = entitlement.ModuleId,
            ModuleCode = entitlement.Module.ModuleCode,
            ModuleName = entitlement.Module.ModuleName,
            DisplayName = entitlement.Module.DisplayName,
            UrlPath = entitlement.Module.Urlpath,
            ImageIconWeb = entitlement.Module.ImageIconWeb,
            ImageIconMobile = entitlement.Module.ImageIconMobile,
            ItemPriority = entitlement.Module.ItemPriority,
            ParentModuleId = entitlement.ParentModuleId,
            IsLeafNode = entitlement.IsLeafNode,
            IsEnabled = entitlement.IsEnabled,
            ModuleScope = entitlement.Module.ModuleScope
        };

    private static InstitutionParentModuleReadModel CreateReadModel(InstitutionEnabledModule entitlement)
    {
        return new InstitutionParentModuleReadModel
        {
            InstitutionId = entitlement.InstitutionId,
            Id = entitlement.ModuleId,
            ModuleCode = entitlement.Module.ModuleCode,
            ModuleName = entitlement.Module.ModuleName,
            DisplayName = entitlement.Module.DisplayName,
            UrlPath = entitlement.Module.Urlpath,
            ImageIconWeb = entitlement.Module.ImageIconWeb,
            ImageIconMobile = entitlement.Module.ImageIconMobile,
            ItemPriority = entitlement.Module.ItemPriority,
            ParentModuleId = entitlement.ParentModuleId,
            IsLeafNode = entitlement.IsLeafNode,
            IsEnabled = entitlement.IsEnabled,
            ModuleScope = entitlement.Module.ModuleScope
        };
    }

    #endregion
}
