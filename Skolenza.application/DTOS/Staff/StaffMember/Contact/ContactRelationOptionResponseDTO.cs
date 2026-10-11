// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the response contract for StaffMember contact relation options.
// ================================================================

namespace skolenza.application.DTOS.Staff.Contact;

/// <summary>
/// Represents one stable StaffMember contact relation option.
/// </summary>
public sealed class ContactRelationOptionResponseDTO
{
    public int Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;
}
