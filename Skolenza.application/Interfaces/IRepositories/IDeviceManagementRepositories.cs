// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines Host-managed DeviceMaster and InstitutionDevice persistence contracts.
// ================================================================

using skolenza.application.DTOS.Host;
using skolenza.application.DTOS.Pagination;
using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.IRepositories;

/// <summary>Defines persistence and dependency queries for the global DeviceMaster catalog.</summary>
public interface IDeviceMasterRepository
{
    /// <summary>Gets a non-soft-deleted device model by identifier.</summary>
    Task<DeviceMaster?> GetByIdAsync(long id, CancellationToken cancellationToken);
    /// <summary>Gets a non-soft-deleted device model by its manufacturer serial number.</summary>
    Task<DeviceMaster?> GetBySNoAsync(string sNo, CancellationToken cancellationToken);
    /// <summary>Gets a tracked non-soft-deleted device model for an administrative update.</summary>
    Task<DeviceMaster?> GetForUpdateAsync(long id, CancellationToken cancellationToken);
    /// <summary>Gets a database-paged device model list.</summary>
    Task<PagedResponseDTO<DeviceMaster>> GetPagedAsync(GetDeviceMasterListRequestDTO filter, CancellationToken cancellationToken);
    /// <summary>Determines whether either live database business key is already used.</summary>
    Task<bool> DuplicateExistsAsync(string deviceCode, string companyName, string modelNo, long? excludeId, CancellationToken cancellationToken);
    /// <summary>Determines whether a live physical device blocks catalog deactivation.</summary>
    Task<bool> HasActiveInstitutionDevicesAsync(long deviceMasterId, CancellationToken cancellationToken);
    /// <summary>Determines whether any non-soft-deleted physical device blocks catalog deletion.</summary>
    Task<bool> HasInstitutionDevicesAsync(long deviceMasterId, CancellationToken cancellationToken);
    /// <summary>Determines whether any connection configuration references a Institution device using this catalog device.</summary>
    Task<bool> HasInstitutionDeviceConfigurationsAsync(long deviceMasterId, CancellationToken cancellationToken);
    /// <summary>Adds a prepared Host-owned device model.</summary>
    Task AddAsync(DeviceMaster entity, CancellationToken cancellationToken);
}

/// <summary>Defines Host-managed physical InstitutionDevice persistence and validation queries.</summary>
public interface IInstitutionDeviceRepository
{
    /// <summary>Gets a non-soft-deleted physical device with display context scoped to its Institution.</summary>
    Task<InstitutionDevice?> GetByIdAsync(long institutionId, long id, CancellationToken cancellationToken);
    /// <summary>Gets a tracked non-soft-deleted physical device for a Institution-scoped update.</summary>
    Task<InstitutionDevice?> GetForUpdateAsync(long institutionId, long id, CancellationToken cancellationToken);
    /// <summary>Gets a database-paged physical-device list scoped to its Institution.</summary>
    Task<PagedResponseDTO<InstitutionDevice>> GetPagedAsync(long institutionId, GetInstitutionDeviceListRequestDTO filter, CancellationToken cancellationToken);
    /// <summary>Gets a database-paged physical-device list across all live Institution devices for Host Admin use.</summary>
    Task<PagedResponseDTO<InstitutionDevice>> GetHostPagedAsync(GetInstitutionDeviceListRequestDTO filter, CancellationToken cancellationToken);
    /// <summary>Determines whether a Institution is active and non-soft-deleted.</summary>
    Task<bool> IsEligibleInstitutionAsync(long institutionId, CancellationToken cancellationToken);
    /// <summary>Determines whether a location is active and non-soft-deleted regardless of Institution ownership.</summary>
    Task<bool> IsActiveCampusAsync(long campusId, CancellationToken cancellationToken);
    /// <summary>Determines whether a location belongs to the selected Institution.</summary>
    Task<bool> CampusBelongsToInstitutionAsync(long institutionId, long campusId, CancellationToken cancellationToken);
    /// <summary>Determines whether a device master is active and non-soft-deleted.</summary>
    Task<bool> IsEligibleDeviceMasterAsync(long deviceMasterId, CancellationToken cancellationToken);
    /// <summary>Determines whether the device code is used by another live device of the selected Institution.</summary>
    Task<bool> DeviceCodeExistsAsync(long institutionId, string deviceCode, long? excludeId, CancellationToken cancellationToken);
    /// <summary>Determines whether a connection configuration is registered for the Institution device.</summary>
    Task<bool> HasConfigurationAsync(long institutionId, long institutionDeviceId, CancellationToken cancellationToken);
    /// <summary>Determines whether active staffMember enrollment blocks deactivation.</summary>
    Task<bool> HasActiveEnrollmentsAsync(long institutionDeviceId, CancellationToken cancellationToken);
    /// <summary>Determines whether any live staffMember enrollment blocks soft deletion.</summary>
    Task<bool> HasEnrollmentsAsync(long institutionDeviceId, CancellationToken cancellationToken);
    /// <summary>Adds a prepared Host-owned physical device.</summary>
    Task AddAsync(InstitutionDevice entity, CancellationToken cancellationToken);
}

/// <summary>Defines persistence and validation queries for one Institution device connection configuration.</summary>
public interface IInstitutionDeviceConfigurationRepository
{
    /// <summary>Gets a configuration with its Institution device context scoped to the authenticated Institution.</summary>
    Task<InstitutionDeviceConfiguration?> GetByIdAsync(long institutionId, long id, CancellationToken cancellationToken);
    /// <summary>Gets a tracked configuration scoped to the authenticated Institution.</summary>
    Task<InstitutionDeviceConfiguration?> GetForUpdateAsync(long institutionId, long id, CancellationToken cancellationToken);
    /// <summary>Gets a tracked configuration by its physical Institution device, scoped to the authenticated Institution.</summary>
    Task<InstitutionDeviceConfiguration?> GetForUpdateByInstitutionDeviceAsync(long institutionId, long institutionDeviceId, CancellationToken cancellationToken);
    /// <summary>Gets a database-paged configuration list scoped to the authenticated Institution.</summary>
    Task<PagedResponseDTO<InstitutionDeviceConfiguration>> GetPagedAsync(long institutionId, GetInstitutionDeviceConfigurationListRequestDTO filter, CancellationToken cancellationToken);
    /// <summary>Gets a database-paged configuration list across all live Institution devices for Host Admin use.</summary>
    Task<PagedResponseDTO<InstitutionDeviceConfiguration>> GetHostPagedAsync(GetInstitutionDeviceConfigurationListRequestDTO filter, CancellationToken cancellationToken);
    /// <summary>Determines whether an active Institution device can be configured.</summary>
    Task<bool> IsEligibleInstitutionDeviceAsync(long institutionId, long institutionDeviceId, CancellationToken cancellationToken);
    /// <summary>Determines whether the Institution device already has a configuration.</summary>
    Task<bool> ExistsForInstitutionDeviceAsync(long institutionId, long institutionDeviceId, long? excludeId, CancellationToken cancellationToken);
    /// <summary>Adds a prepared Institution device configuration.</summary>
    Task AddAsync(InstitutionDeviceConfiguration entity, CancellationToken cancellationToken);
    /// <summary>Hard deletes a tracked Institution device configuration.</summary>
    void Remove(InstitutionDeviceConfiguration entity);
}
