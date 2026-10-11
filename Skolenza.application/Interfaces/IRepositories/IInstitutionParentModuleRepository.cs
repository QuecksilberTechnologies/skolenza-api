// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines Institution-enabled Header Module persistence operations for Host-managed Institution Parent Module APIs.
// ================================================================

using skolenza.application.DTOS.Module.InstitutionParentModule;
using skolenza.application.DTOS.Pagination;

namespace skolenza.application.Interfaces.IRepositories;

/// <summary>
/// Defines Institution entitlement persistence operations for Parent and Sub-Parent Header Modules.
/// </summary>
public interface IInstitutionParentModuleRepository
{
    /// <summary>
    /// Retrieves a Institution-entitled Main Parent Header tree with direct entitled Sub-Parent Header children.
    /// </summary>
    /// <param name="institutionId">The decrypted numeric Institution identifier.</param>
    /// <param name="isEnabled">The optional Institution entitlement enabled-state filter.</param>
    /// <param name="cancellationToken">A token used to cancel the query.</param>
    /// <returns>The entitled Header tree with raw numeric Institution identifiers.</returns>
    Task<List<InstitutionParentModuleReadModel>> GetHeaderTreeAsync(
        long institutionId,
        bool? isEnabled,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a server-paged collection of all Institution-entitled Main Parent Headers.
    /// </summary>
    /// <param name="isActive">The optional Institution entitlement enabled-state filter exposed as IsActive.</param>
    /// <param name="pageNumber">The requested one-based page number.</param>
    /// <param name="pageSize">The requested page size.</param>
    /// <param name="cancellationToken">A token used to cancel the query.</param>
    /// <returns>The Main Parent Header page with raw numeric Institution identifiers.</returns>
    Task<PagedResponseDTO<InstitutionParentModuleReadModel>> GetPagedMainParentHeadersAsync(
        bool? isActive,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves one Institution-entitled Header Module by its global Module identifier.
    /// </summary>
    /// <param name="institutionId">The decrypted numeric Institution identifier.</param>
    /// <param name="moduleId">The global Module identifier exposed by the API.</param>
    /// <param name="moduleScope">The requested global Module scope.</param>
    /// <param name="cancellationToken">A token used to cancel the query.</param>
    /// <returns>The entitled Header Module, or <see langword="null"/> when unavailable.</returns>
    Task<InstitutionParentModuleReadModel?> GetHeaderByModuleIdAsync(
        long institutionId,
        int moduleId,
        short moduleScope,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stages a downward Institution entitlement status cascade for a Main Parent or Sub-Parent Header Module and its descendants.
    /// </summary>
    /// <param name="institutionId">The decrypted numeric Institution identifier.</param>
    /// <param name="moduleId">The target global Module identifier.</param>
    /// <param name="isActive">The requested enabled state.</param>
    /// <param name="auditActorId">The Institution entitlement audit actor identifier.</param>
    /// <param name="updatedDateTime">The UTC timestamp for rows whose state changes.</param>
    /// <param name="cancellationToken">A token used to cancel the update staging.</param>
    /// <returns>The target Header Module after staging, or <see langword="null"/> when it is not an entitled Header Module for the Institution.</returns>
    Task<InstitutionParentModuleReadModel?> StageStatusCascadeAsync(
        long institutionId,
        int moduleId,
        bool isActive,
        long auditActorId,
        DateTime updatedDateTime,
        CancellationToken cancellationToken);
}
