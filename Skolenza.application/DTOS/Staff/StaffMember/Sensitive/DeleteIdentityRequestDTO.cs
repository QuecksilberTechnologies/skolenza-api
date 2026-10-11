using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOS.Staff.Sensitive;

/// <summary>
/// Identifies one persisted staffMember identity record for soft deletion.
/// </summary>
public sealed class DeleteIdentityRequestDTO : PermissionRequestDTO
{
    public long StaffMemberIdentityId { get; set; }
}
