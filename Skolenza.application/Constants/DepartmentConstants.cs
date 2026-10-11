using System;
using System.Collections.Generic;
using global::skolenza.application.Common.SeedData;
using System;
using System.Collections.Generic;
namespace skolenza.application.Constants
{
 

    /// <summary>
        /// Contains default department seed data (partial). Remaining fields to be assigned at runtime.
        /// </summary>
        public static class DepartmentConstants
        {
            public static readonly List<DepartmentSeedData> DefaultDepartments = new()
            {
        new DepartmentSeedData
        {
            DepartmentName = "Executive-Office",
            Description = "Handles company-wide strategy and leadership.",
            IsActive = true,
            IsExecutiveOffice = true
        },
        new DepartmentSeedData
        {
            DepartmentName = "IT-Department",
            Description = "Manages technology, software, and infrastructure support.",
            IsActive = true,
            IsExecutiveOffice = false
        },
        new DepartmentSeedData
        {
            DepartmentName = "HR-Department",
            Description = "Handles staffMember lifecycle, recruitment, and engagement.",
            IsActive = true,
            IsExecutiveOffice = false
        },
        new DepartmentSeedData
        {
            DepartmentName = "Finance-Department",
            Description = "Manages budgeting, accounts, and financial audits.",
            IsActive = true,
            IsExecutiveOffice = false
        },
        new DepartmentSeedData
        {
            DepartmentName = "Asset-Department",
            Description = "Oversees allocation, tracking, and maintenance of assets.",
            IsActive = true,
            IsExecutiveOffice = false
        },
        new DepartmentSeedData
        {
            DepartmentName = "Admin-Department",
            Description = "Takes care of facilities and administrative support.",
            IsActive = true,
            IsExecutiveOffice = false
        }
    };
        }



    }

 
