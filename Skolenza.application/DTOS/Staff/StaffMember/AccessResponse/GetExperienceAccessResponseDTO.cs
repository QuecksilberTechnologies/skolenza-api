using skolenza.application.Common.Attributes;
using skolenza.application.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Staff.AccessResponse
{

    public class GetExperienceAccessResponseDTO
    {
        public FieldWithAccess<long?> Id { get; set; } = new();
        public FieldWithAccess<long?> StaffMemberId { get; set; } = new();
        public FieldWithAccess<string?> CompanyName { get; set; } = new();
        public FieldWithAccess<string?> JobTitle { get; set; } = new();
        public FieldWithAccess<DateTime?> StartDate { get; set; } = new();
        public FieldWithAccess<DateTime?> EndDate { get; set; } = new();
        public FieldWithAccess<string?> ReasonForLeaving { get; set; } = new();
        public FieldWithAccess<string?> Remark { get; set; } = new();
        public FieldWithAccess<bool?> IsExperienceVerified { get; set; } = new();
        public FieldWithAccess<long?> ExperienceVerificationBy { get; set; } = new();
        public FieldWithAccess<bool?> IsSoftDeleted { get; set; } = new();
        public FieldWithAccess<int?> ExperienceTypeId { get; set; } = new();
        public FieldWithAccess<string?> Location { get; set; } = new();
        public FieldWithAccess<decimal?> CTC { get; set; } = new();
        public FieldWithAccess<string?> ReportingManagerName { get; set; } = new();
        public FieldWithAccess<string?> ReportingManagerNumber { get; set; } = new();
        public FieldWithAccess<string?> ReportingManagerEmail { get; set; } = new();
        public FieldWithAccess<string?> WorkedWithName { get; set; } = new();
        public FieldWithAccess<string?> WorkedWithContactNumber { get; set; } = new();
        public FieldWithAccess<string?> WorkedWithDesignation { get; set; } = new();
        public FieldWithAccess<string?> ExperienceLetterPath { get; set; } = new();
        public FieldWithAccess<string?> Comment { get; set; } = new();
        public FieldWithAccess<long?> AddedById { get; set; } = new();
        public FieldWithAccess<DateTime?> AddedDateTime { get; set; } = new();
        public FieldWithAccess<long?> UpdatedById { get; set; } = new();
        public FieldWithAccess<DateTime?> UpdatedDateTime { get; set; } = new();
        public FieldWithAccess<long?> SoftDeletedById { get; set; } = new();
        public FieldWithAccess<DateTime?> DeletedDateTime { get; set; } = new();
        public FieldWithAccess<bool?> IsExperienceVerifiedByMail { get; set; } = new();
        public FieldWithAccess<bool?> IsExperienceVerifiedByCall { get; set; } = new();
        public FieldWithAccess<long?> InfoVerifiedById { get; set; } = new();
        public FieldWithAccess<bool?> IsInfoVerified { get; set; } = new();
        public FieldWithAccess<DateTime?> InfoVerifiedDateTime { get; set; } = new();
        public FieldWithAccess<DateTime?> ExperienceVerificationDateTime { get; set; } = new();
        public FieldWithAccess<bool?> IsEditAllowed { get; set; } = new();
        public FieldWithAccess<bool?> IsActive { get; set; } = new();
    }


}
