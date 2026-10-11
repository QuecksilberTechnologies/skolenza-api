using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using skolenza.domain.Entity;

namespace skolenza.persistance.Repositories
{
    public class EntitySecurityValidator : IEntitySecurityValidator
    {
        private readonly IUnitOfWork _unitOfWork;

        public EntitySecurityValidator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> HasPermissionAsync(long userId, string permissionCode)
        {
            //// Fetch user's roles and permissions
            //return await _unitOfWork.UserRoleRepository
            //       .HasPermission(userId, permissionCode);
            return false;
        }

        public async Task<bool> IsInstitutionValidAsync(long userId, long? institutionId)
        {
            // return await _unitOfWork.InstitutionRepository.IsValidInstitution(userId, institutionId);
            return false;
        }

        public async Task<bool> IsDuplicateAssetTypeAsync(long? institutionId, string typeName)
        {
          //  return await _unitOfWork.AssetRepository.CheckDuplicateAssetType(institutionId, typeName);
            return false;
        }
    }

}
