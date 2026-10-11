using skolenza.application.Common.SeedData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Constants
{
    namespace skolenza.application.Constants
    {
        /// <summary>
        /// Static list of default designations to seed.
        /// DepartmentName is used to map to actual DepartmentId at runtime.
        /// </summary>
        public static class DesignationConstants
        {
            public static readonly List<DesignationSeedData> DefaultDesignations = new()
    {
        new DesignationSeedData
        {
            DesignationName = "CEO",
            DepartmentName = "Executive Office",
            Description = "Chief Executive Officer - Responsible for overall business strategy and vision.",
            IsExecutive = true
        },
        new DesignationSeedData
        {
            DesignationName = "Sr. Software Er",
            DepartmentName = "IT",
            Description = "Senior Software Engineer"
        },
        new DesignationSeedData
        {
            DesignationName = "HR Executive",
            DepartmentName = "HR",
            Description = "Handles HR operations"
        },
        new DesignationSeedData
        {
            DesignationName = "Finance Head",
            DepartmentName = "Finance",
            Description = "Leads financial planning"
        }
        // Add more based on your use case...
    };
        }



    }



}
