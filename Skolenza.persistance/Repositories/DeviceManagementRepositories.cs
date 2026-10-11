// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Provides Host-managed PostgreSQL persistence for DeviceMaster and InstitutionDevice records.
// ================================================================

using skolenza.application.DTOS.Host;
using skolenza.application.DTOS.Pagination;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Repositories;

/// <summary>Provides shared paging behavior for Host device repositories.</summary>
public abstract class DeviceManagementRepositoryBase(SkolenzaDbContext context)
{
    protected SkolenzaDbContext Context { get; } = context;

    protected static (int PageNumber, int PageSize) NormalizePage(int pageNumber, int pageSize) =>
        (pageNumber > 0 ? pageNumber : 1, pageSize is > 0 and <= 100 ? pageSize : 10);

    protected static PagedResponseDTO<T> CreatePage<T>(List<T> data, int totalCount, int pageNumber, int pageSize) =>
        new(data, totalCount, pageNumber, pageSize) { TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize) };
}

/// <summary>Provides DeviceMaster catalog persistence and physical-device dependency checks.</summary>
public sealed class DeviceMasterRepository(SkolenzaDbContext context) : DeviceManagementRepositoryBase(context), IDeviceMasterRepository
{
    #region Device Master Queries

    /// <inheritdoc />
    public Task<DeviceMaster?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        Context.DeviceMasters.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public Task<DeviceMaster?> GetBySNoAsync(string sNo, CancellationToken cancellationToken) =>
        Context.DeviceMasters.AsNoTracking().FirstOrDefaultAsync(x => !x.IsSoftDeleted && x.SNo.ToLower() == sNo.Trim().ToLower(), cancellationToken);

