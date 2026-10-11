
using skolenza.application.DTOS.Staff.Sensitive;
using skolenza.application.DTOS.Staff.Type;

using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.StoreProcedures;

using skolenza.domain.Entity; using MediatR;
using System.Collections.Generic;

namespace skolenza.application.Interfaces.IRepositories;

public interface IStaffMemberIdentityRepository
{


    #region StaffMember-Identity-info

   
   
 
    public Task<GetStaffMemberIdentityResponseDTO> GetInfo(GetIdentityRequestDTO dto);
    public Task<bool> CreateAsync(IEnumerable<StaffMemberIdentity> entities);

    /// <summary>Validates a selected document against the staffMember's persisted country catalogue.</summary>
    Task<bool> IsDocumentAllowedAsync(long staffMemberId, long institutionId, int documentId, CancellationToken cancellationToken);
    Task<StaffMemberIdentity?> GetByStaffMemberAndDocumentAsync(
        long staffMemberId,
        int documentId,
        CancellationToken cancellationToken);
    Task<bool> UpdateAsync(StaffMemberIdentity entity, CancellationToken cancellationToken);
    Task<bool> SoftDeleteAsync(StaffMemberIdentity entity, CancellationToken cancellationToken);
    //  public Task<PagedResponseDTO<GetDependentResponseDTO>> AutoCreatedAsync(StaffMemberContact entity);
       public Task<StaffMemberIdentity> GetSingleRecordAsync(long Id, bool IsActive);  // Ensure this returns 
       public Task<bool> IsStaffMemberIdentityExistsAsync(long Id, bool? IsActive);  // Ensure this returns 

    public Task<bool> UpdateIdentity(StaffMemberPersonalDetail staffMemberPersonal);
    #endregion








}

