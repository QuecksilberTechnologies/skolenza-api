using skolenza.application.Constants;
using skolenza.application.Constants.skolenza.application.Constants;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Common.SeedData
{
    public static class DepartmentSeedHelper
    {
        public static List<Department> GetRuntimeDepartmentsToSeeds(
            Dictionary<int, string> departmentDict,
            long institutionId,
            int institutionTypeId,
            long empId)
        {
            var departments = new List<Department>();

            foreach (var item in departmentDict)
            {
                string moduleName = item.Value.Trim();

                switch (moduleName)
                {
                    case "HR-Management":
                        departments.Add(new Department
                        {
                            InstitutionId = institutionId,
                          //  InstitutionTypeId = institutionTypeId,
                            DepartmentName = "HR-Department",
                            Description = "Handles staffMember lifecycle",
                            IsActive = true,
                            IsExecutiveOffice = false,
                            Remark = "Seeded from module: HR Management",
                            AddedById = empId,
                            AddedDateTime = DateTime.UtcNow,
                            IsSoftDeleted = false
                        });
                        break;

                    case "Asset-Management":
                        departments.Add(new Department
                        {
                            InstitutionId = institutionId,
                         //   InstitutionTypeId = institutionTypeId,
                            DepartmentName = "Asset-Department",
                            Description = "Manages hardware/software",
                            IsActive = true,
                            IsExecutiveOffice = false,
                            Remark = "Seeded from module: Asset Management",
                            AddedById = empId,
                            AddedDateTime = DateTime.UtcNow,
                            IsSoftDeleted = false
                        });
                        break;

                    case "Payroll-Module":
                        departments.Add(new Department
                        {
                            InstitutionId = institutionId,
                           // InstitutionTypeId = institutionTypeId,
                            DepartmentName = "Finance-Department",
                            Description = "Handles payroll and finance",
                            IsActive = true,
                            IsExecutiveOffice = false,
                            Remark = "Seeded from module: Payroll Module",
                            AddedById = empId,
                            AddedDateTime = DateTime.UtcNow,
                            IsSoftDeleted = false
                        });
                        break;

                        // more cases if needed
                }
            }

            // 🔥 Always add Executive Office (if not already added)
            if (!departments.Any(d => d.DepartmentName.Equals("Executive Office", StringComparison.OrdinalIgnoreCase)))
            {
                departments.Add(new Department
                {
                    InstitutionId = institutionId,
                   // InstitutionTypeId = institutionTypeId,
                    DepartmentName = "Executive-Office",
                    Description = "Handles company-wide strategy and leadership.",
                    IsActive = true,
                    IsExecutiveOffice = true,
                    Remark = "Default executive department",
                    AddedById = empId,
                    AddedDateTime = DateTime.UtcNow,
                    IsSoftDeleted = false
                });
            }

            return departments;
        }
    }


}
