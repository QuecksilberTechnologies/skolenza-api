using System.Text.Json.Serialization;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOs.Institution;

namespace skolenza.application.DTOS.Institution;

/// <summary>Previews or confirms a institution-owned pattern and staffMember-code change.</summary>
public sealed class SaveStaffMemberCodePatternRequestDTO : PermissionRequestDTO
{
    public NewInstitutionStaffMemberCodePatternRequestDTO Pattern { get; set; } = new();
    public bool Confirm { get; set; }
    public string? PreviewHash { get; set; }
}

/// <summary>Lists the exact code changes requiring approval before any writes.</summary>
public sealed class SaveStaffMemberCodePatternResponseDTO
{
    public string PreviewHash { get; set; } = string.Empty;
    public bool Applied { get; set; }
    public bool CanCommit => Errors.Count == 0;
    public int LastUsedNumber { get; set; }
    public List<StaffMemberCodeChangeDTO> StaffMembers { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

/// <summary>Shows an existing staffMember's current and proposed code.</summary>
public sealed class StaffMemberCodeChangeDTO
{
    [JsonIgnore]
    public long StaffMemberId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CurrentCode { get; set; }
    public string? ProposedCode { get; set; }
    public DateTime? JoiningDate { get; set; }
    public List<string> Errors { get; set; } = new();
}
