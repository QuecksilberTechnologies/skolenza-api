using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.DTOS.Device.Enroll
{
    public class RegisterStaffMemberDTORequest
    {
        public long StaffMemberId { get; set; }
        public long InstitutionId { get; set; }
        public string StaffMemberCode { get; set; } = null!;
        public string DeviceSn { get; set; } = null!;
        public string Name { get; set; }
        public int DeviceId { get; set; }
    }
    public class DeleteStaffMemberDTORequest
    {
        public long StaffMemberId { get; set; }
        public long InstitutionId { get; set; }
        public string StaffMemberCode { get; set; } = null!;
        public string DeviceSn { get; set; } = null!;
        public string Name { get; set; }
        public int DeviceId { get; set; }

    }
    
}
