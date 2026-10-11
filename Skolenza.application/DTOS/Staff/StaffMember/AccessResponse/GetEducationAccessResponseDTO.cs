using skolenza.application.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Staff.AccessResponse
{
    public class GetEducationAccessResponseDTO
    {
        public FieldWithAccess<int?> Id { get; set; } = new();
        public FieldWithAccess<long?> StaffMemberId { get; set; } = new();

        public FieldWithAccess<string?> Degree { get; set; } = new();
        public FieldWithAccess<string?> InstituteName { get; set; } = new();
        public FieldWithAccess<string?> Remark { get; set; } = new();

        public FieldWithAccess<double?> GapYears { get; set; } = new();

        public FieldWithAccess<DateTime?> StartDate { get; set; } = new();
        public FieldWithAccess<DateTime?> EndDate { get; set; } = new();

        public FieldWithAccess<bool?> HasEducationDocUploded { get; set; } = new();

        public FieldWithAccess<string?> ScoreValue { get; set; } = new();
        public FieldWithAccess<int?> ScoreType { get; set; } = new();
        public FieldWithAccess<string?> GradeDivision { get; set; } = new();

        public FieldWithAccess<string?> FilePath { get; set; } = new();
        public FieldWithAccess<int?> FileType { get; set; } = new();
        public FieldWithAccess<string?> FileName { get; set; } = new();

        public FieldWithAccess<bool?> EducationGap { get; set; } = new();
        public FieldWithAccess<string?> ReasonOfEducationGap { get; set; } = new();

        public FieldWithAccess<long?> AddedById { get; set; } = new();
        public FieldWithAccess<DateTime?> AddedDateTime { get; set; } = new();

        public FieldWithAccess<long?> UpdatedById { get; set; } = new();
        public FieldWithAccess<DateTime?> UpdatedDateTime { get; set; } = new();

        public FieldWithAccess<long?> SoftDeletedById { get; set; } = new();
        public FieldWithAccess<DateTime?> DeletedDateTime { get; set; } = new();

        public FieldWithAccess<bool?> IsSoftDeleted { get; set; } = new();

        public FieldWithAccess<long?> InfoVerifiedById { get; set; } = new();
        public FieldWithAccess<bool?> IsInfoVerified { get; set; } = new();
        public FieldWithAccess<DateTime?> InfoVerifiedDateTime { get; set; } = new();

        public FieldWithAccess<bool?> IsEditAllowed { get; set; } = new();
        public FieldWithAccess<bool?> IsActive { get; set; } = new();
    }
}
