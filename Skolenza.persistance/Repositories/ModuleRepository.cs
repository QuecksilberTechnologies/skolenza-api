// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Provides module, module-operation, and deterministic shared Common-navigation persistence queries.
// ================================================================

using AutoMapper;
using skolenza.application.DTOs.UserLogin;
using skolenza.application.DTOS.Module.CommonModule;
using skolenza.application.DTOS.Module.CommonMenu;
using skolenza.application.DTOS.Module.ManualModule;
using skolenza.application.DTOS.Module.ParentModule;

using skolenza.application.DTOS.Module.SubModule;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using skolenza.application.Constants;
using skolenza.application.DTOS.Host;
using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.FeaturePages;
using skolenza.application.DTOS.Navigation;
using System.Linq.Expressions;


namespace skolenza.persistance.Repositories
{
    /// <summary>
    /// Provides persistence operations for application modules.
    /// </summary>
    public class ModuleRepository : IModuleRepository
    {
        private readonly SkolenzaDbContext? _context;
        private readonly ILogger? _logger;
       
        private readonly IMapper _mapper;
        public ModuleRepository(SkolenzaDbContext? context, ILogger<ModuleRepository>? logger, IMapper mapper)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
            
        }

        #region Host Module Queries

        /// <summary>
        /// Retrieves a database-paged set of Host-scope modules, optionally filtered by their active state.
        /// </summary>
        /// <param name="isActive">When supplied, limits results to modules with the specified active state.</param>
        /// <param name="pageNumber">The normalized one-based page number.</param>
        /// <param name="pageSize">The normalized number of rows per page.</param>
        /// <param name="cancellationToken">A token to observe while executing the database query.</param>
        /// <returns>The requested Host-scope module page.</returns>
        public async Task<PagedResponseDTO<GetHostModuleResponseDTO>> GetHostModulesAsync(
            bool? isActive,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
        {
            if (_context?.Modules == null)
            {
                _logger?.LogWarning("Unable to retrieve Host modules because the Module DbSet is unavailable.");
                return new PagedResponseDTO<GetHostModuleResponseDTO>(
                    new List<GetHostModuleResponseDTO>(),
                    0,
                    pageNumber,
                    pageSize)
                {
                    TotalPages = 0
                };
            }

            try
            {
                // Module has no IsSoftDeleted property, so only its supported scope and activity filters apply.
                var query = _context.Modules
                    .AsNoTracking()
                    .Where(x =>
                        x.ModuleScope == AppConstants.HostModuleScope &&
                        (!isActive.HasValue || x.IsActive == isActive.Value));

                var totalCount = await query.CountAsync(cancellationToken);
                var data = await query
                    .OrderBy(x => x.ItemPriority)
                    .ThenBy(x => x.ModuleName)
                    .ThenBy(x => x.Id)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new GetHostModuleResponseDTO
                    {
                        Id = x.Id,
                        InstitutionId = x.InstitutionId,
                        ModuleCode = x.ModuleCode,
                        ModuleName = x.ModuleName,
                        DisplayName = x.DisplayName,
                        Urlpath = x.Urlpath,
                        ParentModuleId = x.ParentModuleId,
                        IsLeafNode = x.IsLeafNode,
                        IsModuleDisplayInUi = x.IsModuleDisplayInUI,
                        IsCommonMenu = x.IsCommonMenu,
                        ModuleScope = x.ModuleScope,
                        IsActive = x.IsActive,
                        ImageIconWeb = x.ImageIconWeb,
                        ImageIconMobile = x.ImageIconMobile,
                        ItemPriority = x.ItemPriority,
                        Remark = x.Remark,
                        AddedById = x.AddedById,
                        AddedDateTime = x.AddedDateTime,
                        UpdatedById = x.UpdatedById,
                        UpdatedDateTime = x.UpdatedDateTime
                    })
                    .ToListAsync(cancellationToken);

                return new PagedResponseDTO<GetHostModuleResponseDTO>(data, totalCount, pageNumber, pageSize)
                {
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving Host modules. IsActive: {IsActive}", isActive);
                return new PagedResponseDTO<GetHostModuleResponseDTO>(
                    new List<GetHostModuleResponseDTO>(),
                    0,
                    pageNumber,
                    pageSize)
                {
                    TotalPages = 0
                };
            }
        }

        /// <summary>
        /// Retrieves one Host-scope module by identifier, optionally filtered by its active state.
        /// </summary>
        /// <param name="id">The module identifier.</param>
        /// <param name="isActive">When supplied, limits the result to the specified active state.</param>
        /// <param name="cancellationToken">A token to observe while executing the database query.</param>
        /// <returns>The matching Host-scope module, or <see langword="null"/> when none exists.</returns>
        public async Task<GetHostModuleResponseDTO?> GetHostModuleByIdAsync(
            int id,
            bool? isActive,
            CancellationToken cancellationToken)
        {
            if (_context?.Modules == null)
            {
                _logger?.LogWarning("Unable to retrieve Host module {ModuleId} because the Module DbSet is unavailable.", id);
                return null;
            }

            try
            {
                // Module has no IsSoftDeleted property, so only its supported identifier, scope, and activity filters apply.
                return await _context.Modules
                    .AsNoTracking()
                    .Where(x =>
                        x.Id == id &&
                        x.ModuleScope == AppConstants.HostModuleScope &&
                        (!isActive.HasValue || x.IsActive == isActive.Value))
                    .Select(x => new GetHostModuleResponseDTO
                    {
                        Id = x.Id,
                        InstitutionId = x.InstitutionId,
                        ModuleCode = x.ModuleCode,
                        ModuleName = x.ModuleName,
                        DisplayName = x.DisplayName,
                        Urlpath = x.Urlpath,
                        ParentModuleId = x.ParentModuleId,
                        IsLeafNode = x.IsLeafNode,
                        IsModuleDisplayInUi = x.IsModuleDisplayInUI,
                        IsCommonMenu = x.IsCommonMenu,
                        ModuleScope = x.ModuleScope,
                        IsActive = x.IsActive,
                        ImageIconWeb = x.ImageIconWeb,
                        ImageIconMobile = x.ImageIconMobile,
                        ItemPriority = x.ItemPriority,
                        Remark = x.Remark,
                        AddedById = x.AddedById,
                        AddedDateTime = x.AddedDateTime,
                        UpdatedById = x.UpdatedById,
                        UpdatedDateTime = x.UpdatedDateTime
                    })
                    .FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving Host module {ModuleId}. IsActive: {IsActive}", id, isActive);
                throw;
            }
        }

        #endregion

        public async Task<Module?> GetModuleByIdAsync(long moduleId)
        {
            try
            {
               
                return await _context.Modules.FirstOrDefaultAsync(m => m.Id == moduleId && m.IsActive == true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetModuleByIdAsync for ID {ModuleId}", moduleId);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<Module?> GetModuleByIdIncludingInactiveAsync(long moduleId)
        {
            try
            {
                return await _context.Modules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(module => module.Id == moduleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving Module code for ID {ModuleId}", moduleId);
                return null;
            }
        }

        #region Authenticated Navigation Queries

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<NavigationMenuItemResponseDTO>> GetInstitutionNavigationMenuAsync(
            long institutionId,
            long staffMemberId,
            CancellationToken cancellationToken = default)
        {
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");

            // InstitutionEnabledModule is the institution's entitlement snapshot.  Master-module
            // activity/visibility must not revoke a feature that was already assigned to
            // a institution; the master record is used only for the current display metadata.
            var institutionModules = await (
                from institutionModule in context.InstitutionEnabledModules.AsNoTracking()
                join module in context.Modules.AsNoTracking() on institutionModule.ModuleId equals module.Id
                where institutionModule.InstitutionId == institutionId &&
                      institutionModule.IsEnabled &&
                      module.ModuleScope == (short)AppConstants.InstitutionModuleScope
                select new NavigationModuleRecord(
                    module.Id,
                    module.ModuleCode,
                    module.ModuleName,
                    module.PageName,
                    module.DisplayName,
                    module.Urlpath,
                    module.ImageIconWeb,
                    institutionModule.ParentModuleId,
                    institutionModule.IsLeafNode ?? false,
                    module.ModuleScope,
                    module.ItemPriority ?? int.MaxValue))
                .ToListAsync(cancellationToken);

            var allowedOperations = (await (
                // Intentionally do not filter IsPrimaryRole: a user's effective navigation is the
                // union of its active primary and secondary role grants.
                from userRole in context.UserRoles.AsNoTracking()
                join role in context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                join permission in context.RoleModuleAndPermissions.AsNoTracking()
                    on userRole.RoleId equals permission.RoleId
                join institutionModule in context.InstitutionEnabledModules.AsNoTracking()
                    on new { InstitutionId = institutionId, ModuleId = permission.ModuleId!.Value }
                    equals new { institutionModule.InstitutionId, institutionModule.ModuleId }
                join institutionOperation in context.InstitutionEnabledOperations.AsNoTracking()
                    on new { InstitutionId = institutionModule.InstitutionId, institutionModule.ModuleId, OperationId = permission.OperationId!.Value }
                    equals new { institutionOperation.InstitutionId, institutionOperation.ModuleId, institutionOperation.OperationId }
                join module in context.Modules.AsNoTracking() on permission.ModuleId!.Value equals module.Id
                join operation in context.Operations.AsNoTracking() on permission.OperationId!.Value equals operation.Id
                where userRole.StaffMemberId == staffMemberId &&
                      userRole.IsActive &&
                      userRole.IsSoftDeleted != true &&
                      userRole.RoleId.HasValue &&
                      role.InstitutionId == institutionId &&
                      role.IsActive &&
                      role.IsSoftDeleted != true &&
                      userRole.StaffMember != null &&
                      userRole.StaffMember.InstitutionId == institutionId &&
                      userRole.StaffMember.IsActive &&
                      userRole.StaffMember.IsSoftDeleted != true &&
                      permission.ModuleId.HasValue &&
                      permission.OperationId.HasValue &&
                      permission.IsActive == true &&
                      permission.IsSoftDeleted != true &&
                      permission.HasAccess == true &&
                      institutionModule.IsEnabled &&
                      institutionOperation.IsEnabled &&
                      institutionOperation.IsOperationUsed == true &&
                      module.ModuleScope == (short)AppConstants.InstitutionModuleScope
                select new NavigationOperationRecord(
                    module.Id,
                    operation.Id,
                    operation.OperationName,
                    operation.IconImage))
                .ToListAsync(cancellationToken))
                .Distinct()
                .ToArray();

            return BuildNavigationTree(institutionModules, allowedOperations);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<NavigationMenuItemResponseDTO>> GetHostNavigationMenuAsync(
            long hostRoleId,
            CancellationToken cancellationToken = default)
        {
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");

            var hostModules = await context.Modules
                .AsNoTracking()
                .Where(module =>
                    module.ModuleScope == (short)AppConstants.HostModuleScope &&
                    module.IsActive &&
                    module.IsModuleDisplayInUI)
                .Select(module => new NavigationModuleRecord(
                    module.Id,
                    module.ModuleCode,
                    module.ModuleName,
                    module.PageName,
                    module.DisplayName,
                    module.Urlpath,
                    module.ImageIconWeb,
                    module.ParentModuleId,
                    module.IsLeafNode ?? false,
                    module.ModuleScope,
                    module.ItemPriority ?? int.MaxValue))
                .ToListAsync(cancellationToken);

            var hostModuleOperations =
                from moduleOperation in context.ModuleOperationMappings.AsNoTracking()
                join module in context.Modules.AsNoTracking() on moduleOperation.ModuleId equals module.Id
                join operation in context.Operations.AsNoTracking() on moduleOperation.OperationId equals operation.Id
                where module.ModuleScope == (short)AppConstants.HostModuleScope &&
                      module.IsActive &&
                      module.IsModuleDisplayInUI &&
                      moduleOperation.IsActive == true &&
                      moduleOperation.IsOperational == true &&
                      operation.IsActive
                select new { module, operation };

            var allowedOperations = (await (
                from row in hostModuleOperations
                join permission in context.HostRoleModuleAndPermissions.AsNoTracking()
                    on new { ModuleId = row.module.Id, OperationId = row.operation.Id }
                    equals new { permission.ModuleId, permission.OperationId }
                where permission.HostRoleId == hostRoleId &&
                      permission.IsActive &&
                      !permission.IsSoftDeleted
                select new NavigationOperationRecord(
                    row.module.Id,
                    row.operation.Id,
                    row.operation.OperationName,
                    row.operation.IconImage))
                .ToListAsync(cancellationToken))
                .Distinct()
                .ToArray();

            // MyMenu is role/permission driven for every Host role. A module is
            // visible only when its active operational mapping is granted to the
            // current HostRoleModuleAndPermission set.
            return BuildNavigationTree(hostModules, allowedOperations);
        }

        private static IReadOnlyCollection<NavigationMenuItemResponseDTO> BuildNavigationTree(
            IReadOnlyCollection<NavigationModuleRecord> scopedModules,
            IReadOnlyCollection<NavigationOperationRecord> allowedOperations)
        {
            var modulesById = scopedModules
                .GroupBy(module => module.Id)
                .ToDictionary(group => group.Key, group => group.First());
            var visibleModuleIds = new HashSet<int>();
            var operationsByModuleId = allowedOperations
                .GroupBy(operation => operation.ModuleId)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyCollection<NavigationOperationResponseDTO>)group
                        .GroupBy(operation => operation.OperationId)
                        .Select(operationGroup => operationGroup.First())
                        .OrderBy(operation => operation.OperationName)
                        .ThenBy(operation => operation.OperationId)
                        .Select(operation => new NavigationOperationResponseDTO
                        {
                            Id = operation.OperationId,
                            Name = operation.OperationName,
                            IconKey = operation.IconKey
                        })
                        .ToArray());

            foreach (var operationModuleId in operationsByModuleId.Keys)
            {
                AddModuleAndAncestors(operationModuleId, modulesById, visibleModuleIds);
            }

            var childrenByParentId = modulesById.Values
                .Where(module =>
                    visibleModuleIds.Contains(module.Id) &&
                    module.ParentModuleId.HasValue &&
                    visibleModuleIds.Contains(module.ParentModuleId.Value))
                .GroupBy(module => module.ParentModuleId!.Value)
                .ToDictionary(group => group.Key, group => OrderNavigationModules(group));
            var roots = OrderNavigationModules(
                modulesById.Values.Where(module =>
                    visibleModuleIds.Contains(module.Id) &&
                    (!module.ParentModuleId.HasValue ||
                     !visibleModuleIds.Contains(module.ParentModuleId.Value))));

            return roots
                .Select(module => BuildNavigationItem(module, childrenByParentId, operationsByModuleId, new HashSet<int>()))
                .ToArray();
        }

        private static void AddModuleAndAncestors(
            int moduleId,
            IReadOnlyDictionary<int, NavigationModuleRecord> modulesById,
            ISet<int> visibleModuleIds)
        {
            var visitedModuleIds = new HashSet<int>();
            var currentModuleId = moduleId;

            while (modulesById.TryGetValue(currentModuleId, out var module) &&
                   visitedModuleIds.Add(currentModuleId))
            {
                visibleModuleIds.Add(currentModuleId);
                if (!module.ParentModuleId.HasValue)
                {
                    break;
                }

                currentModuleId = module.ParentModuleId.Value;
            }
        }

        private static NavigationMenuItemResponseDTO BuildNavigationItem(
            NavigationModuleRecord module,
            IReadOnlyDictionary<int, IReadOnlyCollection<NavigationModuleRecord>> childrenByParentId,
            IReadOnlyDictionary<int, IReadOnlyCollection<NavigationOperationResponseDTO>> operationsByModuleId,
            ISet<int> branchModuleIds)
        {
            if (!branchModuleIds.Add(module.Id))
            {
                return CreateNavigationItem(module, operationsByModuleId.GetValueOrDefault(module.Id), Array.Empty<NavigationMenuItemResponseDTO>());
            }

            var children = childrenByParentId.TryGetValue(module.Id, out var childModules)
                ? childModules
                    .Select(child => BuildNavigationItem(child, childrenByParentId, operationsByModuleId, new HashSet<int>(branchModuleIds)))
                    .ToArray()
                : Array.Empty<NavigationMenuItemResponseDTO>();

            return CreateNavigationItem(module, operationsByModuleId.GetValueOrDefault(module.Id), children);
        }

        private static NavigationMenuItemResponseDTO CreateNavigationItem(
            NavigationModuleRecord module,
            IReadOnlyCollection<NavigationOperationResponseDTO>? operations,
            IReadOnlyCollection<NavigationMenuItemResponseDTO> children) =>
            new()
            {
                Id = module.Id,
                ModuleCode = module.ModuleCode,
                ModuleName = module.ModuleName,
                PageName = module.PageName,
                DisplayName = module.DisplayName,
                UrlPath = module.UrlPath,
                IconKey = module.IconKey,
                ParentModuleId = module.ParentModuleId,
                IsLeafNode = module.IsLeafNode,
                ModuleScope = module.ModuleScope,
                Operations = operations ?? Array.Empty<NavigationOperationResponseDTO>(),
                Children = children
            };

        private static IReadOnlyCollection<NavigationModuleRecord> OrderNavigationModules(
            IEnumerable<NavigationModuleRecord> modules) =>
            modules
                .OrderBy(module => module.ItemPriority < 0 ? int.MaxValue : module.ItemPriority)
                .ThenBy(module => module.ModuleName)
                .ThenBy(module => module.Id)
                .ToArray();

        private sealed record NavigationModuleRecord(
            int Id,
            string? ModuleCode,
            string ModuleName,
            string? PageName,
            string? DisplayName,
            string? UrlPath,
            string? IconKey,
            int? ParentModuleId,
            bool IsLeafNode,
            short ModuleScope,
            int ItemPriority);

        private sealed record NavigationOperationRecord(
            int ModuleId,
            int OperationId,
            string OperationName,
            string? IconKey);

        #endregion

        #region Common Navigation Queries

        /// <inheritdoc />
        public async Task<Module?> GetCommonMenuParentAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.Modules
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        module =>
                            module.IsCommonMenu &&
                            module.IsModuleDisplayInUI &&
                            module.IsActive &&
                            module.ParentModuleId == null,
                        cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching CommonMenu parent module.", ex);
            }
        }

        /// <inheritdoc />
        public async Task<List<ModuleDTO>> GetCommonMenuTreeAsync(
            int? parentId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Load active UI-visible candidates once, then compose the recursive hierarchy without N+1 queries.
                var allModules = await _context.Modules
                    .AsNoTracking()
                    .Where(module => module.IsActive && module.IsModuleDisplayInUI)
                    .ToListAsync(cancellationToken);

                var result = BuildMenuTree(allModules, parentId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in GetCommonMenuTreeAsync with ParentId={ParentId}", parentId);
                throw;
            }
        }


        private List<ModuleDTO> BuildMenuTree(List<Module> allModules, int? parentId)
        {
            return allModules
                .Where(m => m.ParentModuleId == parentId)
                .OrderBy(m => m.ItemPriority < 0 ? int.MaxValue : m.ItemPriority)
                .ThenBy(m => m.ModuleName)
                .ThenBy(m => m.Id)
                .Select(m => new ModuleDTO
                {
                    Id = m.Id,
                    ModuleName = m.ModuleName,
                    URLPath = m.Urlpath,
                    IsLeafNode = m.IsLeafNode,
                    DisplayName = m.DisplayName,
                    ImageIconMobile = m.ImageIconMobile,
                    ImageIconWeb = m.ImageIconWeb,
                    ItemPriority = m.ItemPriority,
                    Children = BuildMenuTree(allModules, m.Id)
                })
                .ToList();
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<CommonMenuItemResponseDTO>?> GetCommonMenuHierarchyAsync(
            CancellationToken cancellationToken = default)
        {
            // Resolve the unique Common root using hierarchy semantics rather than database row order.
            var commonRoot = await GetCommonMenuParentAsync(cancellationToken);
            if (commonRoot == null)
            {
                return null;
            }

            // Reuse the established child-only CommonItems hierarchy shape.
            var commonItems = await GetCommonMenuTreeAsync(commonRoot.Id, cancellationToken);
            return MapCommonMenuItems(commonItems);
        }

        /// <summary>
        /// Maps the existing Common-menu tree model to the standalone API contract without changing its hierarchy.
        /// </summary>
        /// <param name="items">The existing child-only Common-menu tree.</param>
        /// <returns>The standalone Common-menu response items.</returns>
        private static IReadOnlyCollection<CommonMenuItemResponseDTO> MapCommonMenuItems(
            IReadOnlyCollection<ModuleDTO> items)
        {
            return items
                .Select(item => new CommonMenuItemResponseDTO
                {
                    ModuleId = item.Id,
                    ModuleName = item.ModuleName,
                    DisplayName = item.DisplayName,
                    UrlPath = item.URLPath,
                    ImageIconWeb = item.ImageIconWeb,
                    ImageIconMobile = item.ImageIconMobile,
                    IsLeafNode = item.IsLeafNode ?? item.Children.Count == 0,
                    ItemPriority = item.ItemPriority,
                    Children = MapCommonMenuItems(item.Children)
                })
                .ToArray();
        }

        #endregion

        #region Feature Pages Queries

        /// <summary>
        /// Retrieves active master feature headers with child headers, operational leaf pages, and active Operation configuration.
        /// </summary>
        /// <param name="scope">1 for Institution, 2 for Host, 3 for Common, or <see langword="null"/> for every scope.</param>
        /// <param name="cancellationToken">A token to observe while executing the database queries.</param>
        /// <returns>The ordered active master feature headers with their active operational pages.</returns>
        public async Task<IReadOnlyCollection<FeaturePageResponseDTO>> GetActiveFeaturePagesAsync(
            short? scope,
            CancellationToken cancellationToken)
        {
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");

            var allModules = await context.Modules
                .AsNoTracking()
                .Where(module => module.InstitutionId == null && module.IsActive)
                .OrderBy(module => module.ItemPriority ?? int.MaxValue)
                .ThenBy(module => module.ModuleName)
                .ThenBy(module => module.Id)
                .Select(module => new FeaturePageResponseDTO
                {
                    Id = module.Id,
                    ModuleCode = module.ModuleCode,
                    ModuleName = module.ModuleName,
                    DisplayName = module.DisplayName,
                    UrlPath = module.Urlpath,
                    IconKey = module.ImageIconWeb,
                    ParentModuleId = module.ParentModuleId,
                    IsLeafNode = module.IsLeafNode ?? false,
                    IsModuleDisplayInUI = module.IsModuleDisplayInUI,
                    ModuleScope = module.ModuleScope,
                    IsCommonMenu = module.IsCommonMenu
                })
                .ToListAsync(cancellationToken);

            if (allModules.Count == 0)
            {
                return Array.Empty<FeaturePageResponseDTO>();
            }

            var allModulesById = allModules.ToDictionary(module => module.Id);
            var modules = scope switch
            {
                1 => allModules
                    .Where(module =>
                        !IsInCommonHierarchy(module, allModulesById) &&
                        module.ModuleScope == AppConstants.InstitutionModuleScope)
                    .ToList(),
                2 => allModules
                    .Where(module =>
                        !IsInCommonHierarchy(module, allModulesById) &&
                        module.ModuleScope == AppConstants.HostModuleScope)
                    .ToList(),
                3 => allModules
                    .Where(module => IsInCommonHierarchy(module, allModulesById))
                    .ToList(),
                null => allModules,
                _ => new List<FeaturePageResponseDTO>()
            };

            if (modules.Count == 0)
            {
                return Array.Empty<FeaturePageResponseDTO>();
            }

            foreach (var module in modules)
            {
                var isCommon = IsInCommonHierarchy(module, allModulesById);
                module.ModuleScope = isCommon
                    ? (short)3
                    : module.ModuleScope;
                module.ModuleScopeName = isCommon
                    ? "Common"
                    : module.ModuleScope == AppConstants.InstitutionModuleScope
                        ? "Institution"
                        : module.ModuleScope == AppConstants.HostModuleScope
                            ? "Host"
                            : "Unknown";
            }

            var modulesById = modules.ToDictionary(module => module.Id);
            var moduleIds = modulesById.Keys.ToHashSet();

            foreach (var leafModule in modules.Where(module => module.IsLeafNode))
            {
                leafModule.Operations = new List<FeaturePageOperationResponseDTO>();
            }

            var operations = await context.ModuleOperationMappings
                .AsNoTracking()
                .Where(mapping =>
                    mapping.IsActive == true &&
                    mapping.Module.InstitutionId == null &&
                    mapping.Module.IsActive &&
                    mapping.Module.IsLeafNode == true &&
                    mapping.Operation.IsActive)
                .OrderBy(mapping => mapping.ModuleId)
                .ThenBy(mapping => mapping.Priority ?? int.MaxValue)
                .ThenBy(mapping => mapping.Operation.OperationName)
                .ThenBy(mapping => mapping.OperationId)
                .ThenBy(mapping => mapping.Id)
                .Select(mapping => new
                {
                    mapping.ModuleId,
                    Operation = new FeaturePageOperationResponseDTO
                    {
                        Id = mapping.OperationId,
                        ModuleOperationMappingId = mapping.Id,
                        OperationName = mapping.Operation.OperationName,
                        OperationType = mapping.Operation.OperationType,
                        Remark = mapping.Operation.Remark,
                        IconKey = mapping.IconUrl ?? mapping.Operation.IconImage,
                        PageUrl = mapping.PageUrl,
                        DataViewStructureId = mapping.DataViewStructureId,
                        PageTypeId = mapping.PageTypeId,
                        IsCommonItem = mapping.IsCommonItem,
                        IsOperational = mapping.IsOperational,
                        Priority = mapping.Priority
                    }
                })
                .ToListAsync(cancellationToken);

            foreach (var operationGroup in operations
                         .Where(item =>
                             moduleIds.Contains(item.ModuleId) &&
                             modulesById.TryGetValue(item.ModuleId, out var module) &&
                             module.IsLeafNode)
                         .GroupBy(item => item.ModuleId))
            {
                var module = modulesById[operationGroup.Key];
                module.Operations = operationGroup.Select(item => item.Operation).ToList();
            }

            var rootModules = modules
                .Where(module =>
                    !module.ParentModuleId.HasValue ||
                    !modulesById.ContainsKey(module.ParentModuleId.Value))
                .ToList();

            foreach (var childHeader in modules.Where(module => !module.IsLeafNode))
            {
                var rootModule = GetRootModule(childHeader, modulesById);

                if (rootModule.Id == childHeader.Id || rootModule.IsLeafNode)
                {
                    continue;
                }

                rootModule.ChildHeaders ??= new List<FeaturePageResponseDTO>();
                rootModule.ChildHeaders.Add(childHeader);
            }

            foreach (var leafModule in modules.Where(module => module.IsLeafNode))
            {
                var nearestHeader = GetNearestHeader(leafModule, modulesById);

                if (nearestHeader is not null)
                {
                    nearestHeader.OperationalPages ??= new List<FeaturePageResponseDTO>();
                    nearestHeader.OperationalPages.Add(leafModule);
                }
            }

            return rootModules;
        }

        private static FeaturePageResponseDTO GetRootModule(
            FeaturePageResponseDTO module,
            IReadOnlyDictionary<int, FeaturePageResponseDTO> modulesById)
        {
            var visitedModuleIds = new HashSet<int>();
            var currentModule = module;

            while (visitedModuleIds.Add(currentModule.Id))
            {
                if (!currentModule.ParentModuleId.HasValue ||
                    !modulesById.TryGetValue(currentModule.ParentModuleId.Value, out var parentModule))
                {
                    return currentModule;
                }

                currentModule = parentModule;
            }

            return module;
        }

        private static FeaturePageResponseDTO? GetNearestHeader(
            FeaturePageResponseDTO leafModule,
            IReadOnlyDictionary<int, FeaturePageResponseDTO> modulesById)
        {
            var visitedModuleIds = new HashSet<int>();
            var currentModule = leafModule;

            while (currentModule.ParentModuleId.HasValue && visitedModuleIds.Add(currentModule.Id))
            {
                if (!modulesById.TryGetValue(currentModule.ParentModuleId.Value, out var parentModule))
                {
                    return null;
                }

                if (!parentModule.IsLeafNode)
                {
                    return parentModule;
                }

                currentModule = parentModule;
            }

            return null;
        }

        private static bool IsInCommonHierarchy(
            FeaturePageResponseDTO module,
            IReadOnlyDictionary<int, FeaturePageResponseDTO> modulesById)
        {
            var visitedModuleIds = new HashSet<int>();
            var currentModule = module;

            while (visitedModuleIds.Add(currentModule.Id))
            {
                if (currentModule.IsCommonMenu)
                {
                    return true;
                }

                if (!currentModule.ParentModuleId.HasValue ||
                    !modulesById.TryGetValue(currentModule.ParentModuleId.Value, out currentModule))
                {
                    return false;
                }
            }

            return false;
        }

        #endregion

        /// <summary>
        /// Retrieves visible top-level Parent Modules and their direct non-leaf child headers for the requested scope and optional active-state filter.
        /// </summary>
        /// <param name="moduleScope">The validated module scope.</param>
        /// <param name="isActive">When supplied, limits modules in the tree to the specified active state.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns>The existing module-header tree response model.</returns>
        public async Task<List<GetModuleChildInversResponseDTO>> GetAllOnlyModuleTreeAsync(
            short moduleScope,
            bool? isActive,
            CancellationToken cancellationToken)
        {
            var parentQuery = _context.Modules
                .Where(module =>
                    module.ParentModuleId == null &&
                    module.IsModuleDisplayInUI &&
                    module.ModuleScope == moduleScope);

            if (isActive.HasValue)
            {
                parentQuery = parentQuery.Where(module => module.IsActive == isActive.Value);
            }

            var parentModules = await parentQuery
                .OrderBy(module => module.ItemPriority)
                .ToListAsync(cancellationToken);

            if (parentModules.Count == 0)
            {
                return new List<GetModuleChildInversResponseDTO>();
            }

            var parentModuleIds = parentModules.Select(module => module.Id).ToList();
            var childQuery = _context.Modules
                .Where(module =>
                    module.ParentModuleId.HasValue &&
                    parentModuleIds.Contains(module.ParentModuleId.Value) &&
                    module.IsLeafNode == false &&
                    module.ModuleScope == moduleScope);

            if (isActive.HasValue)
            {
                childQuery = childQuery.Where(module => module.IsActive == isActive.Value);
            }

            var childModules = await childQuery
                .OrderBy(module => module.ItemPriority)
                .ToListAsync(cancellationToken);

            var childrenByParentId = childModules
                .GroupBy(module => module.ParentModuleId!.Value)
                .ToDictionary(group => group.Key, group => group.ToList());

            return parentModules
                .Select(parentModule => new GetModuleChildInversResponseDTO
                {
                    Id = parentModule.Id,
                    ModuleName = parentModule.ModuleName,
                    DisplayName = parentModule.DisplayName,
                    SubModuleUrl = parentModule.Urlpath,
                    URLPath = parentModule.Urlpath,
                    ImageIconWeb = parentModule.ImageIconWeb,
                    ImageIconMobile = parentModule.ImageIconMobile,
                    ItemPriority = parentModule.ItemPriority,
                    IsLeafNode = parentModule.IsLeafNode,
                    Children = childrenByParentId.TryGetValue(parentModule.Id, out var directChildModules)
                        ? directChildModules.Select(childModule => new GetModuleChildInversResponseDTO
                        {
                            Id = childModule.Id,
                            ModuleName = childModule.ModuleName,
                            DisplayName = childModule.DisplayName,
                            SubModuleUrl = childModule.Urlpath,
                            URLPath = childModule.Urlpath,
                            ImageIconWeb = childModule.ImageIconWeb,
                            ImageIconMobile = childModule.ImageIconMobile,
                            ItemPriority = childModule.ItemPriority,
                            IsLeafNode = childModule.IsLeafNode,
                            Children = new List<GetModuleChildInversResponseDTO>()
                        }).ToList()
                        : new List<GetModuleChildInversResponseDTO>()
                })
                .ToList();
        }

        #region ParentAdded

        /// <summary>
        /// Persists a validated Parent/Header Module and lets PostgreSQL generate its identity value.
        /// </summary>
        /// <param name="entity">The Header Module entity prepared by the application layer.</param>
        /// <param name="cancellationToken">A token to observe while saving changes.</param>
        /// <returns>The persisted entity, including its generated identifier.</returns>
        public async Task<Module> AddParentModuleAsync(Module entity, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(entity);
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");

            await context.Modules.AddAsync(entity, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return entity;
        }

        /// <summary>
        /// Retrieves one scope-filtered Parent/Header Module by identifier without tracking it.
        /// </summary>
        /// <param name="id">The requested module identifier.</param>
        /// <param name="moduleScope">The requested validated module scope.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns>The matching response model, or <see langword="null"/> when it is not accessible.</returns>
        public async Task<GetParentModuleResponseDTO?> GetParentModuleByIdAsync(
            int id,
            short moduleScope,
            CancellationToken cancellationToken)
        {
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");

            return await context.Modules
                .AsNoTracking()
                .Where(module =>
                    module.Id == id &&
                    module.ModuleScope == moduleScope &&
                    (module.ParentModuleId == null ||
                     (module.ParentModuleId != null &&
                      module.IsLeafNode == false)))
                .Select(ParentModuleProjection)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Checks whether another Parent/Header Module already uses the supplied code in the same institution and scope.
        /// </summary>
        /// <param name="moduleCode">The module code to check.</param>
        /// <param name="institutionId">The resolved institution identifier, or <see langword="null"/> for Host scope.</param>
        /// <param name="moduleScope">The supported institution module scope.</param>
        /// <param name="excludeModuleId">An existing module identifier to exclude during update.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns><see langword="true"/> when a conflicting Header Module exists.</returns>
        public async Task<bool> ExistsParentModuleCodeAsync(
            string moduleCode,
            long? institutionId,
            short moduleScope,
            int? excludeModuleId,
            CancellationToken cancellationToken)
        {
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");

            return await context.Modules
                .AsNoTracking()
                .AnyAsync(module =>
                    module.InstitutionId == institutionId &&
                    module.ModuleScope == moduleScope &&
                    module.ParentModuleId == null &&
                    module.IsLeafNode == false &&
                    module.ModuleCode == moduleCode &&
                    (!excludeModuleId.HasValue || module.Id != excludeModuleId.Value),
                    cancellationToken);
        }

        /// <summary>
        /// Retrieves a tracked scope-filtered Parent/Header Module so a validated update can preserve its hierarchy.
        /// </summary>
        /// <param name="id">The requested module identifier.</param>
        /// <param name="moduleScope">The requested validated module scope.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns>The matching entity, or <see langword="null"/> when it is not accessible.</returns>
        public async Task<Module?> GetParentModuleForUpdateAsync(
            int id,
            short moduleScope,
            CancellationToken cancellationToken)
        {
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");

            return await context.Modules
                .FirstOrDefaultAsync(module =>
                    module.Id == id &&
                    module.ModuleScope == moduleScope &&
                    module.ParentModuleId == null &&
                    module.IsLeafNode == false,
                    cancellationToken);
        }

        /// <summary>
        /// Saves a validated Parent/Header Module update.
        /// </summary>
        /// <param name="entity">The tracked Header Module entity to save.</param>
        /// <param name="cancellationToken">A token to observe while saving changes.</param>
        /// <returns>The saved Header Module entity.</returns>
        public async Task<Module> UpdateParentModuleAsync(Module entity, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(entity);
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");

            context.Modules.Update(entity);
            await context.SaveChangesAsync(cancellationToken);

            return entity;
        }

        /// <summary>
        /// Determines whether a Parent/Header Module has active direct children in the same scope that would be left orphaned by deactivation.
        /// </summary>
        /// <param name="parentModuleId">The Header Module identifier.</param>
        /// <param name="moduleScope">The requested validated module scope.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns><see langword="true"/> when an active direct child exists.</returns>
        public async Task<bool> HasChildrenAsync(
            int parentModuleId,
            short moduleScope,
            CancellationToken cancellationToken)
        {
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");

            return await context.Modules
                .AsNoTracking()
                .AnyAsync(module =>
                    module.ParentModuleId == parentModuleId &&
                    module.ModuleScope == moduleScope &&
                    module.IsLeafNode == true &&
                    module.IsActive,
                    cancellationToken);
        }

        #region Module Status Cascade

        /// <summary>
        /// Retrieves a tracked non-leaf Header Module for a status cascade.
        /// Root and nested Header Modules are valid targets; leaf modules are excluded.
        /// </summary>
        /// <param name="id">The requested Header Module identifier.</param>
        /// <param name="moduleScope">The requested validated module scope.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns>The matching tracked Header Module, or <see langword="null"/> when it does not exist.</returns>
        public async Task<Module?> GetHeaderModuleForStatusUpdateAsync(
            int id,
            short moduleScope,
            CancellationToken cancellationToken)
        {
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");

            return await context.Modules
                .FirstOrDefaultAsync(module =>
                    module.Id == id &&
                    module.ModuleScope == moduleScope &&
                    module.IsLeafNode == false,
                    cancellationToken);
        }

        /// <summary>
        /// Retrieves all tracked descendants for one Parent Module status cascade.
        /// The current Module schema has no soft-delete field, so every descendant in the requested scope is returned.
        /// </summary>
        /// <param name="parentModuleId">The validated Parent Module identifier.</param>
        /// <param name="moduleScope">The validated module scope.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns>The tracked descendant modules at every depth.</returns>
        public async Task<List<Module>> GetDescendantModulesForStatusUpdateAsync(
            int parentModuleId,
            short moduleScope,
            CancellationToken cancellationToken)
        {
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");

            var scopedModules = await context.Modules
                .Where(module =>
                    module.ModuleScope == moduleScope &&
                    module.ParentModuleId.HasValue)
                .ToListAsync(cancellationToken);

            var childrenByParentId = scopedModules
                .GroupBy(module => module.ParentModuleId!.Value)
                .ToDictionary(group => group.Key, group => group.ToList());

            var descendants = new List<Module>();
            var visitedModuleIds = new HashSet<int> { parentModuleId };
            var pendingParentIds = new Queue<int>();
            pendingParentIds.Enqueue(parentModuleId);

            while (pendingParentIds.Count > 0)
            {
                var currentParentId = pendingParentIds.Dequeue();
                if (!childrenByParentId.TryGetValue(currentParentId, out var children))
                {
                    continue;
                }

                foreach (var childModule in children)
                {
                    if (!visitedModuleIds.Add(childModule.Id))
                    {
                        continue;
                    }

                    descendants.Add(childModule);
                    pendingParentIds.Enqueue(childModule.Id);
                }
            }

            return descendants;
        }

        /// <summary>
        /// Retrieves tracked operation mappings for all affected modules in one query.
        /// The current mapping schema has no soft-delete field, so every mapping for the affected modules is returned.
        /// </summary>
        /// <param name="moduleIds">The affected Parent Module and descendant module identifiers.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns>The tracked operation mappings for the affected modules.</returns>
        public async Task<List<ModuleOperationMapping>> GetModuleOperationMappingsForStatusUpdateAsync(
            IReadOnlyCollection<int> moduleIds,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(moduleIds);

            if (moduleIds.Count == 0)
            {
                return new List<ModuleOperationMapping>();
            }

            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");

            return await context.ModuleOperationMappings
                .Where(mapping => moduleIds.Contains(mapping.ModuleId))
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Persists the complete tracked status cascade with one SaveChangesAsync call.
        /// EF Core executes the relational changes in its SaveChanges transaction.
        /// </summary>
        /// <param name="cancellationToken">A token to observe while saving the complete cascade.</param>
        /// <returns>A task that completes after the cascade is persisted.</returns>
        public async Task SaveModuleStatusCascadeAsync(CancellationToken cancellationToken)
        {
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");

            await context.SaveChangesAsync(cancellationToken);
        }

        #endregion

        #region Parent Module Projection

        /// <summary>
        /// Defines the server-translatable response projection used by Parent Module reads.
        /// </summary>
        private static readonly Expression<Func<Module, GetParentModuleResponseDTO>> ParentModuleProjection = module =>
            new GetParentModuleResponseDTO
            {
                Id = module.Id,
                ModuleCode = module.ModuleCode,
                ModuleName = module.ModuleName,
                DisplayName = module.DisplayName,
                URLPath = module.Urlpath,
                ParentModuleId = module.ParentModuleId,
                IsLeafNode = module.IsLeafNode,
                IsModuleDisplayInUI = module.IsModuleDisplayInUI,
                IsCommonMenu = module.IsCommonMenu,
                ModuleScope = module.ModuleScope,
                IsActive = module.IsActive,
                ImageIconWeb = module.ImageIconWeb,
                ImageIconMobile = module.ImageIconMobile,
                ItemPriority = module.ItemPriority,
                Remark = module.Remark,
                AddedById = module.AddedById,
                AddedDateTime = module.AddedDateTime,
                UpdatedById = module.UpdatedById,
                UpdatedDateTime = module.UpdatedDateTime
            };

        #endregion

        #region SubModule Projection

        /// <summary>
        /// Defines the server-translatable response projection used by direct SubModule reads.
        /// </summary>
        private static readonly Expression<Func<Module, GetSubModuleResponseDTO>> SubModuleProjection = module =>
            new GetSubModuleResponseDTO
            {
                Id = module.Id,
                ModuleCode = module.ModuleCode,
                ModuleName = module.ModuleName,
                DisplayName = module.DisplayName,
                URLPath = module.Urlpath,
                ParentModuleId = module.ParentModuleId,
                IsLeafNode = module.IsLeafNode,
                IsModuleDisplayInUI = module.IsModuleDisplayInUI,
                IsCommonMenu = module.IsCommonMenu,
                ModuleScope = module.ModuleScope,
                IsActive = module.IsActive,
                ImageIconWeb = module.ImageIconWeb,
                ImageIconMobile = module.ImageIconMobile,
                ItemPriority = module.ItemPriority,
                Remark = module.Remark,
                AddedById = module.AddedById,
                AddedDateTime = module.AddedDateTime,
                UpdatedById = module.UpdatedById,
                UpdatedDateTime = module.UpdatedDateTime,
                ParentModule = module.ParentModule == null
                    ? null
                    : new ParentModuleSummaryDTO
                    {
                        Id = module.ParentModule.Id,
                        ModuleCode = module.ParentModule.ModuleCode,
                        ModuleName = module.ParentModule.ModuleName,
                        DisplayName = module.ParentModule.DisplayName,
                        ModuleScope = module.ParentModule.ModuleScope,
                        IsActive = module.ParentModule.IsActive
                    }
            };

        #endregion

        #region SubModule CRUD

        /// <summary>
        /// Retrieves a Header Module that is valid to own a direct child in the requested scope.
        /// </summary>
        /// <param name="id">The Header Module identifier.</param>
        /// <param name="moduleScope">The requested validated module scope.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns>The matching tracked Header Module, or <see langword="null"/> when it is not a Header Module in the requested scope.</returns>
        public async Task<Module?> GetParentModuleForSubModuleAsync(
            int id,
            short moduleScope,
            CancellationToken cancellationToken)
        {
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");

            return await context.Modules
                .FirstOrDefaultAsync(module =>
                    module.Id == id &&
                    module.ModuleScope == moduleScope &&
                    module.ParentModuleId == null &&
                    module.IsLeafNode == false,
                    cancellationToken);
        }

        /// <summary>
        /// Retrieves one direct child SubModule with its Header Module summary.
        /// </summary>
        /// <param name="id">The SubModule identifier.</param>
        /// <param name="moduleScope">The requested validated module scope.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns>The matching direct-child response, or <see langword="null"/> when it does not exist.</returns>
        public async Task<GetSubModuleResponseDTO?> GetSubModuleByIdAsync(
            int id,
            short moduleScope,
            CancellationToken cancellationToken)
        {
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");

            return await context.Modules
                .AsNoTracking()
                .Where(module =>
                    module.Id == id &&
                    module.ModuleScope == moduleScope &&
                    module.ParentModuleId != null &&
                    module.IsLeafNode == true)
                .Select(SubModuleProjection)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Retrieves direct child SubModules for one scope, optionally narrowed by Header Module and active state.
        /// </summary>
        /// <param name="moduleScope">The requested validated module scope.</param>
        /// <param name="parentModuleId">When supplied, limits results to the Header Module identifier.</param>
        /// <param name="isActive">When supplied, limits results to the requested active state.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns>The ordered direct-child SubModule list.</returns>
        public async Task<List<GetSubModuleResponseDTO>> GetSubModulesAsync(
            short moduleScope,
            int? parentModuleId,
            bool? isActive,
            CancellationToken cancellationToken)
        {
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");

            var modules = context.Modules
                .AsNoTracking()
                .Where(module =>
                    module.ModuleScope == moduleScope &&
                    module.ParentModuleId != null &&
                    module.IsLeafNode == true);

            if (parentModuleId.HasValue)
            {
                modules = modules.Where(module => module.ParentModuleId == parentModuleId.Value);
            }

            if (isActive.HasValue)
            {
                modules = modules.Where(module => module.IsActive == isActive.Value);
            }

            return await modules
                .OrderBy(module => module.ItemPriority)
                .ThenBy(module => module.ModuleName)
                .Select(SubModuleProjection)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Retrieves a tracked direct child SubModule for an update or status change.
        /// </summary>
        /// <param name="id">The SubModule identifier.</param>
        /// <param name="moduleScope">The requested validated module scope.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns>The matching tracked direct child, or <see langword="null"/> when it does not exist.</returns>
        public async Task<Module?> GetSubModuleForUpdateAsync(
            int id,
            short moduleScope,
            CancellationToken cancellationToken)
        {
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");

            return await context.Modules
                .FirstOrDefaultAsync(module =>
                    module.Id == id &&
                    module.ModuleScope == moduleScope &&
                    module.ParentModuleId != null &&
                    module.IsLeafNode == true,
                    cancellationToken);
        }

        /// <summary>
        /// Checks whether another direct child SubModule already uses the supplied code in the same inherited institution and scope.
        /// </summary>
        /// <param name="moduleCode">The module code to check.</param>
        /// <param name="institutionId">The institution identifier inherited from the Header Module.</param>
        /// <param name="moduleScope">The requested validated module scope.</param>
        /// <param name="excludeModuleId">An existing SubModule identifier to exclude during update.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns><see langword="true"/> when a conflicting direct child exists.</returns>
        public async Task<bool> ExistsSubModuleCodeAsync(
            string moduleCode,
            long? institutionId,
            short moduleScope,
            int? excludeModuleId,
            CancellationToken cancellationToken)
        {
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");

            return await context.Modules
                .AsNoTracking()
                .AnyAsync(module =>
                    module.InstitutionId == institutionId &&
                    module.ModuleScope == moduleScope &&
                    module.ParentModuleId != null &&
                    module.IsLeafNode == true &&
                    module.ModuleCode == moduleCode &&
                    (!excludeModuleId.HasValue || module.Id != excludeModuleId.Value),
                    cancellationToken);
        }

        /// <summary>
        /// Saves a validated tracked direct child SubModule update.
        /// </summary>
        /// <param name="entity">The direct child entity to save.</param>
        /// <param name="cancellationToken">A token to observe while saving changes.</param>
        /// <returns>The saved direct child entity.</returns>
        public async Task<Module> UpdateSubModuleAsync(Module entity, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(entity);
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");

            context.Modules.Update(entity);
            await context.SaveChangesAsync(cancellationToken);

            return entity;
        }

        #endregion

        #endregion
        #region SubModuleAdded



        public async Task<List<GetSubModuleResponseDTO>> GetAllSubModuleAsync(Module module)
        {
            try
            {
                if (module == null)
                {
                    _logger.LogWarning("GetAllSubModuleAsync called with null module entity.");
                    return new List<GetSubModuleResponseDTO>();
                }

                

                if (_context.Modules == null)
                {
                    _logger.LogError("❌ DbSet<Module> is null in context.");
                    return new List<GetSubModuleResponseDTO>();
                }

                // ✅ Fetch SubModules under the given ParentModule
                var subModules = await _context.Modules
                    .Where(m => m.IsActive
                             && m.IsLeafNode == true
                             && m.IsModuleDisplayInUI == module.IsModuleDisplayInUI
                             && m.IsCommonMenu == module.IsCommonMenu
                             && m.ParentModuleId == module.ParentModuleId)
                    .OrderBy(m => m.ItemPriority)
                    .ThenBy(m => m.ModuleName)
                    .ToListAsync();

                // ✅ Return mapped list
                return _mapper.Map<List<GetSubModuleResponseDTO>>(subModules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in GetAllSubModuleAsync.");
                return new List<GetSubModuleResponseDTO>();
            }
        }

        #endregion

        public async Task<bool> UpdateModuleAsync(Module module)
        {
            try
            {
               

                var existing = await _context.Modules.FindAsync(module.Id);
                if (existing == null) return false;

                existing.ModuleName = module.ModuleName;
                existing.ParentModuleId = module.ParentModuleId;
                existing.ImageIconWeb = module.ImageIconWeb;
                existing.ImageIconMobile = module.ImageIconMobile;
                existing.IsActive = module.IsActive;
                existing.UpdatedById = module.UpdatedById;
                existing.UpdatedDateTime = DateTime.Now;
                existing.Remark = module.Remark;

				_context.Modules.Update(existing);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating module with ID {ModuleId}", module.Id);
                return false;
            }
        }

        public async Task<bool> DeleteModuleAsync(long moduleId)
        {
            try
            {
              

                var module = await _context.Modules.FindAsync(moduleId);
                if (module == null) return false;

                module.IsActive = false;
                module.UpdatedDateTime = DateTime.Now;

				_context.Modules.Update(module);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting module with ID {ModuleId}", moduleId);
                return false;
            }
        }
        public async Task<List<GetModuleChildInversResponseDTO>> AddSubModuleAsync(CreateSubModuleRequestDTO module)
        {
            try
            {
                // ✅ Step 1: Validation
                if (module == null)
                {
                    _logger.LogWarning("AddSubModuleAsync called with null module DTO.");
                    return new List<GetModuleChildInversResponseDTO>(); // empty list
                }

                if (string.IsNullOrWhiteSpace(module.ModuleName))
                {
                    _logger.LogWarning("Module name is missing in AddSubModuleAsync.");
                    return new List<GetModuleChildInversResponseDTO>();
                }

              

                // ✅ Step 2: Check for duplicate module under same parent
                bool isDuplicate = await _context.Modules
                    .AnyAsync(m => m.InstitutionId == module.InstitutionId &&
                        m.ModuleName == module.ModuleName &&
                        m.ParentModuleId == module.ParentModuleId &&
                        m.IsActive);

                if (isDuplicate)
                {
                    _logger.LogWarning("Duplicate SubModule name found under Parent ID: {ParentId}, Name: {ModuleName}",
                        module.ParentModuleId, module.ModuleName);
                    return new List<GetModuleChildInversResponseDTO>(); // return empty if duplicate
                }

                // ✅ Step 3: Mapping DTO → Entity
                var moduleEntity = _mapper.Map<Module>(module);

                // ✅ Step 4: Assign SubModule-specific values
                moduleEntity.IsLeafNode = module.IsLeafNode;
                moduleEntity.AddedById = module.StaffMemberId;
                moduleEntity.AddedDateTime = DateTime.UtcNow;
                moduleEntity.IsModuleDisplayInUI = module.IsModuleDisplayInUI;
                moduleEntity.IsCommonMenu = module.IsCommonMenu;
                moduleEntity.IsActive = module.IsActive;

                // ✅ Step 5: Save to DB
                var entity = await _context.Modules.AddAsync(moduleEntity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ New SubModule '{ModuleName}' added successfully under Parent ID {ParentId}",
                    moduleEntity.ModuleName, moduleEntity.ParentModuleId);
                int moduleId = entity.Entity.Id;

                // ✅ Call and wrap result into list
                var hierarchy = await GetModuleHierarchyByIdAsync(moduleId);

                return hierarchy != null
                    ? new List<GetModuleChildInversResponseDTO> { hierarchy }
                    : new List<GetModuleChildInversResponseDTO>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error while adding SubModule.");
                return new List<GetModuleChildInversResponseDTO>(); // return empty in case of exception
            }
        }
        public async Task<GetModuleChildInversResponseDTO?> GetModuleHierarchyByIdAsync(int moduleId)
        {
            try
            {
                // ✅ Step 1: Load all active modules
                var allModules = await _context.Modules
                    .Where(m => m.IsActive == true)
                    .ToListAsync();

                if (allModules == null || allModules.Count == 0)
                {
                    _logger?.LogWarning("⚠️ No active modules found.");
                    return null;
                }

                // ✅ Step 2: Build lookup dictionaries for traversal
                var childrenLookup = allModules
                    .GroupBy(m => m.ParentModuleId ?? 0)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var parentLookup = allModules.ToDictionary(m => m.Id, m => m.ParentModuleId);

                // ✅ Step 3: Recursive builder for children
                GetModuleChildInversResponseDTO BuildChildTree(Module module)
                {
                    var dto = new GetModuleChildInversResponseDTO
                    {
                        Id = module.Id,
                        ModuleName = module.ModuleName,
                        DisplayName = module.DisplayName,
                        SubModuleUrl = module.Urlpath,
                        URLPath = module.Urlpath,
                        ImageIconWeb = module.ImageIconWeb,
                        ImageIconMobile = module.ImageIconMobile,
                        ItemPriority = module.ItemPriority,
                        IsLeafNode = module.IsLeafNode,
                        Children = new List<GetModuleChildInversResponseDTO>()
                    };

                    if (childrenLookup.TryGetValue(module.Id, out var childModules))
                    {
                        foreach (var child in childModules.OrderBy(c => c.ItemPriority))
                        {
                            dto.Children.Add(BuildChildTree(child));
                        }
                    }

                    return dto;
                }

                // ✅ Step 4: Find the requested module
                var targetModule = allModules.FirstOrDefault(m => m.Id == moduleId);
                if (targetModule == null)
                {
                    _logger?.LogWarning("⚠️ Module with Id {Id} not found.", moduleId);
                    return null;
                }

                // ✅ Step 5: Build child tree for the target node
                var hierarchy = BuildChildTree(targetModule);

                // ✅ Step 6: Build upward chain (parents)
                var parentList = new List<GetModuleChildInversResponseDTO>();
                long? currentParentId = targetModule.ParentModuleId;

                while (currentParentId.HasValue && currentParentId != 0)
                {
                    var parentModule = allModules.FirstOrDefault(m => m.Id == currentParentId);
                    if (parentModule == null) break;

                    var parentDto = new GetModuleChildInversResponseDTO
                    {
                        Id = parentModule.Id,
                        ModuleName = parentModule.ModuleName,
                        DisplayName = parentModule.DisplayName,
                        URLPath = parentModule.Urlpath,
                        ItemPriority = parentModule.ItemPriority,
                        Children = new List<GetModuleChildInversResponseDTO> { hierarchy }
                    };

                    hierarchy = parentDto; // move up
                    currentParentId = parentModule.ParentModuleId;
                }

                _logger?.LogInformation("✅ Hierarchy for Module Id {Id} fetched successfully.", moduleId);
                return hierarchy;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "❌ Error occurred while fetching hierarchy for module Id {Id}.", moduleId);
                return null;
            }
        }

        public Task<List<ModuleDTO>> GetAllActiveModulesAsync(List<ModuleDTO> modules)
        {
            throw new NotImplementedException();
        }

        public Task<List<GetCommonModuleResponseDTO>> AddCommonModuleAsync(CreateCommonModuleRequestDTO Dto)
        {
            throw new NotImplementedException();
        }

      
        public Task<List<GetParentModuleResponseDTO>> GetSubParentModuleAsync(GetSubParentModulRequestDTO Dto)
        {
            throw new NotImplementedException();
        }

        public Task<List<GetCommonModuleResponseDTO>> GetCommonModuleAsync(GetCommonModuleRequestDTO Dto)
        {
            throw new NotImplementedException();
        }

        #region ModuleOperation Mapping CRUD

        /// <summary>
        /// Persists a validated module-operation mapping.
        /// </summary>
        /// <param name="entity">The mapping entity to persist.</param>
        /// <param name="cancellationToken">A token to observe while saving changes.</param>
        /// <returns>The persisted mapping, including its generated identifier.</returns>
        public async Task<ModuleOperationMapping> CreateModuleOperationMappingAsync(
            ModuleOperationMapping entity,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(entity);
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");

            await context.ModuleOperationMappings.AddAsync(entity, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return entity;
        }

        /// <summary>
        /// Retrieves one module-operation mapping with its module configuration lookups.
        /// </summary>
        /// <param name="id">The mapping identifier.</param>
        /// <param name="cancellationToken">A token to observe while querying.</param>
        /// <returns>The matching mapping, or <see langword="null"/> when it does not exist.</returns>
        public async Task<ModuleOperationMapping?> GetModuleOperationMappingByIdAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");

            return await context.ModuleOperationMappings
                .AsNoTracking()
                .Include(item => item.Module)
                .Include(item => item.Operation)
                .Include(item => item.DataViewStructure)
                .Include(item => item.PageType)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Module>?> GetModuleHierarchyForOperationActivationAsync(
            int moduleId,
            CancellationToken cancellationToken)
        {
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");
            var hierarchy = new List<Module>();
            var visitedModuleIds = new HashSet<int>();
            int? currentModuleId = moduleId;

            while (currentModuleId.HasValue)
            {
                if (!visitedModuleIds.Add(currentModuleId.Value))
                {
                    return null;
                }

                var module = await context.Modules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == currentModuleId.Value, cancellationToken);
                if (module is null)
                {
                    return null;
                }

                hierarchy.Add(module);
                currentModuleId = module.ParentModuleId;
            }

            return hierarchy;
        }

        /// <summary>
        /// Retrieves all module-operation mappings with their module configuration lookups.
        /// </summary>
        /// <param name="cancellationToken">A token to observe while querying.</param>
        /// <returns>The ordered module-operation mappings.</returns>
        public async Task<List<ModuleOperationMapping>> GetAllModuleOperationMappingsAsync(
            CancellationToken cancellationToken)
        {
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");

            return await context.ModuleOperationMappings
                .AsNoTracking()
                .Include(item => item.Module)
                .Include(item => item.Operation)
                .Include(item => item.DataViewStructure)
                .Include(item => item.PageType)
                .OrderBy(item => item.ModuleId)
                .ThenBy(item => item.OperationId)
                .ThenBy(item => item.Id)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Saves changes to a validated module-operation mapping.
        /// </summary>
        /// <param name="entity">The mapping entity to update.</param>
        /// <param name="cancellationToken">A token to observe while saving changes.</param>
        /// <returns>The updated mapping.</returns>
        public async Task<ModuleOperationMapping> UpdateModuleOperationMappingAsync(
            ModuleOperationMapping entity,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(entity);
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");

            context.ModuleOperationMappings.Update(entity);
            await context.SaveChangesAsync(cancellationToken);

            return entity;
        }

        /// <summary>
        /// Deactivates a module-operation mapping and records the acting Host user.
        /// </summary>
        /// <param name="id">The mapping identifier.</param>
        /// <param name="hostUserId">The authenticated Host user identifier.</param>
        /// <param name="utcNow">The UTC audit timestamp captured by the application layer.</param>
        /// <param name="cancellationToken">A token to observe while saving changes.</param>
        /// <returns><see langword="true"/> when a mapping was deactivated; otherwise <see langword="false"/>.</returns>
        public async Task<bool> DeactivateModuleOperationMappingAsync(
            int id,
            long hostUserId,
            DateTime utcNow,
            CancellationToken cancellationToken)
        {
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");
            var entity = await context.ModuleOperationMappings
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (entity == null)
            {
                return false;
            }

            entity.IsActive = false;
            entity.UpdatedById = hostUserId;
            entity.UpdatedDateTime = utcNow;

            await context.SaveChangesAsync(cancellationToken);

            return true;
        }

        #endregion

        #region Operation Dependency Queries

        /// <summary>
        /// Determines whether the supplied operation is still referenced by a module-operation mapping.
        /// The mapping schema does not expose a soft-delete column, so the database query evaluates every persisted relationship.
        /// </summary>
        /// <param name="operationId">The operation identifier to check.</param>
        /// <param name="cancellationToken">A token to observe while executing the database existence query.</param>
        /// <returns><see langword="true"/> when a persisted mapping references the operation; otherwise <see langword="false"/>.</returns>
        public async Task<bool> IsOperationLinkedToAnyModuleAsync(
            int operationId,
            CancellationToken cancellationToken)
        {
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");

            // Inactive mappings remain relationships and must not permit bypassing the operation dependency guard.
            return await context.ModuleOperationMappings
                .AnyAsync(item => item.OperationId == operationId, cancellationToken);
        }

        #endregion

        /// <summary>
        /// Persists a validated direct child SubModule and lets PostgreSQL generate its identity value.
        /// </summary>
        /// <param name="entity">The direct child entity prepared by the application layer.</param>
        /// <param name="cancellationToken">A token to observe while saving changes.</param>
        /// <returns>The persisted direct child entity, including its generated identifier.</returns>
        public async Task<Module> AddSubModuleAsync(Module entity, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(entity);
            var context = _context ?? throw new InvalidOperationException("Module context is unavailable.");

            await context.Modules.AddAsync(entity, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return entity;
        }

       
        
    }
}
