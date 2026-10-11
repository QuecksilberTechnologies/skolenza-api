using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Interfaces.IPermission
{
    public interface IPermissionService
    {
        Task<List<string>> GetPermissionsAsync(int roleId);
        Task InvalidatePermissionsAsync(int roleId); // Role change hone pe call hoga
    }
}
