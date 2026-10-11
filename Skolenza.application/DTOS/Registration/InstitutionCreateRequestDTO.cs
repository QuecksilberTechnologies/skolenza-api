namespace skolenza.application.DTOs.Registration;

/// <summary>
/// Canonical public request contract for institution self-registration.
/// The inherited creation fields keep the established transactional handler
/// and permission pipeline intact while exposing Skolenza terminology.
/// </summary>
public sealed class InstitutionCreateRequestDTO : InstitutionCreationRequestDTO
{
    public string InstitutionName
    {
        get => CompanyName;
        set => CompanyName = value;
    }

    public string StaffCodePrefix
    {
        get => Prefix;
        set => Prefix = value;
    }

    public string InstitutionEmailDomain
    {
        get => CompanyEmailDomain;
        set => CompanyEmailDomain = value;
    }

    /// <summary>Returns the established creation-pipeline request.</summary>
    public InstitutionCreationRequestDTO ToCreationRequest() => this;
}
