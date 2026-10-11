// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the lightweight Host role runtime authorization bootstrap response.
// ================================================================

namespace skolenza.application.DTOS.Host.Access;

/// <summary>
/// Represents the current Host role module and operation access available to an authenticated Host user.
/// </summary>
public sealed class HostAccessResponseDTO
{
    #region Properties

    /// <summary>
    /// Gets or sets the Host modules that contain at least one currently allowed operation.
    /// </summary>
    public IReadOnlyCollection<HostAccessModuleResponseDTO> Modules { get; init; }
        = Array.Empty<HostAccessModuleResponseDTO>();

    #endregion
}

/// <summary>
/// Represents one Host module and its currently allowed operations.
/// </summary>
public sealed class HostAccessModuleResponseDTO
{
    #region Properties

    /// <summary>
    /// Gets or sets the module identifier.
    /// </summary>
    public int ModuleId { get; init; }

    /// <summary>
    /// Gets or sets the configured module name.
    /// </summary>
    public string ModuleName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the configured display name when one exists.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets or sets the allowed operations for this Host module.
    /// </summary>
    public IReadOnlyCollection<HostAccessOperationResponseDTO> Operations { get; init; }
        = Array.Empty<HostAccessOperationResponseDTO>();

    #endregion
}

/// <summary>
/// Represents one currently allowed Host operation.
/// </summary>
public sealed class HostAccessOperationResponseDTO
{
    #region Properties

    /// <summary>
    /// Gets or sets the operation identifier.
    /// </summary>
    public int OperationId { get; init; }

    /// <summary>
    /// Gets or sets the configured operation name.
    /// </summary>
    public string OperationName { get; init; } = string.Empty;

    #endregion
}
