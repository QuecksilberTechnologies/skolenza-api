using skolenza.application.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Staff.AccessResponse
{
    public class GetPersonalAccessResponseDTO
    {
        public FieldWithAccess<long> Id { get; set; } = new();
        public FieldWithAccess<long> StaffMemberId { get; set; } = new();
        public FieldWithAccess<string?> AadhaarNumber { get; set; } = new();
        public FieldWithAccess<string?> PanNumber { get; set; } = new();
        public FieldWithAccess<string?> PassportNumber { get; set; } = new();
        public FieldWithAccess<string?> DrivingLicenseNumber { get; set; } = new();
        public FieldWithAccess<string?> VoterId { get; set; } = new();
        public FieldWithAccess<string?> BloodGroup { get; set; } = new();
        public FieldWithAccess<string?> MaritalStatus { get; set; } = new();
        public FieldWithAccess<string?> Nationality { get; set; } = new();
        public FieldWithAccess<string?> EmergencyContactName { get; set; } = new();
        public FieldWithAccess<string?> EmergencyContactNumber { get; set; } = new();

        public FieldWithAccess<bool> IsActive { get; set; } = new();
        public FieldWithAccess<bool> IsEditAllowed { get; set; } = new();
        public FieldWithAccess<bool?> IsSoftDeleted { get; set; } = new();

        public FieldWithAccess<long?> AddedById { get; set; } = new();
        public FieldWithAccess<DateTime?> AddedDateTime { get; set; } = new();
        public FieldWithAccess<long?> UpdatedById { get; set; } = new();
        public FieldWithAccess<DateTime?> UpdatedDateTime { get; set; } = new();
        public FieldWithAccess<long?> SoftDeletedById { get; set; } = new();
        public FieldWithAccess<DateTime?> DeletedDateTime { get; set; } = new();
        public FieldWithAccess<long?> InfoVerifiedById { get; set; } = new();
        public FieldWithAccess<DateTime?> InfoVerifiedDateTime { get; set; } = new();
        public FieldWithAccess<bool?> IsInfoVerified { get; set; } = new();
    }

}
