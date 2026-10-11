using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

using System;
using skolenza.application.Common.Attributes;

namespace skolenza.application.DTOs.Staff.AccessControlReadOnlyType
{
    public class StaffMemberExperienceEditableFieldsDTO
    {
        [AccessControl(ReadOnly = true)]
        public int Id { get; set; }

        [AccessControl(ReadOnly = true)]
        public int StaffMemberId { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? CompanyName { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? JobTitle { get; set; }

        [AccessControl(ReadOnly = false)]
        public DateTime? StartDate { get; set; }

        [AccessControl(ReadOnly = false)]
        public DateTime? EndDate { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? ReasonForLeaving { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? Remark { get; set; }

        [AccessControl(ReadOnly = true)]
        public bool IsExperienceVerified { get; set; }

        [AccessControl(ReadOnly = true)]
        public string? ExperienceVerificationBy { get; set; }

        [AccessControl(ReadOnly = true)]
        public bool IsSoftDeleted { get; set; }

        [AccessControl(ReadOnly = false)]
        public int? ExperienceTypeId { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? Location { get; set; }

        [AccessControl(ReadOnly = false)]
        public decimal? CTC { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? ReportingManagerName { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? ReportingManagerNumber { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? ReportingManagerEmail { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? WorkedWithName { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? WorkedWithContactNumber { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? WorkedWithDesignation { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? ExperienceLetterPath { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? Comment { get; set; }

        [AccessControl(ReadOnly = true)]
        public long? AddedById { get; set; }

        [AccessControl(ReadOnly = true)]
        public DateTime? AddedDateTime { get; set; }

        [AccessControl(ReadOnly = true)]
        public long? UpdatedById { get; set; }

        [AccessControl(ReadOnly = true)]
        public DateTime? UpdatedDateTime { get; set; }

        [AccessControl(ReadOnly = true)]
        public long? SoftDeletedById { get; set; }

        [AccessControl(ReadOnly = true)]
        public DateTime? DeletedDateTime { get; set; }

        [AccessControl(ReadOnly = true)]
        public bool IsExperienceVerifiedByMail { get; set; }

        [AccessControl(ReadOnly = true)]
        public bool IsExperienceVerifiedByCall { get; set; }

        [AccessControl(ReadOnly = true)]
        public long? InfoVerifiedById { get; set; }

        [AccessControl(ReadOnly = true)]
        public bool IsInfoVerified { get; set; }

        [AccessControl(ReadOnly = true)]
        public DateTime? InfoVerifiedDateTime { get; set; }

        [AccessControl(ReadOnly = true)]
        public DateTime? ExperienceVerificationDateTime { get; set; }

        [AccessControl(ReadOnly = true)]
        public bool IsEditAllowed { get; set; }

        [AccessControl(ReadOnly = true)]
        public bool IsActive { get; set; }
    }
}

