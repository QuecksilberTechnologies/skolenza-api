// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines only the client-editable Institution fields accepted by the Host-managed route update endpoint.
// ================================================================

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOs.Institution;

/// <summary>
/// Represents editable Institution details for the Host-managed route update endpoint.
/// </summary>
public class UpdateHostManagedInstitutionRequestDTO : PermissionRequestDTO
{
    #region Editable Institution Properties

    /// <summary>
    /// Gets or sets the Institution type identifier.
    /// </summary>
    public int InstitutionTypeId { get; set; }

    /// <summary>
    /// Gets or sets the Institution company name.
    /// </summary>
    public string? CompanyName { get; set; }

    /// <summary>
    /// Gets or sets the Institution code.
    /// </summary>
    public string? InstitutionCode { get; set; }

    /// <summary>
    /// Gets or sets the Institution company email domain.
    /// </summary>
    public string? CompanyEmailDomain { get; set; }

    /// <summary>
    /// Gets or sets the Institution email address.
    /// </summary>
    public string? InstitutionEmail { get; set; }

    /// <summary>
    /// Gets or sets the Institution contact person name.
    /// </summary>
    public string? ContactPersonName { get; set; }

    /// <summary>
    /// Gets or sets the Institution contact-person gender identifier.
    /// </summary>
    public int? GenderId { get; set; }

    /// <summary>
    /// Gets or sets the Institution contact number.
    /// </summary>
    public string? ContactNumber { get; set; }

    /// <summary>
    /// Gets or sets the Institution country identifier.
    /// </summary>
    public int CountryId { get; set; }

    /// <summary>
    /// Gets or sets the Institution default currency identifier.
    /// </summary>
    public int? DefaultCurrency { get; set; }

    #endregion
}
