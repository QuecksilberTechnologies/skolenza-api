// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines editable fields for a partial Operation update request.
// ================================================================

namespace skolenza.application.DTOs.Operation;

/// <summary>
/// Represents the request to update an existing operation.
/// </summary>
public class UpdateOperationRequestDTO
{
    #region Update Properties

    /// <summary>
    /// Gets or sets the legacy product-owner identifier retained for request compatibility.
    /// The authenticated Host user is used for audit attribution.
    /// </summary>
    public required int ProductOwnerId { get; set; }

    /// <summary>
    /// Gets or sets the product-owner role identifier retained by the existing request contract.
    /// </summary>
    public required int ProductOwnerRoleId { get; set; }

    /// <summary>
    /// Gets or sets the operation identifier to update.
    /// </summary>
    public required int Id { get; set; }

    /// <summary>
    /// Gets or sets the optional operation type.
    /// </summary>
    public int? OperationType { get; set; }

    /// <summary>
    /// Gets or sets the optional operation name.
    /// </summary>
    public string? OperationName { get; set; }

    /// <summary>
    /// Gets or sets the optional operation remark.
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// Gets or sets the optional operation icon image reference.
    /// </summary>
    public string? IconImage { get; set; }

    /// <summary>
    /// Gets or sets the requested active state.
    /// A <see langword="null"/> value preserves the current state during a partial update.
    /// </summary>
    public bool? IsActive { get; set; }

    #endregion
}
