// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the repository contract for  Base StaffMember Repository.
// ================================================================

using skolenza.application.DTOs.Department;
using skolenza.application.DTOs.Staff.AccessResponse;
using skolenza.application.DTOS.Staff.Bank;
using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.application.DTOS.Staff.CompletionPercentage;
using skolenza.application.DTOS.Staff.Contact;
using skolenza.application.DTOS.Staff.Dependent;
using skolenza.application.DTOS.Staff.Education;
using skolenza.application.DTOS.Staff.Experience;
using skolenza.application.DTOS.Staff.Sensitive;
using skolenza.application.DTOS.Staff.Type;
using skolenza.application.DTOS.Pagination;
using skolenza.domain.Entity;
using System.Collections.Generic;

namespace skolenza.application.Interfaces.IRepositories;

/// <summary>
/// Defines the contract for BaseStaffMemberRepository.
/// </summary>
public interface IBaseStaffMemberRepository
{
    #region StaffMember-Base-info

    /// <summary>Returns false until the institution explicitly allows operational self-service.</summary>
    Task<bool> IsOperationalSectionEditAllowedAsync(long institutionId, string moduleCode, CancellationToken ct);

    /// <summary>Persists a institution-wide operational self-service default.</summary>
    Task SetOperationalSectionDefaultAsync(long institutionId, string moduleCode, bool isEditAllowed, long actorId, CancellationToken ct);

    Task<bool> UpdateSectionVerifyStatusAsync(
        int sectionName,
        long staffMemberId,
        long institutionId,
        bool isVerified,
        bool isEditAllowed,
        long userId,
        CancellationToken ct);

    Task<bool> UpdateVerificationStatusByTabAsync(
        int tabInfoType,
        long staffMemberId,
        long userStaffMemberId,
        bool isVerified,
        CancellationToken ct);

    Task<bool> UpdateEditableStatusByEntityAsync(
        int tabInfoType,
        long staffMemberId,
        long userStaffMemberId,
        bool isVerified,
        CancellationToken ct);

    Task<PagedResponseDTO<GetBaseStaffMemberResponseDTO>> GetInfo(long institutionId, long staffMemberId, GetBaseStaffMemberRequestDTO dto);

    Task<PagedResponseDTO<GetAllStaffMemberInfoResponseDTO>> GetAllInfo(
        long institutionId,
        long? staffMemberId,
        GetAllStaffMemberInfoRequestDTO dto,
        long requestingStaffMemberId,
        int requestingRoleTypeId);

    Task<StaffMember?> IsStaffMemberExist(string staffMemberCode, long institutionId, bool track = true);

    Task<GetBaseStaffMemberResponseDTO> CreateStaffMemberAsync(
        StaffMember entity,
        LoginCredential loginCredential,
        UserRole userRole);

    Task<GetStaffMemberImageReponseDTO> AddImageAsync(StaffMemberImage entity);

    Task<GetMinimalStaffMemberResponseDTO> GetSingleRecordAsync(long id, bool isActive);

    Task<SummaryStaffMemberInfo?> BuildStaffMemberSummaryAsync(long staffMemberId, bool isActive);

    Task<StaffMemberProfileSummaryInfo?> StaffMemberProfileSummaryAsync(long staffMemberId, bool isActive);

    Task<StaffMemberImage> IsImageExist(long id, bool isActive);

    Task<bool> UpdateProfileImage(StaffMemberImage staffMemberImageInfo);

    Task<StaffMember?> GetByIdAsync(long id, long institutionId, bool track = true);

    Task<bool> DeleteAllAsync(StaffMember staffMember);

    Task<bool> ActivateAllStaffMemberAsync(StaffMember staffMember, bool isActive);

    Task<bool> UpdateStaffMemberAsync(StaffMember entity, long institutionId);

    Task<GetStaffMemberImageReponseDTO> GetImage(long institutionId, long staffMemberId, GetStaffMemberImageRequestDTO dto);

    Task<string?> ProfileImage(long staffMemberId);

    Task<List<CompletionSectionDTO>> GetStaffMemberCompletionAsync(long staffMemberId);

    Task<GetBaseStaffMemberResponseDTO?> GetCreatedStaffMemberResponseAsync(
        long staffMemberId,
        CancellationToken cancellationToken = default);

    Task AddStaffMemberAggregateAsync(
        StaffMember staffMember,
        LoginCredential loginCredential,
        UserRole userRole,
        CancellationToken cancellationToken = default);

    #endregion
}
