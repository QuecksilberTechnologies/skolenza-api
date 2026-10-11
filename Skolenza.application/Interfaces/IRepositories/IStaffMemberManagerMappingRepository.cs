using skolenza.domain.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.Interfaces.IRepositories
{
    public interface IStaffMemberManagerMappingRepository
    {
          Task<bool> AddAsync(StaffMemberManagerMapping entity);
          Task<bool>  ExistsPrimaryAsync(long staffMemberId, long institutionId);

          /// <summary>
          /// Checks whether the specified manager currently has an active direct-report mapping
          /// to the requested staffMember in the same institution.
          /// </summary>
          Task<bool> IsCurrentDirectReportAsync(
              long institutionId,
              long managerId,
              long staffMemberId,
              CancellationToken cancellationToken = default);
    }
}
