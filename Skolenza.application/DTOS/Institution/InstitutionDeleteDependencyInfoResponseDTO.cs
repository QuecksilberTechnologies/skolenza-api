// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the read-only Institution deletion dependency information response.
// ================================================================

namespace skolenza.application.DTOs.Institution;

/// <summary>Describes the transactional data groups that a future Institution deletion workflow must process.</summary>
public sealed class InstitutionDeleteDependencyInfoResponseDTO
{
    public string InstitutionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<string> TransactionalDataGroups { get; set; } = new();
}
