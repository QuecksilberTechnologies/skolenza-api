// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the explicit Host-triggered Institution plan-entitlement synchronization contract.
// ================================================================

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOs.Institution;

/// <summary>
/// Requests an additive entitlement snapshot synchronization for one Host-selected Institution.
/// </summary>
/// <remarks>
/// <para><see cref="InstitutionId"/> must be the existing Host-facing encrypted Institution identifier.</para>
/// <para>Super Admin Hosts may submit zero permission identifiers; other Host roles require both identifiers.</para>
/// </remarks>
public sealed class SynchronizeInstitutionPlanEntitlementsRequestDTO : PermissionRequestDTO
{
    /// <summary>
    /// Gets or sets the encrypted Institution identifier selected in the Host UI.
    /// </summary>
    public string InstitutionId { get; set; } = string.Empty;
}

/// <summary>
/// Returns the safe outcome of an additive Institution plan-entitlement synchronization.
/// </summary>
public sealed class SynchronizeInstitutionPlanEntitlementsResponseDTO
{
    /// <summary>Gets or sets the encrypted Institution identifier submitted to the synchronization.</summary>
    public string InstitutionId { get; set; } = string.Empty;

    /// <summary>Gets or sets the active subscription-plan identifier used as the synchronization source.</summary>
    public int SubscriptionPlanId { get; set; }

    /// <summary>Gets or sets the number of active plan modules considered by the synchronization.</summary>
    public int SourceModuleCount { get; set; }

    /// <summary>Gets or sets the number of new InstitutionEnabledModule records added.</summary>
    public int AddedModuleCount { get; set; }

    /// <summary>Gets or sets the number of already-existing eligible Institution module records left untouched.</summary>
    public int ExistingModuleCount { get; set; }

    /// <summary>Gets or sets the number of active module-operation mappings considered by the synchronization.</summary>
    public int SourceOperationCount { get; set; }

    /// <summary>Gets or sets the number of new InstitutionEnabledOperation records added.</summary>
    public int AddedOperationCount { get; set; }

    /// <summary>Gets or sets the number of already-existing eligible Institution operation records left untouched.</summary>
    public int ExistingOperationCount { get; set; }

    /// <summary>Gets or sets the eligible leaf module details read from the Module master.</summary>
    public List<InstitutionPlanEntitlementModuleSyncResponseDTO> Modules { get; set; } = new();

    /// <summary>Gets or sets the eligible operation details read from the ModuleOperationMapping and Operation masters.</summary>
    public List<InstitutionPlanEntitlementOperationSyncResponseDTO> Operations { get; set; } = new();
}

/// <summary>
/// Represents one eligible Institution leaf-module entitlement with safe Module-master details.
/// </summary>
public sealed class InstitutionPlanEntitlementModuleSyncResponseDTO
{
    /// <summary>Gets or sets the master Module identifier.</summary>
    public int ModuleId { get; init; }

    /// <summary>Gets or sets the Module-master name.</summary>
    public string ModuleName { get; init; } = string.Empty;

    /// <summary>Gets or sets the optional Module display name.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Gets or sets the optional parent Module identifier retained from the Module master.</summary>
    public int? ParentModuleId { get; init; }

    /// <summary>Gets or sets whether the Module is a leaf node.</summary>
    public bool? IsLeafNode { get; init; }

    /// <summary>Gets or sets whether the Institution already had this Module entitlement before the request.</summary>
    public bool AlreadyEnabled { get; set; }
}

/// <summary>
/// Represents one eligible Institution operation entitlement with safe mapping and Operation-master details.
/// </summary>
public sealed class InstitutionPlanEntitlementOperationSyncResponseDTO
{
    /// <summary>Gets or sets the Module-master identifier.</summary>
    public int ModuleId { get; init; }

    /// <summary>Gets or sets the Module-master name.</summary>
    public string ModuleName { get; init; } = string.Empty;

    /// <summary>Gets or sets the Operation-master identifier.</summary>
    public int OperationId { get; init; }

    /// <summary>Gets or sets the Operation-master name.</summary>
    public string OperationName { get; init; } = string.Empty;

    /// <summary>Gets or sets whether the active module-operation mapping is operational.</summary>
    public bool? IsOperational { get; init; }

    /// <summary>Gets or sets the optional active module-operation page URL.</summary>
    public string? PageUrl { get; init; }

    /// <summary>Gets or sets whether the Institution already had this operation entitlement before the request.</summary>
    public bool AlreadyEnabled { get; set; }
}

/// <summary>
/// Carries the persistence result before the handler protects the Institution identifier for the API response.
/// </summary>
public sealed class InstitutionPlanEntitlementSyncResult
{
    /// <summary>Gets or sets the active subscription-plan identifier used as the source.</summary>
    public int SubscriptionPlanId { get; init; }

    /// <summary>Gets or sets the number of active plan modules considered.</summary>
    public int SourceModuleCount { get; init; }

    /// <summary>Gets or sets the number of newly staged Institution module entitlements.</summary>
    public int AddedModuleCount { get; init; }

    /// <summary>Gets or sets the number of existing module entitlements retained unchanged.</summary>
    public int ExistingModuleCount { get; init; }

    /// <summary>Gets or sets the number of active module-operation mappings considered.</summary>
    public int SourceOperationCount { get; init; }

    /// <summary>Gets or sets the number of newly staged Institution operation entitlements.</summary>
    public int AddedOperationCount { get; init; }

    /// <summary>Gets or sets the number of existing operation entitlements retained unchanged.</summary>
    public int ExistingOperationCount { get; init; }

    /// <summary>Gets or sets the safe eligible Module-master details.</summary>
    public IReadOnlyList<InstitutionPlanEntitlementModuleSyncResponseDTO> Modules { get; init; } = Array.Empty<InstitutionPlanEntitlementModuleSyncResponseDTO>();

    /// <summary>Gets or sets the safe eligible mapping and Operation-master details.</summary>
    public IReadOnlyList<InstitutionPlanEntitlementOperationSyncResponseDTO> Operations { get; init; } = Array.Empty<InstitutionPlanEntitlementOperationSyncResponseDTO>();
}
