using skolenza.application.Interfaces.IPermission;
using skolenza.application.Interfaces.IRepositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace skolenza.infrastructure.Permission
{
    public class PermissionService : IPermissionService
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(
            IPermissionRepository permissionRepository,
            IMemoryCache cache,
            ILogger<PermissionService> logger)
        {
            _permissionRepository = permissionRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<string>> GetPermissionsAsync(int roleId)
        {
            // 🧠 Cache Key
            string cacheKey = $"RolePermissions_{roleId}";

            // ✅ Check Cache
            if (_cache.TryGetValue(cacheKey, out List<string> cachedPermissions))
            {
                _logger.LogInformation($"✅ Permissions loaded from cache for RoleId: {roleId}");
                return cachedPermissions;
            }

            // ❌ Not Found in Cache → Get from DB
            var permissions = await _permissionRepository.GetPermissionsByRoleAsync(roleId);

            // 🧱 Save in Cache
            _cache.Set(cacheKey, permissions, TimeSpan.FromHours(1));

            _logger.LogInformation($"🧠 Permissions cached for RoleId: {roleId}");
            return permissions;
        }

        public async Task InvalidatePermissionsAsync(int roleId)
        {
            string cacheKey = $"RolePermissions_{roleId}";
            _cache.Remove(cacheKey);
            _logger.LogInformation($"🗑️ Cache cleared for RoleId: {roleId}");
        }
    }
}
