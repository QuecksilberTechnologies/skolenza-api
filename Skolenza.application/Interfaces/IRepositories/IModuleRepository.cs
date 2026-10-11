// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines module, module-operation, and shared Common-navigation persistence operations.
// ================================================================

using skolenza.application.DTOs;
using skolenza.application.DTOs.Module;
using skolenza.application.DTOs.RoleModulePermission;
using skolenza.application.DTOS.Module.CommonModule;
using skolenza.application.DTOS.Module.CommonMenu;
using skolenza.application.DTOS.Module.ManualModule;
using skolenza.application.DTOS.Module.ParentModule;
using skolenza.application.DTOS.Module.SubModule;
using skolenza.application.Features.ModuleCmd.Parent.Commands;
using skolenza.application.Features.ModuleCmd.SubModule.Commands;
using skolenza.application.Features.OperationCmd.Queries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks; 
using skolenza.domain.Entity; 
using MediatR;
using skolenza.application.DTOs.UserLogin;
using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Host;
using skolenza.application.DTOS.FeaturePages;
using skolenza.application.DTOS.Navigation;

namespace skolenza.application.Interfaces.IRepositories
{
    public interface IModuleRepository
    {
        #region Host Module Queries

        /// <summary>
        /// Retrieves a database-paged set of Host-scope modules, optionally filtered by their active state.
        /// </summary>
        /// <param name="isActive">When supplied, limits results to modules with the specified active state.</param>
        /// <param name="pageNumber">The normalized one-based page number.</param>
        /// <param name="pageSize">The normalized number of rows per page.</param>
        /// <param name="cancellationToken">A token to observe while executing the database query.</param>
        /// <returns>The requested Host-scope module page.</returns>
        Task<PagedResponseDTO<GetHostModuleResponseDTO>> GetHostModulesAsync(
            bool? isActive,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves one Host-scope module by identifier, optionally filtered by its active state.
        /// </summary>
        /// <param name="id">The module identifier.</param>
        /// <param name="isActive">When supplied, limits the result to the specified active state.</param>
        /// <param name="cancellationToken">A token to observe while executing the database query.</param>
        /// <returns>The matching Host-scope module, or <see langword="null"/> when none exists.</returns>
        Task<GetHostModuleResponseDTO?> GetHostModuleByIdAsync(
            int id,
            bool? isActive,
            CancellationToken cancellationToken);

        #endregion

        #region Common Navigation Queries

        /// <summary>
        /// Retrieves the unique active, UI-visible structural root of the shared Common navigation hierarchy.
        /// </summary>
        /// <param name="cancellationToken">A token used to cancel the database operation.</param>
        /// <returns>The unique Common root, or <see langword="null"/> when none is configured.</returns>
        Task<Module?> GetCommonMenuParentAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the visible active navigation children beneath the supplied Common root.
        /// </summary>
        /// <param name="parentModuleId">The validated Common root identifier.</param>
        /// <param name="cancellationToken">A token used to cancel the database operation.</param>
        /// <returns>The ordered child hierarchy below the supplied root.</returns>
        Task<List<ModuleDTO>> GetCommonMenuTreeAsync(
            int? parentModuleId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the deterministic active Common-menu hierarchy displayed across the authenticated application.
        /// </summary>
        /// <param name="cancellationToken">A token used to cancel the database operation.</param>
        /// <returns>The configured Common navigation items, or <see langword="null"/> when no Common root exists.</returns>
        Task<IReadOnlyCollection<CommonMenuItemResponseDTO>?> GetCommonMenuHierarchyAsync(
            CancellationToken cancellationToken = default);

        #endregion

        #region Authenticated Navigation Queries

        /// <summary>
        /// Retrieves the Institution Module hierarchy permitted to the current StaffMember through active entitlement and consolidated role-operation grants.
        /// </summary>
        Task<IReadOnlyCollection<NavigationMenuItemResponseDTO>> GetInstitutionNavigationMenuAsync(
            long institutionId,
            long staffMemberId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the Host Module hierarchy permitted to the current Host role.
        /// </summary>
        Task<IReadOnlyCollection<NavigationMenuItemResponseDTO>> GetHostNavigationMenuAsync(
            long hostRoleId,
            CancellationToken cancellationToken = default);

        #endregion

        #region Feature Pages Queries

        /// <summary>
        /// Retrieves active master feature headers with child headers, operational leaf pages, and active Operation configuration.
        /// </summary>
        /// <param name="scope">1 for Institution, 2 for Host, 3 for Common, or <see langword="null"/> for every scope.</param>
        /// <param name="cancellationToken">A token to observe while executing the database queries.</param>
        /// <returns>The ordered active master feature headers with their active operational pages.</returns>
        Task<IReadOnlyCollection<FeaturePageResponseDTO>> GetActiveFeaturePagesAsync(
            short? scope,
            CancellationToken cancellationToken);

        #endregion

        Task<Module?> GetModuleByIdAsync(long moduleId);

        /// <summary>Retrieves a Module by identifier without filtering its master active state.</summary>
        Task<Module?> GetModuleByIdIncludingInactiveAsync(long moduleId);

        #region ModuleOperation Mapping CRUD

        /// <summary>
        /// Persists a validated module-operation mapping.
        /// </summary>
        /// <param name="entity">The mapping entity to persist.</param>
        /// <param name="cancellationToken">A token to observe while saving changes.</param>
        /// <returns>The persisted mapping, including its generated identifier.</returns>
        Task<ModuleOperationMapping> CreateModuleOperationMappingAsync(
            ModuleOperationMapping entity,
            CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves one module-operation mapping with its module configuration lookups.
        /// </summary>
        /// <param name="id">The mapping identifier.</param>
        /// <param name="cancellationToken">A token to observe while querying.</param>
        /// <returns>The matching mapping, or <see langword="null"/> when it does not exist.</returns>
        Task<ModuleOperationMapping?> GetModuleOperationMappingByIdAsync(
            int id,
            CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves the owning Module and its complete ancestor hierarchy for module-operation validation.
        /// </summary>
        /// <param name="moduleId">The owning module identifier.</param>
        /// <param name="cancellationToken">A token to observe while querying.</param>
        /// <returns>The hierarchy ordered from owning Module to root, or <see langword="null"/> when a link is missing or cyclic.</returns>
        Task<IReadOnlyList<Module>?> GetModuleHierarchyForOperationActivationAsync(
            int moduleId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves all module-operation mappings with their module configuration lookups.
        /// </summary>
        /// <param name="cancellationToken">A token to observe while querying.</param>
        /// <returns>The ordered module-operation mappings.</returns>
        Task<List<ModuleOperationMapping>> GetAllModuleOperationMappingsAsync(
            CancellationToken cancellationToken);

        /// <summary>
        /// Saves changes to a validated module-operation mapping.
        /// </summary>
        /// <param name="entity">The mapping entity to update.</param>
        /// <param name="cancellationToken">A token to observe while saving changes.</param>
        /// <returns>The updated mapping.</returns>
        Task<ModuleOperationMapping> UpdateModuleOperationMappingAsync(
            ModuleOperationMapping entity,
            CancellationToken cancellationToken);

        /// <summary>
        /// Deactivates a module-operation mapping and records the acting Host user.
        /// </summary>
        /// <param name="id">The mapping identifier.</param>
        /// <param name="hostUserId">The authenticated Host user identifier.</param>
        /// <param name="utcNow">The UTC audit timestamp captured by the application layer.</param>
        /// <param name="cancellationToken">A token to observe while saving changes.</param>
        /// <returns><see langword="true"/> when a mapping was deactivated; otherwise <see langword="false"/>.</returns>
        Task<bool> DeactivateModuleOperationMappingAsync(
            int id,
            long hostUserId,
            DateTime utcNow,
            CancellationToken cancellationToken);

        #endregion

        #region Operation Dependency Queries

        /// <summary>
        /// Determines whether an operation is referenced by any persisted module-operation mapping.
        /// The current mapping schema has no soft-delete property; a mapping is ignored only after it is removed from the database.
        /// Mapping activity is intentionally not used because an inactive mapping still represents a relationship.
        /// </summary>
        /// <param name="operationId">The operation identifier to check.</param>
        /// <param name="cancellationToken">A token to observe while executing the database existence query.</param>
        /// <returns><see langword="true"/> when a module-operation mapping still references the operation; otherwise <see langword="false"/>.</returns>
        Task<bool> IsOperationLinkedToAnyModuleAsync(
            int operationId,
            CancellationToken cancellationToken);

        #endregion

        /// <summary>
        /// Sare modules laata hai (optionally filterable)
        /// </summary>
    

        Task<List<ModuleDTO>> GetAllActiveModulesAsync(List<ModuleDTO> modules);

        /// <summary>
        /// Creates a Parent/Header Module after the caller has established its institution, scope, hierarchy, and audit values.
        /// </summary>
        #region Create All module

        /// <summary>
        /// Persists a Parent/Header Module and returns the database-generated entity.
        /// </summary>
        /// <param name="entity">The Header Module entity to persist.</param>
        /// <param name="cancellationToken">A token to observe while saving changes.</param>
        /// <returns>The persisted entity, including its database-generated identifier.</returns>
        Task<Module> AddParentModuleAsync(Module entity, CancellationToken cancellationToken);

        Task<List<GetCommonModuleResponseDTO>> AddCommonModuleAsync(CreateCommonModuleRequestDTO Dto);
        Task<List<GetModuleChildInversResponseDTO>> AddSubModuleAsync(CreateSubModuleRequestDTO Dto);     

        #endregion
        #region Get All module

        /// <summary>
        /// Retrieves one scope-filtered Parent/Header Module by identifier.
        /// </summary>
        /// <param name="id">The requested module identifier.</param>
        /// <param name="moduleScope">The requested validated module scope.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns>The matching response model, or <see langword="null"/> when it does not exist.</returns>
        Task<GetParentModuleResponseDTO?> GetParentModuleByIdAsync(
            int id,
            short moduleScope,
            CancellationToken cancellationToken);

        /// <summary>
        /// Determines whether a Header Module code is already used in the institution and scope.
        /// </summary>
        /// <param name="moduleCode">The module code to check.</param>
        /// <param name="institutionId">The resolved institution identifier, or <see langword="null"/> for Host scope.</param>
        /// <param name="moduleScope">The supported institution module scope.</param>
        /// <param name="excludeModuleId">An existing module identifier to exclude during update.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns><see langword="true"/> when a conflicting Header Module exists.</returns>
        Task<bool> ExistsParentModuleCodeAsync(
            string moduleCode,
            long? institutionId,
            short moduleScope,
            int? excludeModuleId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves a tracked scope-filtered Parent/Header Module for a guarded update operation.
        /// </summary>
        /// <param name="id">The requested module identifier.</param>
        /// <param name="moduleScope">The requested validated module scope.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns>The matching entity, or <see langword="null"/> when it does not exist.</returns>
        Task<Module?> GetParentModuleForUpdateAsync(
            int id,
            short moduleScope,
            CancellationToken cancellationToken);

        /// <summary>
        /// Saves changes made to a tracked Parent/Header Module.
        /// </summary>
        /// <param name="entity">The validated Header Module entity to update.</param>
        /// <param name="cancellationToken">A token to observe while saving changes.</param>
        /// <returns>The saved entity.</returns>
        Task<Module> UpdateParentModuleAsync(Module entity, CancellationToken cancellationToken);

        /// <summary>
        /// Determines whether a Header Module has active direct child modules in the same scope.
        /// </summary>
        /// <param name="parentModuleId">The Header Module identifier.</param>
        /// <param name="moduleScope">The requested validated module scope.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns><see langword="true"/> when an active direct child exists.</returns>
        Task<bool> HasChildrenAsync(
            int parentModuleId,
            short moduleScope,
            CancellationToken cancellationToken);

        #region Module Status Cascade

        /// <summary>
        /// Retrieves a tracked non-leaf Header Module for a status cascade in the requested scope.
        /// Root and nested Header Modules are valid targets; leaf modules are excluded.
        /// </summary>
        /// <param name="id">The requested Header Module identifier.</param>
        /// <param name="moduleScope">The requested validated module scope.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns>The matching tracked Header Module, or <see langword="null"/> when it does not exist.</returns>
        Task<Module?> GetHeaderModuleForStatusUpdateAsync(
            int id,
            short moduleScope,
            CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves all tracked descendants for a Parent Module status cascade.
        /// The current Module schema has no soft-delete field, so every descendant in the requested scope is returned.
        /// </summary>
        /// <param name="parentModuleId">The validated Parent Module identifier.</param>
        /// <param name="moduleScope">The validated module scope.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns>The tracked descendant modules at every depth.</returns>
        Task<List<Module>> GetDescendantModulesForStatusUpdateAsync(
            int parentModuleId,
            short moduleScope,
            CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves tracked operation mappings for the supplied affected module identifiers.
        /// The current mapping schema has no soft-delete field, so every mapping for the affected modules is returned.
        /// </summary>
        /// <param name="moduleIds">The affected Parent Module and descendant module identifiers.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns>The tracked operation mappings for the affected modules.</returns>
        Task<List<ModuleOperationMapping>> GetModuleOperationMappingsForStatusUpdateAsync(
            IReadOnlyCollection<int> moduleIds,
            CancellationToken cancellationToken);

        /// <summary>
        /// Persists all tracked changes prepared for one Parent Module status cascade.
        /// </summary>
        /// <param name="cancellationToken">A token to observe while saving the complete cascade.</param>
        /// <returns>A task that completes after the cascade is persisted.</returns>
        Task SaveModuleStatusCascadeAsync(CancellationToken cancellationToken);

        #endregion

        #region SubModule CRUD

        /// <summary>
        /// Persists a validated direct child SubModule and returns the database-generated entity.
        /// </summary>
        /// <param name="entity">The direct child entity prepared by the application layer.</param>
        /// <param name="cancellationToken">A token to observe while saving changes.</param>
        /// <returns>The persisted direct child entity.</returns>
        Task<Module> AddSubModuleAsync(Module entity, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves one Header Module validated for direct child operations in the requested scope.
        /// </summary>
        /// <param name="id">The Header Module identifier.</param>
        /// <param name="moduleScope">The required module scope.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns>The matching tracked Header Module, or <see langword="null"/> when it does not exist.</returns>
        Task<Module?> GetParentModuleForSubModuleAsync(
            int id,
            short moduleScope,
            CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves one direct child SubModule with a Header Module summary in the requested scope.
        /// </summary>
        /// <param name="id">The SubModule identifier.</param>
        /// <param name="moduleScope">The required module scope.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns>The matching response, or <see langword="null"/> when it does not exist.</returns>
        Task<GetSubModuleResponseDTO?> GetSubModuleByIdAsync(
            int id,
            short moduleScope,
            CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves direct child SubModules in a requested scope with parent summaries.
        /// </summary>
        /// <param name="moduleScope">The required module scope.</param>
        /// <param name="parentModuleId">When supplied, limits results to this Header Module.</param>
        /// <param name="isActive">When supplied, limits results to the specified active state.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns>The ordered direct-child SubModule list.</returns>
        Task<List<GetSubModuleResponseDTO>> GetSubModulesAsync(
            short moduleScope,
            int? parentModuleId,
            bool? isActive,
            CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves a tracked direct child SubModule for a guarded update operation.
        /// </summary>
        /// <param name="id">The SubModule identifier.</param>
        /// <param name="moduleScope">The required module scope.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns>The matching tracked direct child, or <see langword="null"/> when it does not exist.</returns>
        Task<Module?> GetSubModuleForUpdateAsync(
            int id,
            short moduleScope,
            CancellationToken cancellationToken);

        /// <summary>
        /// Determines whether a direct child module code already exists in the same inherited institution and scope.
        /// </summary>
        /// <param name="moduleCode">The module code to check.</param>
        /// <param name="institutionId">The institution identifier inherited from the Header Module.</param>
        /// <param name="moduleScope">The required module scope.</param>
        /// <param name="excludeModuleId">An existing SubModule identifier to exclude during update.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns><see langword="true"/> when a conflicting direct child exists.</returns>
        Task<bool> ExistsSubModuleCodeAsync(
            string moduleCode,
            long? institutionId,
            short moduleScope,
            int? excludeModuleId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Saves changes made to a validated tracked direct child SubModule.
        /// </summary>
        /// <param name="entity">The validated direct child entity to update.</param>
        /// <param name="cancellationToken">A token to observe while saving changes.</param>
        /// <returns>The saved direct child entity.</returns>
        Task<Module> UpdateSubModuleAsync(Module entity, CancellationToken cancellationToken);

        #endregion

        Task<List<GetParentModuleResponseDTO>> GetSubParentModuleAsync(GetSubParentModulRequestDTO Dto);
        Task<List<GetCommonModuleResponseDTO>> GetCommonModuleAsync(GetCommonModuleRequestDTO Dto);

        /// <summary>
        /// Retrieves visible top-level Parent Modules and their direct non-leaf child headers for a validated scope and optional active-state filter.
        /// </summary>
        /// <param name="moduleScope">The validated module scope.</param>
        /// <param name="isActive">When supplied, limits modules in the tree to the specified active state.</param>
        /// <param name="cancellationToken">A token to observe while executing the query.</param>
        /// <returns>The existing module-header tree response model.</returns>
        Task<List<GetModuleChildInversResponseDTO>> GetAllOnlyModuleTreeAsync(
            short moduleScope,
            bool? isActive,
            CancellationToken cancellationToken);
        #endregion
        /// <summary>
        /// Module ko update karta hai
        /// </summary>
        Task<bool> UpdateModuleAsync(Module module);

        /// <summary>
        /// Module ko soft/hard delete karta hai
        /// </summary>
        Task<bool> DeleteModuleAsync(long moduleId);


    }
}
