// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the repository contract for  StaffMember Education Repository.
// ================================================================

using skolenza.application.DTOS.Staff.Education;

using skolenza.application.DTOS.Pagination;


using skolenza.domain.Entity;


namespace skolenza.application.Interfaces.IRepositories;

/// <summary>
/// Defines the contract for StaffMemberEducationRepository.
/// </summary>
public interface IStaffMemberEducationRepository
{



    #region StaffMember-Education-info
    public  Task<bool> DeleteAsync(StaffMemberEducation staffMemberEducation);
    public Task<PagedResponseDTO<GetEducationResponseDTO>> GetInfo(long staffMemberId, GetEducationRequestDTO dto);
    public Task<PagedResponseDTO<GetEducationResponseDTO>> CreateAsync(StaffMemberEducation entity);
    //  public Task<PagedResponseDTO<GetDependentResponseDTO>> AutoCreatedAsync(StaffMemberContact entity);
    public Task<StaffMemberEducation> GetSingleRecordAsync(long Id, bool IsActive);  // Ensure this returns    
    public   Task<bool> UpdateStaffMemberFieldAsync(StaffMemberEducation entity);
    #endregion




}

