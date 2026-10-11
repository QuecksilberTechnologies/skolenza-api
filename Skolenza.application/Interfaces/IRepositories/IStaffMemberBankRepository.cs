// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the repository contract for  StaffMember Bank Repository.
// ================================================================



using skolenza.application.DTOs.Staff.AccessResponse;
using skolenza.application.DTOS.Staff.Bank;
using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.application.DTOS.Staff.CompletionPercentage;
using skolenza.application.DTOS.Staff.Contact;
 
 
using skolenza.application.DTOS.Staff.Type;

using skolenza.application.DTOS.Pagination;
 using skolenza.domain.Entity; using MediatR;  

using skolenza.domain.Entity; using MediatR;
using System.Collections.Generic;

namespace skolenza.application.Interfaces.IRepositories;

/// <summary>
/// Defines the contract for StaffMemberBankRepository.
/// </summary>
public interface IStaffMemberBankRepository
{

    #region Bank-info
    public Task<bool> UpdateEditStatus(long StaffMemberId, long UserId, bool Status);
    public Task<bool> UpdateVerificationStatus(long StaffMemberId, long UserId, bool Status);
    public Task< PagedResponseDTO<GetBankResponseDTO>> CreateAsync(StaffMemberBankDetail entity);  // Ensure this returns 
    public Task<StaffMemberBankDetail> GetSingleRecordAsync(int Id ,bool IsActive);  // Ensure this returns    
    public Task<PagedResponseDTO<GetBankResponseDTO>> GetInfoAsync(long staffMemberId, GetBankReqestDTO dto);
    public Task<bool> ResetPrimaryAccountAsync(long staffMemberId, long byUserId);
    public Task<bool> DeleteAsync(StaffMemberBankDetail staffMemberBankDetail);
    public Task<bool> UpdateAsync(StaffMemberBankDetail entity);



    #endregion

}

