// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the repository contract for  StaffMember Dependent Repository.
// ================================================================



using skolenza.application.DTOs.Staff.AccessResponse;
using skolenza.application.DTOS.Staff.Bank;
using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.application.DTOS.Staff.Contact;
 
using skolenza.application.DTOS.Staff.Dependent;
using skolenza.application.DTOS.Staff.Education;
using skolenza.application.DTOS.Staff.Experience;
using skolenza.application.DTOS.Staff.Sensitive;
using skolenza.application.DTOS.Staff.Type;

using skolenza.application.DTOS.Pagination;
 

using skolenza.domain.Entity; using MediatR;
using System.Collections.Generic;

namespace skolenza.application.Interfaces.IRepositories;

/// <summary>
/// Defines the contract for StaffMemberDependentRepository.
/// </summary>
public interface IStaffMemberDependentRepository
{
 


    #region StaffMember-dependent-info
    public Task<PagedResponseDTO<GetDependentResponseDTO>> GetInfo(long staffMemberId, GetDependentRequestDTO dto);
    public Task<List<GetDependentResponseDTO>> GetBulkInfo(List<long> dependentIds);
    public Task<GetDependentsDetailResponseDTO> GetDetailInfo(long staffMemberId, GetDependentRequestDTO dto);
    public Task<PagedResponseDTO<GetDependentResponseDTO>> CreateAsync(StaffMemberDependent entity);
  //  public Task<PagedResponseDTO<GetDependentResponseDTO>> AutoCreatedAsync(StaffMemberContact entity);
    public Task<StaffMemberDependent> GetSingleRecordAsync(long Id, bool IsActive);  // Ensure this returns 
    public Task<bool> UpdateAsync(StaffMemberDependent dependent);
    public Task<bool> UpdateAsyncRangeAsync(List<StaffMemberDependent>dependents);
    public Task<bool> DeleteAsync(StaffMemberDependent dependent);


    #endregion


}

