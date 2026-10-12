using skolenza.application.Constants;
using skolenza.domain.Entity;

namespace skolenza.application.Common.SeedData;

public static class DepartmentSeedHelper
{
    public static List<Department> GetRuntimeDepartmentsToSeed(
        long institutionId,
        long addedById)
    {
        return DepartmentConstants.DefaultDepartments
            .Select(definition => new Department
            {
                InstitutionId = institutionId,
                DepartmentName = definition.Name,
                Description = definition.Description,
                IsActive = true,
                IsExecutiveOffice = definition.IsExecutiveOffice,
                Remark = definition.Remark,
                AddedById = addedById,
                AddedDateTime = DateTime.UtcNow,
                IsSoftDeleted = false
            })
            .ToList();
    }
}
