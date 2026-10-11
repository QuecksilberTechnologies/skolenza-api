using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

using System;
using skolenza.application.Common.Attributes;

namespace skolenza.application.DTOs.Staff.AccessControlReadOnlyType
{
    public class StaffMemberEducationEditableFieldsDTO
    {
        [AccessControl(ReadOnly = true)]
        public int Id { get; set; }

        [AccessControl(ReadOnly = true)]
        public long StaffMemberId { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? Degree { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? InstituteName { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? Remark { get; set; }

        [AccessControl(ReadOnly = false)]
        public double GapYears { get; set; }

        [AccessControl(ReadOnly = false)]
        public DateTime? StartDate { get; set; }

        [AccessControl(ReadOnly = false)]
        public DateTime? EndDate { get; set; }

        [AccessControl(ReadOnly = false)]
        public bool HasEducationDocUploded { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? ScoreValue { get; set; }

        [AccessControl(ReadOnly = false)]
        public int? ScoreType { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? GradeDivision { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? FilePath { get; set; }

        [AccessControl(ReadOnly = false)]
        public int? FileType { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? FileName { get; set; }

        [AccessControl(ReadOnly = false)]
        public bool? EducationGap { get; set; }

        [AccessControl(ReadOnly = false)]
        public string? ReasonOfEducationGap { get; set; }

        [AccessControl(ReadOnly = true)]
        public long AddedById { get; set; }

        [AccessControl(ReadOnly = true)]
        public DateTime AddedDateTime { get; set; }

        [AccessControl(ReadOnly = true)]
        public long? UpdatedById { get; set; }

        [AccessControl(ReadOnly = true)]
        public DateTime? UpdatedDateTime { get; set; }

        [AccessControl(ReadOnly = true)]
        public long? SoftDeletedById { get; set; }

        [AccessControl(ReadOnly = true)]
        public DateTime? DeletedDateTime { get; set; }

        [AccessControl(ReadOnly = true)]
        public bool? IsSoftDeleted { get; set; }

        [AccessControl(ReadOnly = true)]
        public long? InfoVerifiedById { get; set; }

        [AccessControl(ReadOnly = true)]
        public bool? IsInfoVerified { get; set; }

        [AccessControl(ReadOnly = true)]
        public DateTime? InfoVerifiedDateTime { get; set; }

        [AccessControl(ReadOnly = true)]
        public bool? IsEditAllowed { get; set; }

        [AccessControl(ReadOnly = true)]
        public bool? IsActive { get; set; }
    }


}

