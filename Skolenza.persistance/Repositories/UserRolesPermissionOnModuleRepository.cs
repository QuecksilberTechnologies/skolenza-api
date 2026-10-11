// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Provides persistence operations for User Roles Permission On Module records.
// ================================================================

using skolenza.application.DTOs.BasicAndRoleBaseMenu;
using skolenza.application.DTOs.Module;
using skolenza.application.DTOs.ModuleOperation;
using skolenza.application.DTOs.Role;
using skolenza.application.DTOs.RoleModulePermission;
using skolenza.application.DTOs.Institution;
using skolenza.application.DTOS.RoleModulePermission;
using skolenza.application.Constants;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace skolenza.persistance.Repositories
{

    #region Persistence Operations


    /// <summary>
    /// Provides persistence operations for UserRolesPermissionOnModule records.
    /// </summary>
    public class UserRolesPermissionOnModuleRepository : IUserRolesPermissionOnModuleRepository
    {
        private readonly SkolenzaDbContext? _context;
        private readonly ILogger<UserRolesPermissionOnModuleRepository>? _logger;

        public UserRolesPermissionOnModuleRepository(SkolenzaDbContext? context, ILogger<UserRolesPermissionOnModuleRepository>? logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> AdminAssignModulePermissionAsync(List<RoleModuleAndPermission> insertRoleModulePermissionsRequest)
        {
            try
            {
                if (insertRoleModulePermissionsRequest == null || !insertRoleModulePermissionsRequest.Any())
                {
                    _logger.LogWarning("No role module permissions to assign.");
                    return 0;
                }

                await _context.RoleModuleAndPermissions.AddRangeAsync(insertRoleModulePermissionsRequest);

                // Final save to the database
                return await _context.SaveChangesAsync(); //  returns number of inserted rows
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AdminAssignModulePermissionAsync while assigning permissions.");
                throw;
            }
        }
        public async Task<InstitutionEnabledOperationsResponseDTO> GetAllInstitutionModuleWithOperation(InstitutionEnabledOperation dto)
        {
            try
            {
                _logger.LogInformation("🔹 GetAllInstitutionModuleWithOperation started for InstitutionId: {InstitutionId}", dto.InstitutionId);

                var data = await _context.InstitutionEnabledOperations
                    .AsNoTracking()
                    .Where(x => x.InstitutionId == dto.InstitutionId && x.IsEnabled)
                    .Select(x => new
                    {
                        ModuleId = x.ModuleId,
                        ModuleName = x.Module.ModuleName,
                        ParentModuleId = x.Module.ParentModuleId,
                        //  NEW FIELD
                        ParentModuleName = x.Module.ParentModule != null ? x.Module.ParentModule.ModuleName : null,
                        OperationId = x.OperationId,
                        OperationName = x.Operation.OperationName,
                        OperationType = x.Operation.OperationType,

                        IsOperationEnabled = x.IsEnabled
                    })
                    .ToListAsync();

                // ===============================
                // GROUPING
                // ===============================
                var modules = data
                    .GroupBy(x => x.ModuleId)
                    .Select(g => new EnabledModuleActiveDTO
                    {
                        Id = g.Key,
                        ModuleName = g.First().ModuleName,
                        ParentModuleId = g.First().ParentModuleId,
                        IsEnabled = true,

                        Operations = g.Select(op => new EnabledOperationActiveDTO
                        {
                            Id = op.OperationId,
                            OperationName = op.OperationName,
                            OperationType = op.OperationType,
                            IsEnabled = op.IsOperationEnabled
                        }).ToList()
                    })
                    .ToList();

                return new InstitutionEnabledOperationsResponseDTO
                {
                    InstitutionId = dto.InstitutionId,
                    Modules = modules
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in GetAllInstitutionModuleWithOperation");
                throw;
            }
        }

        public async Task<int> BulkInsertAsync(List<RoleModuleAndPermission> rolePermissions)
        {
            try
            {
                _logger?.LogInformation("🔹 BulkInsertAsync started for RoleModuleAndPermission");

                // ===============================
                // 2️⃣ REMOVE DUPLICATES (SAFETY)
                // ===============================
                rolePermissions = rolePermissions
                    .GroupBy(x => new { x.RoleId, x.ModuleId, x.OperationId })
                    .Select(g => g.First())
                    .ToList();

                // ===============================
                // 3️⃣ BULK INSERT (EF CORE)
                // ===============================
                await _context.RoleModuleAndPermissions.AddRangeAsync(rolePermissions);

                // ️ SaveChanges yahan nahi (UnitOfWork handle karega)
                // await _context.SaveChangesAsync();

                _logger?.LogInformation("✅ Bulk insert prepared. Count: {Count}", rolePermissions.Count);

                _logger?.LogInformation("✅ Bulk insert prepared. Count: {Count}", rolePermissions.Count);

                return rolePermissions.Count; //  CORRECT
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "❌ Error in BulkInsertAsync");
                return 0;
            }
        }

        /// <inheritdoc />
        public async Task<int> AddMissingInstitutionAdminPermissionsAsync(
            long institutionId,
            long addedById,
            CancellationToken cancellationToken = default)
        {
            var adminRole = await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(role =>
                    role.InstitutionId == institutionId &&
                    role.RoleType == ConstantValues.RoleTypeAdmin &&
                    role.IsActive &&
                    role.IsSoftDeleted == false &&
                    role.IsSystemDefault == false,
                    cancellationToken);

            if (adminRole is null)
            {
                _logger.LogWarning(
                    "Institution Admin role was not found while synchronizing missing permissions. InstitutionId: {InstitutionId}",
                    institutionId);
                return 0;
            }

            var enabledOperationKeys = await _context.InstitutionEnabledOperations
                .AsNoTracking()
                .Where(operation => operation.InstitutionId == institutionId && operation.IsEnabled)
                .Select(operation => new
                {
                    operation.ModuleId,
                    operation.OperationId
                })
                .Distinct()
                .ToListAsync(cancellationToken);

            if (enabledOperationKeys.Count == 0)
            {
                return 0;
            }

            var enabledModuleIds = enabledOperationKeys
                .Select(operation => operation.ModuleId)
                .Distinct()
                .ToList();

            var existingPermissionKeys = (await _context.RoleModuleAndPermissions
                    .AsNoTracking()
                    .Where(permission =>
                        permission.RoleId == adminRole.Id &&
                        permission.ModuleId.HasValue &&
                        permission.OperationId.HasValue &&
                        enabledModuleIds.Contains(permission.ModuleId.Value))
                    .Select(permission => new
                    {
                        permission.ModuleId,
                        permission.OperationId
                    })
                    .ToListAsync(cancellationToken))
                .Select(permission => (permission.ModuleId!.Value, permission.OperationId!.Value))
                .ToHashSet();

            var utcNow = DateTime.UtcNow;
            var permissionsToAdd = enabledOperationKeys
                .Where(operation => !existingPermissionKeys.Contains(
                    (operation.ModuleId, operation.OperationId)))
                .Select(operation => new RoleModuleAndPermission
                {
                    RoleId = adminRole.Id,
                    ModuleId = operation.ModuleId,
                    OperationId = operation.OperationId,
                    HasAccess = true,
                    IsActive = true,
                    IsSoftDeleted = false,
                    AddedById = addedById,
                    AddedDateTime = utcNow,
                    Remark = "Auto-assigned during Institution plan entitlement synchronization"
                })
                .ToList();

            if (permissionsToAdd.Count > 0)
            {
                await _context.RoleModuleAndPermissions.AddRangeAsync(
                    permissionsToAdd,
                    cancellationToken);
            }

            _logger.LogInformation(
                "Prepared missing Institution Admin permissions. InstitutionId: {InstitutionId}, RoleId: {RoleId}, AddedPermissions: {AddedPermissions}",
                institutionId,
                adminRole.Id,
                permissionsToAdd.Count);

            return permissionsToAdd.Count;
        }

        public async Task<IEnumerable<UserRolesPermissionOnModuleDTO>> GetModuleListAndOperationByRollIdAsync(List<RoleInfoDTO> roleList, int? forPlatform)
        {
            try
            {
                if (roleList == null || !roleList.Any())
                {
                    _logger?.LogWarning("Role list is empty or null.");
                    return Enumerable.Empty<UserRolesPermissionOnModuleDTO>();
                }
                // Extract Role IDs from the role list
                List<int>? roleIds = roleList.Select(r => r.Id).ToList();
                //var result = await (from rmp in _context.RoleModuleAndPermissions
                //                    join submd in _context.ProjectSubModuleDetails on rmp.SubModuleId equals submd.Id
                //                    join pmd in _context.ProjectModuleDetails on submd.ModuleId equals pmd.Id
                //                    join op in _context.Operations on rmp.OperationId equals op.Id
                //                    select new UserRolesPermissionOnModuleDTO
                //                    {
                //                        Id = rmp.Id, // RoleModuleAndPermission Id
                //                        SubModuleName = submd.SubModuleName, // SubModuleName from SubModule table
                //                        ModuleName = pmd.ModuleName, // ModuleName from Module table
                //                        ModuleDescription = pmd.Remark, // Description from Module table
                //                        ModuleURL = pmd.ModuleUrl, // URL from Module table
                //                       // ImageIcon = rmp.ImageIcon, // Icon from RoleModuleAndPermission
                //                        ActionType = op.OperationName, // OperationName from Operation table
                //                        ActionDescription = op.Remark, // Description from RoleModuleAndPermission
                //                      //  HasAccess = rmp.HasAccess, // Access permission from RoleModuleAndPermission
                //                        IsActive = rmp.IsActive, // Assign directly as it’s non-nullable
                //                        SubModuleDescription = submd.Remark // SubModule description
                //                    }).ToListAsync();




                //    _logger?.LogInformation($"Successfully fetched {result.Count} active records for Role IDs: {string.Join(",", roleIds)}.");

                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while fetching module list and operations by Role ID.");
                throw; // Re-throw the exception after logging
            }
        }


     //yeh function sirf enabled module or operation laata hai , login mei bhi used
        public async Task<List<InstitutionEnabledModule>> GetAllInstitutionEnabledModulesWithOperationsAsync(long? institutionId)
        {
            try
            {
                var modules = await _context.InstitutionEnabledModules
                    .Where(m => m.InstitutionId == institutionId && m.IsEnabled)
                    .Include(m => m.Module)
                        .ThenInclude(mod => mod.ModuleOperationMapping
                            .Where(mop => mop.IsActive == true)) //  filtered include EF Core 5+
                    .ToListAsync();

                return modules;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching enabled modules and operations for InstitutionId: {InstitutionId}", institutionId);
                return new List<InstitutionEnabledModule>();
            }
        }



        public async Task<InstitutionEnabledOperationsResponseDTO> Get(long institutionId)
        {
            try
            {
                //  Step 1: Get all enabled modules
                var institutionModules = await _context.InstitutionEnabledModules
                    .Where(t => t.InstitutionId == institutionId)
                    .Include(t => t.Module)
                    .ThenInclude(m => m.ParentModule)
                    .ToListAsync();

                //  Step 2: Get all enabled operations
                var institutionOperations = await _context.InstitutionEnabledOperations
                    .Where(op => op.InstitutionId == institutionId && op.IsEnabled)
                    .Include(op => op.Operation)
                    .ToListAsync();

                //  Step 3: Map manually in memory
                var modules = institutionModules.Select(t => new EnabledModuleActiveDTO
                {
                    Id = t.Module.Id,
                    ModuleName = t.Module.ModuleName,
                    ParentModuleId = t.Module.ParentModuleId,
                    ParentModuleName = t.Module.ParentModule != null ? t.Module.ParentModule.ModuleName : "",
                    IsEnabled = t.IsEnabled,
                    Operations = institutionOperations
                        .Where(op => op.ModuleId == t.ModuleId)
                        .Select(op => new EnabledOperationActiveDTO
                        {
                            Id = op.OperationId,
                            OperationName = op.Operation?.OperationName ?? "",
                            IsEnabled = op.IsEnabled
                        }).ToList()
                }).ToList();

                return new InstitutionEnabledOperationsResponseDTO
                {
                    InstitutionId = institutionId,
                    Modules = modules
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching enabled modules and operations for institution.");
                return new InstitutionEnabledOperationsResponseDTO
                {
                    InstitutionId = institutionId,
                    Modules = null
                };
            }
        }
        public async Task<bool> UpdateInstitutionModuleAndItsOperationsAsync(InstitutionModuleOperationsUpdateRequestDTO request)
        {
            try
            {
                foreach (var module in request.Modules)
                {
                    //  Step 1: Update InstitutionEnabledModule table
                    await _context.Database.ExecuteSqlRawAsync(
                        @"UPDATE [Skolenza].[InstitutionEnabledModule]
                        SET IsEnabled = {0}, UpdatedDateTime = GETUTCDATE()
                        WHERE InstitutionId = {1} AND ModuleId = {2}",
                        module.IsEnabled, request.InstitutionId, module.ModuleId
                    );

                    if (module.IsEnabled == false)
                    {
                        //  Step 2: If module is disabled, disable all its operations
                        await _context.Database.ExecuteSqlRawAsync(
                            @"UPDATE [Skolenza].[InstitutionEnabledOperation]
                              SET IsEnabled = 0, IsOperationUsed = 0, UpdatedDateTime = GETUTCDATE()
                              WHERE InstitutionId = {0} AND ModuleId = {1}",
                            request.InstitutionId, module.ModuleId
                        );
                    }
                    else
                    {
                        //  Step 3: If module is enabled, update only selected operations
                        foreach (var operation in module.Operations)
                        {
                            await _context.Database.ExecuteSqlRawAsync(
                                @"UPDATE [Skolenza].[InstitutionEnabledOperation]
                                   SET IsEnabled = {0}, IsOperationUsed = {1}, UpdatedDateTime = GETUTCDATE()
                                   WHERE InstitutionId = {2} AND ModuleId = {3} AND OperationId = {4}",
                                   operation.IsEnabled, operation.IsOperationUsed, request.InstitutionId, module.ModuleId, operation.OperationId
                            );
                        }
                    }
                }



                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk update failed while updating module & operations");
                return false;
            }
        }

        public async Task<List<ModuleOperationMapping>> GetModuleOperationMappings(List<Module> modules)
        {
            try
            {
                // await using var _context = await _contextFactory.CreateDbContextAsync();

                if (modules == null || !modules.Any())
                {
                    return new List<ModuleOperationMapping>();
                }

                var moduleIds = modules.Select(m => m.Id).ToList();

                var mappings = await _context.ModuleOperationMappings.Where(m => moduleIds.Contains(m.ModuleId) && m.IsActive == true).Include(m => m.Operation).ToListAsync();


                return mappings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetModuleOperationMappings");
                return new List<ModuleOperationMapping>();
            }
        }

        public async Task<List<GetModuleOperationRolePermissionsResponseDTO>> GetInstitutionModulesOperationRolePermissionResponses(GetInstitutionModuleOperationRolePermissionsRequestDTO dto)
        {
            try
            {
                //  Step 0: Create DbContext
                //   await using var context = await _contextFactory.CreateDbContextAsync();

                // InstitutionEnabledModule is the persisted entitlement snapshot.  Do not
                // revalidate it through the current plan mapping: a Host may later
                // retire/delete that plan without invalidating an existing institution.
                var institutionEnabledModuleIds = await _context.InstitutionEnabledModules
                    .Where(t => t.InstitutionId == dto.InstitutionId && t.IsEnabled == true)
                    .Select(t => t.ModuleId)
                    .Distinct()
                    .ToListAsync();

                if (institutionEnabledModuleIds == null || !institutionEnabledModuleIds.Any())
                {
                    _logger?.LogWarning("No enabled modules found for InstitutionId: {InstitutionId}", dto.InstitutionId);
                    return null;
                }

                // -------------------------------------------------------
                // Step 4: Special filtering (ModuleId & ParentModuleId both provided)
                // -------------------------------------------------------
                if (dto.ModuleId > 0 && dto.ParentModuleId > 0)
                {
                    //  Step 4.1: dono me se badi value nikaal lo
                    int selectedModuleId = Math.Max(dto.ModuleId ?? 0, dto.ParentModuleId ?? 0);

                    //  Step 4.2: check karo kya ye module institution ke enabled list me hai
                    bool isModuleEnabled = institutionEnabledModuleIds.Contains(selectedModuleId);
                    if (!isModuleEnabled)
                    {
                        _logger?.LogWarning("Selected module {ModuleId} is not enabled for InstitutionId {InstitutionId}", selectedModuleId, dto.InstitutionId);
                        return null;
                    }

                    //  Step 4.3: ab Module table se uske saare child modules (Nth level tak) nikaalo
                    // Recursive fetching karenge — manually loop se karenge
                    var allModules = await _context.Modules
                        .Select(m => new { m.Id, m.ModuleName, m.ParentModuleId, m.IsLeafNode })
                        .ToListAsync();

                    // recursion ke bina iterative approach se Nth level children nikaal rahe hain
                    List<int> childModuleIds = new() { selectedModuleId };
                    bool newAdded;

                    do
                    {
                        var currentCount = childModuleIds.Count;
                        var nextLevel = allModules
                            .Where(m => m.ParentModuleId.HasValue && childModuleIds.Contains(m.ParentModuleId.Value))
                            .Select(m => m.Id)
                            .ToList();

                        childModuleIds.AddRange(nextLevel.Except(childModuleIds));
                        newAdded = childModuleIds.Count > currentCount;
                    }
                    while (newAdded);

                    //  Step 4.4: sirf leaf node wale modules filter kar lo
                    var leafModules = allModules
                        .Where(m => childModuleIds.Contains(m.Id) && m.IsLeafNode == true)
                        .ToList();

                    if (!leafModules.Any())
                    {
                        _logger?.LogInformation("No leaf modules found under ModuleId {ModuleId}", selectedModuleId);
                        return new List<GetModuleOperationRolePermissionsResponseDTO>();
                    }

                    //  Step 4.5: ab un leaf modules ke liye operations nikaalo
                    var operationsData = await (
                        from teo in _context.InstitutionEnabledOperations
                        join op in _context.Operations on teo.OperationId equals op.Id
                        where teo.InstitutionId == dto.InstitutionId
                              && leafModules.Select(x => x.Id).Contains(teo.ModuleId)
                              && teo.IsEnabled == true
                        select new
                        {
                            ModuleId = teo.ModuleId,
                            OperationId = op.Id,
                            op.OperationName,
                            teo.IsOperationUsed
                        }
                    ).ToListAsync();

                    if (!operationsData.Any())
                    {
                        _logger?.LogInformation("No operations found for leaf modules for InstitutionId: {InstitutionId}", dto.InstitutionId);
                        return new List<GetModuleOperationRolePermissionsResponseDTO>();
                    }

                    //  Step 4.6: DTO me map kar lo
                    //  Step 4.6: Ab RoleModuleAndPermission table ke saath left join karenge
                    var rolePermissions = await (
                        from rp in _context.RoleModuleAndPermissions
                        where rp.RoleId == dto.RoleId
                              && rp.IsActive == true
                        select new
                        {
                            rp.RoleId,
                            rp.ModuleId,
                            rp.OperationId,
                            rp.HasAccess
                        }
                    ).ToListAsync();

                    //  Step 4.7: Ab operationsData ko left join karte hain RolePermission ke saath
                    var response = leafModules
                        .GroupJoin(operationsData,
                            m => m.Id,
                            o => o.ModuleId,
                            (m, ops) => new GetModuleOperationRolePermissionsResponseDTO
                            {
                                ModuleId = m.Id,
                                ModuleName = m.ModuleName,
                                IsLeafNode = true,
                                IsEnabled = true,
                                Level = 0,
                                BreadCrum = "",
                                Operations = ops.Select(op =>
                                {
                                    // matching permission search
                                    var permission = rolePermissions.FirstOrDefault(p =>
                                        p.ModuleId == m.Id &&
                                        p.OperationId == op.OperationId);

                                    return new OperationDTO_Config
                                    {
                                        OperationId = op.OperationId,
                                        OperationName = op.OperationName,
                                        IsOperationUsed = op.IsOperationUsed ?? false,
                                        HasAccess = permission?.HasAccess ?? false  // ← left join ka core point
                                    };
                                }).ToList()
                            })
                        .ToList();


                    _logger?.LogInformation("Fetched {Count} leaf modules and operations for InstitutionId: {InstitutionId}", response.Count, dto.InstitutionId);
                    return response;
                }

                // -------------------------------------------------------
                // Step 5: Default path (if special condition not met)
                // -------------------------------------------------------
                // Yahan normal institution-operation-module data la rahe hain
                var operationDataDefault = await (
                    from teo in _context.InstitutionEnabledOperations
                    join op in _context.Operations on teo.OperationId equals op.Id
                    join m in _context.Modules on teo.ModuleId equals m.Id
                    where teo.InstitutionId == dto.InstitutionId
                          && teo.IsEnabled == true
                    select new
                    {
                        m.Id,
                        m.ModuleName,
                        m.ParentModuleId,
                        m.IsLeafNode,
                        OperationId = op.Id,
                        op.OperationName,
                        teo.IsOperationUsed
                    }
                ).ToListAsync();

                if (!operationDataDefault.Any())
                {
                    _logger?.LogInformation("No operations found for InstitutionId: {InstitutionId}", dto.InstitutionId);
                    return new List<GetModuleOperationRolePermissionsResponseDTO>();
                }

                //  Step 6: Group & map to DTO
                var modulesDefault = operationDataDefault
                    .GroupBy(x => new { x.Id, x.ModuleName, x.ParentModuleId, x.IsLeafNode })
                    .Select(g => new GetModuleOperationRolePermissionsResponseDTO
                    {
                        ModuleId = g.Key.Id,
                        ModuleName = g.Key.ModuleName,
                        IsLeafNode = g.Key.IsLeafNode,
                        IsEnabled = true,
                        Level = 0,
                        BreadCrum = "",
                        Operations = g.Select(op => new OperationDTO_Config
                        {
                            OperationId = op.OperationId,
                            OperationName = op.OperationName,
                            IsOperationUsed = op.IsOperationUsed ?? false
                        }).ToList()
                    })
                    .ToList();

                //  Step 7: Hierarchy build karna
                var moduleDict = modulesDefault.ToDictionary(m => m.ModuleId);
                List<GetModuleOperationRolePermissionsResponseDTO> rootModules = new();

                foreach (var module in modulesDefault)
                {
                    var parentId = operationDataDefault.FirstOrDefault(x => x.Id == module.ModuleId)?.ParentModuleId;
                    if (parentId.HasValue && moduleDict.ContainsKey(parentId.Value))
                        moduleDict[parentId.Value].Children.Add(module);
                    else
                        rootModules.Add(module);
                }

                _logger?.LogInformation("Institution module-operation-role data successfully fetched for InstitutionId: {InstitutionId}", dto.InstitutionId);
                return rootModules;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error while fetching Institution module-operation-role permissions for InstitutionId: {InstitutionId}", dto.InstitutionId);
                return null;
            }
        }
        // =====================================================
        // 1️⃣ GET BY ROLE ID
        // =====================================================
        public async Task<List<RoleModuleAndPermission>> GetByRoleIdAsync(
            int roleId,
            long institutionId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("🔹 Fetching permissions for RoleId: {RoleId}", roleId);

                var data = await _context.RoleModuleAndPermissions
                    .AsNoTracking() //  performance
                    .Where(x =>
                        x.RoleId == roleId &&
                        x.Role.InstitutionId == institutionId &&
                        x.IsActive ==true &&
                        (x.IsSoftDeleted != true))
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("✅ Permissions fetched: {Count}", data.Count);

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching Role permissions for RoleId: {RoleId}", roleId);
                throw; // middleware handle karega
            }
        }

        // =====================================================
        // 2️⃣ BULK DELETE
        // =====================================================
        public async Task BulkDeleteAsync(List<RoleModuleAndPermission> list)
        {
            try
            {
                if (list == null || !list.Any())
                    return;

                _logger.LogInformation("🔹 Bulk delete started. Count: {Count}", list.Count);

                // ===============================
                // OPTION 1: HARD DELETE (FASTEST )
                // ===============================
                _context.RoleModuleAndPermissions.RemoveRange(list);

                // ===============================
                // OPTION 2: SOFT DELETE (if needed)
                // ===============================
                /*
                foreach (var item in list)
                {
                    item.IsSoftDeleted = true;
                    item.IsActive = false;
                    item.DeletedDateTime = DateTime.UtcNow;
                }

                _context.RoleModuleAndPermissions.UpdateRange(list);
                */

                _logger.LogInformation("✅ Bulk delete prepared successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in BulkDeleteAsync");
                throw;
            }
        }

    }





    #endregion
}