    /// <inheritdoc />
    public Task<DeviceMaster?> GetForUpdateAsync(long id, CancellationToken cancellationToken) =>
        Context.DeviceMasters.FirstOrDefaultAsync(x => x.Id == id && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public async Task<PagedResponseDTO<DeviceMaster>> GetPagedAsync(GetDeviceMasterListRequestDTO filter, CancellationToken cancellationToken)
    {
        var (pageNumber, pageSize) = NormalizePage(filter.PageNumber, filter.PageSize);
        var query = Context.DeviceMasters.AsNoTracking().Where(x => !x.IsSoftDeleted);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = $"%{filter.Search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.SNo, term) || EF.Functions.ILike(x.DeviceCode, term) || EF.Functions.ILike(x.DeviceName, term) || EF.Functions.ILike(x.ModelNo, term) || EF.Functions.ILike(x.CompanyName, term) || (x.BrandName != null && EF.Functions.ILike(x.BrandName, term)) || (x.Description != null && EF.Functions.ILike(x.Description, term)));
        }
        if (filter.DeviceType.HasValue) query = query.Where(x => x.DeviceType == (short)filter.DeviceType.Value);
        if (filter.IsActive.HasValue) query = query.Where(x => x.IsActive == filter.IsActive.Value);
        if (filter.IsOccupied.HasValue) query = query.Where(x => x.IsOccupied == filter.IsOccupied.Value);
        var totalCount = await query.CountAsync(cancellationToken);
        var data = await query.OrderByDescending(x => x.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return CreatePage(data, totalCount, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public Task<bool> DuplicateExistsAsync(string deviceCode, string companyName, string modelNo, long? excludeId, CancellationToken cancellationToken) =>
        Context.DeviceMasters.AnyAsync(x => !x.IsSoftDeleted && (!excludeId.HasValue || x.Id != excludeId.Value) && (x.DeviceCode.ToLower() == deviceCode.ToLower() || (x.CompanyName.ToLower() == companyName.ToLower() && x.ModelNo.ToLower() == modelNo.ToLower())), cancellationToken);

    /// <inheritdoc />
    public Task<bool> HasActiveInstitutionDevicesAsync(long deviceMasterId, CancellationToken cancellationToken) =>
        Context.InstitutionDevices.AnyAsync(x => x.DeviceMasterId == deviceMasterId && x.IsActive && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public Task<bool> HasInstitutionDevicesAsync(long deviceMasterId, CancellationToken cancellationToken) =>
        Context.InstitutionDevices.AnyAsync(x => x.DeviceMasterId == deviceMasterId && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public Task<bool> HasInstitutionDeviceConfigurationsAsync(long deviceMasterId, CancellationToken cancellationToken) =>
        Context.InstitutionDeviceConfigurations.AnyAsync(x => x.InstitutionDevice.DeviceMasterId == deviceMasterId, cancellationToken);

    #endregion

    #region Device Master Commands

    /// <inheritdoc />
    public Task AddAsync(DeviceMaster entity, CancellationToken cancellationToken) => Context.DeviceMasters.AddAsync(entity, cancellationToken).AsTask();

    #endregion
}

/// <summary>Provides Host-managed physical InstitutionDevice persistence.</summary>
public sealed class InstitutionDeviceRepository(SkolenzaDbContext context) : DeviceManagementRepositoryBase(context), IInstitutionDeviceRepository
{
    #region Institution Device Queries

    /// <inheritdoc />
    public Task<InstitutionDevice?> GetByIdAsync(long institutionId, long id, CancellationToken cancellationToken) =>
        DeviceQuery().FirstOrDefaultAsync(x => x.Id == id && x.InstitutionId == institutionId && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public Task<InstitutionDevice?> GetForUpdateAsync(long institutionId, long id, CancellationToken cancellationToken) =>
        Context.InstitutionDevices.FirstOrDefaultAsync(x => x.Id == id && x.InstitutionId == institutionId && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public async Task<PagedResponseDTO<InstitutionDevice>> GetPagedAsync(long institutionId, GetInstitutionDeviceListRequestDTO filter, CancellationToken cancellationToken)
    {
        var (pageNumber, pageSize) = NormalizePage(filter.PageNumber, filter.PageSize);
        var query = DeviceQuery().Where(x => x.InstitutionId == institutionId && !x.IsSoftDeleted);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = $"%{filter.Search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.DeviceCode, term) || (x.DeviceName != null && EF.Functions.ILike(x.DeviceName, term)) || EF.Functions.ILike(x.Institution.CompanyName, term) || EF.Functions.ILike(x.Campus.LocationName, term) || EF.Functions.ILike(x.DeviceMaster.SNo, term));
        }
        if (filter.CampusId.HasValue) query = query.Where(x => x.CampusId == filter.CampusId.Value);
        if (filter.DeviceMasterId.HasValue) query = query.Where(x => x.DeviceMasterId == filter.DeviceMasterId.Value);
        if (filter.IsAttendanceDevice.HasValue) query = query.Where(x => x.IsAttendanceDevice == filter.IsAttendanceDevice.Value);
        if (filter.IsActive.HasValue) query = query.Where(x => x.IsActive == filter.IsActive.Value);
        var totalCount = await query.CountAsync(cancellationToken);
        var data = await query.OrderByDescending(x => x.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return CreatePage(data, totalCount, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public async Task<PagedResponseDTO<InstitutionDevice>> GetHostPagedAsync(GetInstitutionDeviceListRequestDTO filter, CancellationToken cancellationToken)
    {
        var (pageNumber, pageSize) = NormalizePage(filter.PageNumber, filter.PageSize);
        var query = DeviceQuery().Where(x => !x.IsSoftDeleted);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = $"%{filter.Search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.DeviceCode, term) || (x.DeviceName != null && EF.Functions.ILike(x.DeviceName, term)) || EF.Functions.ILike(x.Institution.CompanyName, term) || EF.Functions.ILike(x.Campus.LocationName, term) || EF.Functions.ILike(x.DeviceMaster.SNo, term));
        }
        if (filter.CampusId.HasValue) query = query.Where(x => x.CampusId == filter.CampusId.Value);
        if (filter.DeviceMasterId.HasValue) query = query.Where(x => x.DeviceMasterId == filter.DeviceMasterId.Value);
        if (filter.IsAttendanceDevice.HasValue) query = query.Where(x => x.IsAttendanceDevice == filter.IsAttendanceDevice.Value);
        if (filter.IsActive.HasValue) query = query.Where(x => x.IsActive == filter.IsActive.Value);
        var totalCount = await query.CountAsync(cancellationToken);
        var data = await query.OrderByDescending(x => x.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return CreatePage(data, totalCount, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public Task<bool> IsEligibleInstitutionAsync(long institutionId, CancellationToken cancellationToken) =>
        Context.Institutions.AnyAsync(x => x.Id == institutionId && x.IsActive && x.IsSoftDeleted != true, cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsActiveCampusAsync(long campusId, CancellationToken cancellationToken) =>
        Context.Campuss.AnyAsync(x => x.Id == campusId && x.IsActive && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public Task<bool> CampusBelongsToInstitutionAsync(long institutionId, long campusId, CancellationToken cancellationToken) =>
        Context.Campuss.AnyAsync(x => x.Id == campusId && x.InstitutionId == institutionId && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsEligibleDeviceMasterAsync(long deviceMasterId, CancellationToken cancellationToken) =>
        Context.DeviceMasters.AnyAsync(x => x.Id == deviceMasterId && x.IsActive && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public Task<bool> DeviceCodeExistsAsync(long institutionId, string deviceCode, long? excludeId, CancellationToken cancellationToken) =>
        Context.InstitutionDevices.AnyAsync(x => !x.IsSoftDeleted && x.InstitutionId == institutionId && x.DeviceCode.ToLower() == deviceCode.Trim().ToLower() && (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken);

    /// <inheritdoc />
    public Task<bool> HasConfigurationAsync(long institutionId, long institutionDeviceId, CancellationToken cancellationToken) =>
        Context.InstitutionDeviceConfigurations.AnyAsync(x => x.InstitutionDeviceId == institutionDeviceId && x.InstitutionDevice.InstitutionId == institutionId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> HasActiveEnrollmentsAsync(long institutionDeviceId, CancellationToken cancellationToken) =>
        Context.StaffMemberDeviceEnrollments.AnyAsync(x => x.InstitutionDeviceId == institutionDeviceId && x.IsActive && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public Task<bool> HasEnrollmentsAsync(long institutionDeviceId, CancellationToken cancellationToken) =>
        Context.StaffMemberDeviceEnrollments.AnyAsync(x => x.InstitutionDeviceId == institutionDeviceId && !x.IsSoftDeleted, cancellationToken);

    #endregion

    #region Institution Device Commands

    /// <inheritdoc />
    public Task AddAsync(InstitutionDevice entity, CancellationToken cancellationToken) => Context.InstitutionDevices.AddAsync(entity, cancellationToken).AsTask();

    #endregion

    private IQueryable<InstitutionDevice> DeviceQuery() => Context.InstitutionDevices.AsNoTracking().Include(x => x.Institution).Include(x => x.Campus).Include(x => x.DeviceMaster).Include(x => x.InstitutionDeviceConfiguration);
}

/// <summary>Provides InstitutionDeviceConfiguration persistence with Institution-scoped parent-device validation.</summary>
public sealed class InstitutionDeviceConfigurationRepository(SkolenzaDbContext context) : DeviceManagementRepositoryBase(context), IInstitutionDeviceConfigurationRepository
{
    /// <inheritdoc />
    public Task<InstitutionDeviceConfiguration?> GetByIdAsync(long institutionId, long id, CancellationToken cancellationToken) =>
        ConfigurationQuery().FirstOrDefaultAsync(x => x.Id == id && x.InstitutionDevice.InstitutionId == institutionId && !x.InstitutionDevice.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public Task<InstitutionDeviceConfiguration?> GetForUpdateAsync(long institutionId, long id, CancellationToken cancellationToken) =>
        Context.InstitutionDeviceConfigurations.Include(x => x.InstitutionDevice).FirstOrDefaultAsync(x => x.Id == id && x.InstitutionDevice.InstitutionId == institutionId && !x.InstitutionDevice.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public Task<InstitutionDeviceConfiguration?> GetForUpdateByInstitutionDeviceAsync(long institutionId, long institutionDeviceId, CancellationToken cancellationToken) =>
        Context.InstitutionDeviceConfigurations.Include(x => x.InstitutionDevice).FirstOrDefaultAsync(
            x => x.InstitutionDeviceId == institutionDeviceId && x.InstitutionDevice.InstitutionId == institutionId && !x.InstitutionDevice.IsSoftDeleted,
            cancellationToken);

    /// <inheritdoc />
    public async Task<PagedResponseDTO<InstitutionDeviceConfiguration>> GetPagedAsync(long institutionId, GetInstitutionDeviceConfigurationListRequestDTO filter, CancellationToken cancellationToken)
    {
        var (pageNumber, pageSize) = NormalizePage(filter.PageNumber, filter.PageSize);
        var query = ConfigurationQuery().Where(x => x.InstitutionDevice.InstitutionId == institutionId && !x.InstitutionDevice.IsSoftDeleted);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = $"%{filter.Search.Trim()}%";
            query = query.Where(x => (x.IpAddress != null && EF.Functions.ILike(x.IpAddress, term)) || (x.MacAddress != null && EF.Functions.ILike(x.MacAddress, term)) || (x.ServerHost != null && EF.Functions.ILike(x.ServerHost, term)) || (x.ServerUrl != null && EF.Functions.ILike(x.ServerUrl, term)) || EF.Functions.ILike(x.InstitutionDevice.DeviceCode, term) || (x.InstitutionDevice.DeviceName != null && EF.Functions.ILike(x.InstitutionDevice.DeviceName, term)));
        }
        if (filter.ResolvedInstitutionDeviceId.HasValue) query = query.Where(x => x.InstitutionDeviceId == filter.ResolvedInstitutionDeviceId.Value);
        if (filter.CommandTransport.HasValue)
        {
            query = query.Where(x => (x.CommandTransport ?? x.MqttTransport) == (short)filter.CommandTransport.Value);
        }
        else if (filter.MqttTransport.HasValue)
        {
            query = query.Where(x => (x.CommandTransport ?? x.MqttTransport) == (short)filter.MqttTransport.Value);
        }
        if (filter.IsEnrollmentEnabled.HasValue) query = query.Where(x => x.IsEnrollmentEnabled == filter.IsEnrollmentEnabled.Value);
        var totalCount = await query.CountAsync(cancellationToken);
        var data = await query.OrderByDescending(x => x.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return CreatePage(data, totalCount, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public async Task<PagedResponseDTO<InstitutionDeviceConfiguration>> GetHostPagedAsync(GetInstitutionDeviceConfigurationListRequestDTO filter, CancellationToken cancellationToken)
    {
        var (pageNumber, pageSize) = NormalizePage(filter.PageNumber, filter.PageSize);
        var query = ConfigurationQuery().Where(x => !x.InstitutionDevice.IsSoftDeleted);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = $"%{filter.Search.Trim()}%";
            query = query.Where(x => (x.IpAddress != null && EF.Functions.ILike(x.IpAddress, term)) || (x.MacAddress != null && EF.Functions.ILike(x.MacAddress, term)) || (x.ServerHost != null && EF.Functions.ILike(x.ServerHost, term)) || (x.ServerUrl != null && EF.Functions.ILike(x.ServerUrl, term)) || EF.Functions.ILike(x.InstitutionDevice.DeviceCode, term) || (x.InstitutionDevice.DeviceName != null && EF.Functions.ILike(x.InstitutionDevice.DeviceName, term)));
        }
        if (filter.ResolvedInstitutionDeviceId.HasValue) query = query.Where(x => x.InstitutionDeviceId == filter.ResolvedInstitutionDeviceId.Value);
        if (filter.CommandTransport.HasValue)
        {
            query = query.Where(x => (x.CommandTransport ?? x.MqttTransport) == (short)filter.CommandTransport.Value);
        }
        else if (filter.MqttTransport.HasValue)
        {
            query = query.Where(x => (x.CommandTransport ?? x.MqttTransport) == (short)filter.MqttTransport.Value);
        }
        if (filter.IsEnrollmentEnabled.HasValue) query = query.Where(x => x.IsEnrollmentEnabled == filter.IsEnrollmentEnabled.Value);
        var totalCount = await query.CountAsync(cancellationToken);
        var data = await query.OrderByDescending(x => x.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return CreatePage(data, totalCount, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public Task<bool> IsEligibleInstitutionDeviceAsync(long institutionId, long institutionDeviceId, CancellationToken cancellationToken) =>
        Context.InstitutionDevices.AnyAsync(x => x.Id == institutionDeviceId && x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsForInstitutionDeviceAsync(long institutionId, long institutionDeviceId, long? excludeId, CancellationToken cancellationToken) =>
        Context.InstitutionDeviceConfigurations.AnyAsync(x => x.InstitutionDeviceId == institutionDeviceId && x.InstitutionDevice.InstitutionId == institutionId && (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken);

    /// <inheritdoc />
    public Task AddAsync(InstitutionDeviceConfiguration entity, CancellationToken cancellationToken) => Context.InstitutionDeviceConfigurations.AddAsync(entity, cancellationToken).AsTask();

    /// <inheritdoc />
    public void Remove(InstitutionDeviceConfiguration entity) => Context.InstitutionDeviceConfigurations.Remove(entity);

    private IQueryable<InstitutionDeviceConfiguration> ConfigurationQuery() => Context.InstitutionDeviceConfigurations.AsNoTracking().Include(x => x.InstitutionDevice).ThenInclude(x => x.DeviceMaster);
}
