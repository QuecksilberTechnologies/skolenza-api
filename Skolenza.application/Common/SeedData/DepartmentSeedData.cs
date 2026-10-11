using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Common.SeedData
{
    /// <summary>
    /// Model for department seed data. Only some fields are pre-filled; others to be populated at runtime.
    /// </summary>
    public class DepartmentSeedData
    {
        public long Id { get; set; }
        public long InstitutionId { get; set; }
        public long InstitutionTypeId { get; set; }
        public string DepartmentName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsExecutiveOffice { get; set; }
        public string Remark { get; set; }
        public long? AddedById { get; set; }
        public DateTime? AddedDateTime { get; set; }
        public long? UpdatedById { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public bool IsSoftDeleted { get; set; }
        public long? SoftDeletedById { get; set; }
        public DateTime? DeletedDateTime { get; set; }
    }

}
