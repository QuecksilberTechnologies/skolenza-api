using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Interfaces.IRepositories
{
    public interface IEntitySecurityValidator
    {
        Task<bool> HasPermissionAsync(long userId, string permissionCode);
        Task<bool> IsInstitutionValidAsync(long userId, long? InstitutionId);
        Task<bool> IsDuplicateAssetTypeAsync(long? InstitutionId, string typeName);
    }

}
