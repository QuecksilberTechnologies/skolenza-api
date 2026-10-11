// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Composes and validates eligible Subscription Plan Module hierarchies without mutating Module configuration.
// ================================================================

using skolenza.application.DTOs.PlanModule;
using skolenza.domain.Entity;

namespace skolenza.application.Features.PlanModuleMappingCmd;

/// <summary>
/// Represents the valid, selectable portion of the eligible Institution and Common Module hierarchy.
/// </summary>
internal sealed class PlanModuleHierarchy
{
    #region Fields

    private readonly IReadOnlyDictionary<int, Module> _modulesById;
    private readonly IReadOnlyDictionary<int, IReadOnlyCollection<Module>> _childrenByParentId;
    private readonly IReadOnlyCollection<Module> _rootModules;

    #endregion

    #region Constructor

    private PlanModuleHierarchy(
        IReadOnlyDictionary<int, Module> modulesById,
        IReadOnlyDictionary<int, IReadOnlyCollection<Module>> childrenByParentId,
        IReadOnlyCollection<Module> rootModules)
    {
        _modulesById = modulesById;
        _childrenByParentId = childrenByParentId;
        _rootModules = rootModules;
    }

    #endregion

    #region Factory

    /// <summary>
    /// Builds a hierarchy from Modules already filtered by the repository's canonical eligibility rule.
    /// </summary>
    /// <param name="eligibleModules">The visible, active Institution- and Common-scope Modules.</param>
    /// <returns>The selectable hierarchy rooted at eligible Module Headers.</returns>
    public static PlanModuleHierarchy Create(IReadOnlyCollection<Module> eligibleModules)
    {
        var modulesById = eligibleModules.ToDictionary(module => module.Id);
        var directChildrenByParentId = eligibleModules
            .Where(module => module.ParentModuleId.HasValue && module.ParentModuleId.Value > 0)
            .GroupBy(module => module.ParentModuleId!.Value)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<Module>)OrderModules(group));

        var roots = OrderModules(eligibleModules.Where(module =>
            !module.ParentModuleId.HasValue || module.ParentModuleId.Value <= 0));

        // Only descendants reachable through eligible roots can be selected.
        var reachableIds = new HashSet<int>();
        foreach (var root in roots)
        {
            AddReachableIds(root.Id, directChildrenByParentId, reachableIds, new HashSet<int>());
        }

        var reachableModulesById = modulesById
            .Where(pair => reachableIds.Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        var reachableChildrenByParentId = directChildrenByParentId
            .ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyCollection<Module>)pair.Value
                    .Where(module => reachableIds.Contains(module.Id))
                    .ToList());

        return new PlanModuleHierarchy(
            reachableModulesById,
            reachableChildrenByParentId,
            roots.Where(root => reachableIds.Contains(root.Id)).ToList());
    }

    #endregion

    #region Hierarchy Composition

    /// <summary>
    /// Gets all Module identifiers that are valid members of the selectable hierarchy.
    /// </summary>
    public IReadOnlyCollection<int> ModuleIds => _modulesById.Keys.ToArray();

    /// <summary>
    /// Builds popup response nodes with the supplied active mapping state.
    /// </summary>
    /// <param name="mappedModuleIds">The Modules currently mapped to the plan.</param>
    /// <returns>The eligible Module Header hierarchy with non-null child collections.</returns>
    public IReadOnlyCollection<PlanModuleOptionResponseDTO> BuildOptions(IReadOnlySet<int> mappedModuleIds)
    {
        return _rootModules
            .Select(module => BuildOption(module, mappedModuleIds, new HashSet<int>()))
            .ToList();
    }

    /// <summary>
    /// Validates explicit selections and expands each selected Header through its eligible descendants.
    /// </summary>
    /// <param name="selectedModuleIds">The Module identifiers submitted by the client.</param>
    /// <param name="expandedModuleIds">The complete valid selection when validation succeeds.</param>
    /// <returns><see langword="true"/> when every supplied Module is in the selectable hierarchy.</returns>
    public bool TryExpandSelection(
        IReadOnlyCollection<int> selectedModuleIds,
        out IReadOnlyCollection<int> expandedModuleIds)
    {
        var requestedModuleIds = selectedModuleIds.Distinct().ToArray();
        if (requestedModuleIds.Any(moduleId => !_modulesById.ContainsKey(moduleId)))
        {
            expandedModuleIds = Array.Empty<int>();
            return false;
        }

        var expandedIds = new HashSet<int>();
        foreach (var moduleId in requestedModuleIds)
        {
            AddReachableIds(moduleId, _childrenByParentId, expandedIds, new HashSet<int>());
        }

        expandedModuleIds = expandedIds.ToArray();
        return true;
    }

    private PlanModuleOptionResponseDTO BuildOption(
        Module module,
        IReadOnlySet<int> mappedModuleIds,
        ISet<int> ancestry)
    {
        if (!ancestry.Add(module.Id))
        {
            return new PlanModuleOptionResponseDTO
            {
                ModuleId = module.Id,
                ModuleName = module.ModuleName,
                DisplayName = module.DisplayName,
                IsMapped = mappedModuleIds.Contains(module.Id),
                IsActive = module.IsActive,
                Children = Array.Empty<PlanModuleOptionResponseDTO>()
            };
        }

        var children = _childrenByParentId.TryGetValue(module.Id, out var childModules)
            ? childModules.Select(child => BuildOption(child, mappedModuleIds, new HashSet<int>(ancestry))).ToList()
            : new List<PlanModuleOptionResponseDTO>();

        return new PlanModuleOptionResponseDTO
        {
            ModuleId = module.Id,
            ModuleName = module.ModuleName,
            DisplayName = module.DisplayName,
            IsMapped = mappedModuleIds.Contains(module.Id),
            IsActive = module.IsActive,
            Children = children
        };
    }

    private static void AddReachableIds(
        int moduleId,
        IReadOnlyDictionary<int, IReadOnlyCollection<Module>> childrenByParentId,
        ISet<int> reachableIds,
        ISet<int> ancestry)
    {
        if (!ancestry.Add(moduleId))
        {
            return;
        }

        reachableIds.Add(moduleId);

        if (!childrenByParentId.TryGetValue(moduleId, out var childModules))
        {
            return;
        }

        foreach (var child in childModules)
        {
            AddReachableIds(child.Id, childrenByParentId, reachableIds, new HashSet<int>(ancestry));
        }
    }

    private static List<Module> OrderModules(IEnumerable<Module> modules)
    {
        return modules
            .OrderBy(module => module.ItemPriority ?? int.MaxValue)
            .ThenBy(module => module.ModuleName)
            .ThenBy(module => module.Id)
            .ToList();
    }

    #endregion
}
