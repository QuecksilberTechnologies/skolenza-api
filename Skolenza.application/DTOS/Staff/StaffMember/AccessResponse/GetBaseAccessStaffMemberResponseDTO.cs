using skolenza.application.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Staff.AccessResponse
{
    public class GetBaseAccessStaffMemberResponseDTO
    {
        public FieldWithAccess<long> InstitutionId { get; set; } = new();
        public FieldWithAccess<int> StaffMemberDocumentId { get; set; } = new();
        public FieldWithAccess<string> EmployementCode { get; set; } = new();
        public FieldWithAccess<string> LastName { get; set; } = new();
        public FieldWithAccess<string> MiddleName { get; set; } = new();
        public FieldWithAccess<string> FirstName { get; set; } = new();
        public FieldWithAccess<DateTime?> DateOfBirth { get; set; } = new();
        public FieldWithAccess<DateTime?> DateOfOnBoarding { get; set; } = new();
        public FieldWithAccess<DateTime?> DateOfExit { get; set; } = new();
        public FieldWithAccess<int?> DesignationId { get; set; } = new();
        public FieldWithAccess<int> StaffEmploymentTypeId { get; set; } = new();
        public FieldWithAccess<int?> DepartmentId { get; set; } = new();
        public FieldWithAccess<string> OfficialEmail { get; set; } = new();
        public FieldWithAccess<bool> HasPermanent { get; set; } = new();
        public FieldWithAccess<bool> IsActive { get; set; } = new();
        public FieldWithAccess<string?> Remark { get; set; } = new();

        // Audit Fields
        public FieldWithAccess<long?> AddedById { get; set; } = new();
        public FieldWithAccess<DateTime?> AddedDateTime { get; set; } = new();
        public FieldWithAccess<long?> UpdatedById { get; set; } = new();
        public FieldWithAccess<DateTime?> UpdatedDateTime { get; set; } = new();
        public FieldWithAccess<long?> SoftDeletedById { get; set; } = new();
        public FieldWithAccess<DateTime?> DeletedDateTime { get; set; } = new();
        public FieldWithAccess<bool> IsSoftDeleted { get; set; } = new();
        public FieldWithAccess<long?> InfoVerifiedById { get; set; } = new();
        public FieldWithAccess<DateTime?> InfoVerifiedDateTime { get; set; } = new();
        public FieldWithAccess<bool> IsInfoVerified { get; set; } = new();

    }

}
