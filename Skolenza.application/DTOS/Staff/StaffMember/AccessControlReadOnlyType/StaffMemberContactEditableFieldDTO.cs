using skolenza.application.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Staff.AccessControlReadOnlyType
{
    public class StaffMemberContactEditableFieldDTO
    {
        [AccessControl(ReadOnly = true)]
        public int Id { get; }
        [AccessControl(ReadOnly = true)]
        public long InstitutionId { get; }

        [AccessControl(ReadOnly = true)]
        public long StaffMemberId { get; set; }

        [AccessControl(ReadOnly = false)]      

        public int? ContactType { get; set; }
        [AccessControl(ReadOnly = false)]

        public string ContactNumber { get; set; } = string.Empty;
        [AccessControl(ReadOnly = false)]


        public string? AlternateNumber { get; set; }
        [AccessControl(ReadOnly = false)]


        public string? Email { get; set; }
        [AccessControl(ReadOnly = false)]


        public bool? IsPrimary { get; set; }
        [AccessControl(ReadOnly = false)]

        public int? CountryId { get; set; }
        [AccessControl(ReadOnly = false)]


        public int? StateId { get; set; }
        [AccessControl(ReadOnly = false)]


        public int? DistrictId { get; set; }
        [AccessControl(ReadOnly = false)]


        public string? HouseNo { get; set; }
        [AccessControl(ReadOnly = false)]


        public string? LandMark { get; set; }
        [AccessControl(ReadOnly = false)]


        public string? Street { get; set; }
        [AccessControl(ReadOnly = false)]


        public string? LocalAddress { get; set; }
        [AccessControl(ReadOnly = false)]


        public string? PermanentAddress { get; set; }
        [AccessControl(ReadOnly = false)]


        public string? Remark { get; set; }
        [AccessControl(ReadOnly = false)]


        public bool IsActive { get; set; }


        [AccessControl(ReadOnly = false)]

        public long? UpdatedById { get; set; }

        public DateTime? UpdatedDateTime { get; set; }
        [AccessControl(ReadOnly = false)]


        public bool? IsEditAllowed { get; set; }
        [AccessControl(ReadOnly = false)]


        public bool? IsInfoVerified { get; set; }
        [AccessControl(ReadOnly = false)]


        public long? InfoVerifiedById { get; set; }
        [AccessControl(ReadOnly = false)]


        public DateTime? InfoVerifiedDateTime { get; set; }
        [AccessControl(ReadOnly = false)]

        public string? Description { get; set; }
 

    }

}
