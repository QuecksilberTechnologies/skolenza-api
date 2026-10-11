// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the repository contract for  StaffMember Contact Repository.
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
/// Defines the contract for StaffMemberContactRepository.
/// </summary>
public interface IStaffMemberContactRepository
{



    #region StaffMember-Contact-info
    public Task<PagedResponseDTO<GetContactResponseDTO>> GetInfo(long staffMemberId, GetContactRequestDTO dto);
    public Task<PagedResponseDTO<GetContactResponseDTO>> CreateAsync(StaffMemberContact entity);
    public Task<PagedResponseDTO<GetContactResponseDTO>> AutoCreatedAsync(StaffMemberContact entity);  
    public  Task<StaffMemberContact?> GetSingleRecordAsync(long id, bool track = true);// Ensure this returns 
    public  Task<StaffMemberContact?> GetPrimaryLocationAsync(long staffMemberId, bool isActive, bool track = true);// Ensure this returns 

    public Task<bool> UpdateContactAsync(StaffMemberContact staffMemberContact);
    public Task<bool> DeleteAsync(StaffMemberContact staffMemberContact);


    #endregion

}

