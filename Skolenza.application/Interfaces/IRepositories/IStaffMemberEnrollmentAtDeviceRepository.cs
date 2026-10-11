using skolenza.application.DTOS.Device.Enroll;
using skolenza.application.Interfaces.IRepositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.Interfaces.IRepositories
{
    public interface IStaffMemberEnrollmentAtDeviceRepository
    {
        Task<bool> EnrollStaffMemberAsync(RegisterStaffMemberDTORequest dto);

        Task<bool> DeleteStaffMemberAsync(DeleteStaffMemberDTORequest dto);
    }
}
