// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the repository contract for  StaffMember Expereince Repository.
// ================================================================

using skolenza.application.DTOS.Staff.Experience;
using skolenza.application.DTOS.Pagination;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
namespace skolenza.application.Interfaces.IRepositories;

/// <summary>
/// Defines the contract for StaffMemberExperienceRepository.
/// </summary>
public interface IStaffMemberExperienceRepository
{
    //  CREATE
    Task<GetStaffMemberExperienceResponseDTO> AddAsync(StaffMemberExperience entity);

    //  UPDATE
    Task<bool> UpdateAsync(StaffMemberExperience entity);

    //  DELETE (Soft Delete)
    Task<bool> SoftDeleteAsync(StaffMemberExperience entity);
    Task<bool> SoftDeleteDocAsync(StaffMemberExperienceDocument entity);

    //  GET BY ID
    Task<StaffMemberExperience?> GetByIdAsync(long id, long staffMemberid);

    //  GET LIST (WITH DETAILS)
    Task<PagedResponseDTO<GetStaffMemberExperienceResponseDTO>> GetByStaffMemberIdWithDocumentsAsync(long staffMemberId, GetExperienceRequestDTO staffMember);
}

