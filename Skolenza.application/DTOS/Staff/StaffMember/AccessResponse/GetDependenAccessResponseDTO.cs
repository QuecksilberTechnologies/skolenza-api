using skolenza.application.Common.Attributes;
using skolenza.application.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Staff.AccessResponse
{

    public class GetDependenAccessResponseDTO
    {
        public FieldWithAccess<long> Id { get; set; } = new();
        public FieldWithAccess<long?> StaffMemberId { get; set; } = new();
        public FieldWithAccess<string?> DependentName { get; set; } = new();
        public FieldWithAccess<string?> Relation { get; set; } = new();
        public FieldWithAccess<DateTime?> DateOfBirth { get; set; } = new();
        public FieldWithAccess<bool?> IsCoveredInPolicy { get; set; } = new();
        public FieldWithAccess<bool?> IsMarried { get; set; } = new();
        public FieldWithAccess<string?> Remark { get; set; } = new();
        public FieldWithAccess<string?> Description { get; set; } = new();

        // 🔹 Audit Fields
        public FieldWithAccess<long?> AddedById { get; set; } = new();
        public FieldWithAccess<DateTime?> AddedDateTime { get; set; } = new();
        public FieldWithAccess<long?> UpdatedById { get; set; } = new();
        public FieldWithAccess<DateTime?> UpdatedDateTime { get; set; } = new();
        public FieldWithAccess<long?> SoftDeletedById { get; set; } = new();
        public FieldWithAccess<DateTime?> DeletedDateTime { get; set; } = new();

        // 🔹 Flags
        public FieldWithAccess<bool?> IsSoftDeleted { get; set; } = new();
        public FieldWithAccess<bool?> IsActive { get; set; } = new();

        // 🔹 Info Verification
        public FieldWithAccess<long?> InfoVerifiedById { get; set; } = new();
        public FieldWithAccess<bool?> IsInfoVerified { get; set; } = new();
        public FieldWithAccess<bool?> IsEditAllowed { get; set; } = new();
        public FieldWithAccess<DateTime?> InfoVerifiedDateTime { get; set; } = new();
    }

}


