using skolenza.application.Constants;
using skolenza.domain.Entity;

namespace skolenza.application.Common.SeedData;

public static class DesignationsSeedHelper
{
    public static List<Designation> GetRuntimeDesignationsToSeed(
        long institutionId,
        long addedById,
        IReadOnlyDictionary<string, int> departmentNameIdMap)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(institutionId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(addedById);

        var departmentIds = new Dictionary<string, int>(
            departmentNameIdMap,
            StringComparer.OrdinalIgnoreCase);

        return DesignationConstants.DefaultDesignations
            .Where(definition => departmentIds.ContainsKey(definition.DepartmentName))
            .Select(definition => new Designation
            {
                InstitutionId = institutionId,
                DepartmentId = departmentIds[definition.DepartmentName],
                DesignationName = definition.Name,
                Description = definition.Description,
                IsActive = true,
                IsSoftDeleted = false,
                AddedById = addedById,
                AddedDateTime = DateTime.UtcNow
            })
            .ToList();
    }
}
