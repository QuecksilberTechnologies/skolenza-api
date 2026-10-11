using skolenza.application.Common.Attributes;
using skolenza.application.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Staff.AccessResponse
{

    public class GetContactAccessResponseDTO
    {
        public FieldWithAccess<long> Id { get; set; } = new();
        public FieldWithAccess<long?> StaffMemberId { get; set; } = new();
        public FieldWithAccess<int?> ContactType { get; set; } = new();
        public FieldWithAccess<string> ContactNumber { get; set; } = new();
        public FieldWithAccess<string?> AlternateNumber { get; set; } = new();
        public FieldWithAccess<string?> Email { get; set; } = new();
        public FieldWithAccess<bool?> IsPrimary { get; set; } = new();
        public FieldWithAccess<int?> CountryId { get; set; } = new();
        public FieldWithAccess<int?> StateId { get; set; } = new();
        public FieldWithAccess<int?> DistrictId { get; set; } = new();
        public FieldWithAccess<string?> HouseNo { get; set; } = new();
        public FieldWithAccess<string?> LandMark { get; set; } = new();
        public FieldWithAccess<string?> Street { get; set; } = new();
        public FieldWithAccess<string?> LocalAddress { get; set; } = new();
        public FieldWithAccess<string?> PermanentAddress { get; set; } = new();
        public FieldWithAccess<string?> Remark { get; set; } = new();
        public FieldWithAccess<bool> IsActive { get; set; } = new();
        public FieldWithAccess<long?> UpdatedById { get; set; } = new();
        public FieldWithAccess<DateTime?> UpdatedDateTime { get; set; } = new();
        public FieldWithAccess<bool?> IsEditAllowed { get; set; } = new();
        public FieldWithAccess<bool?> IsInfoVerified { get; set; } = new();
        public FieldWithAccess<long?> InfoVerifiedById { get; set; } = new();
        public FieldWithAccess<DateTime?> InfoVerifiedDateTime { get; set; } = new();
        public FieldWithAccess<string?> Description { get; set; } = new();
    }

}


