// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Provides Institution-isolated EF Core persistence for InstitutionConfiguration records.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Host;
using skolenza.application.DTOS.InstitutionConfiguration;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Repositories;

/// <summary>Provides shared database paging behavior for InstitutionConfiguration repositories.</summary>
public abstract class InstitutionConfigurationRepositoryBase
{
    protected InstitutionConfigurationRepositoryBase(SkolenzaDbContext context) => Context = context;
    protected SkolenzaDbContext Context { get; }

    protected static (int PageNumber, int PageSize) NormalizePage(int pageNumber, int pageSize) =>
        (pageNumber > 0 ? pageNumber : 1, pageSize > 0 ? pageSize : 10);

    protected static PagedResponseDTO<T> CreatePage<T>(List<T> data, int count, int pageNumber, int pageSize) =>
        new(data, count, pageNumber, pageSize)
        {
            TotalPages = (int)Math.Ceiling(count / (double)pageSize)
        };
}

/// <summary>Provides Institution-location persistence and dependency queries.</summary>
public sealed class CampusRepository : InstitutionConfigurationRepositoryBase, ICampusRepository
{
    /// <summary>Initializes a new repository over the shared workforce context.</summary>
    public CampusRepository(SkolenzaDbContext context) : base(context) { }

    #region Institution Location Queries

    /// <inheritdoc />
    public Task<Campus?> GetByIdAsync(long institutionId, long id, CancellationToken cancellationToken) =>
        Context.Campuss.AsNoTracking().Include(x => x.Country).Include(x => x.District).Include(x => x.Locality)
            .FirstOrDefaultAsync(x => x.Id == id && x.InstitutionId == institutionId && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public Task<Campus?> GetForUpdateAsync(long institutionId, long id, CancellationToken cancellationToken) =>
        Context.Campuss.FirstOrDefaultAsync(x => x.Id == id && x.InstitutionId == institutionId && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public Task<Campus?> GetInitialForUpdateAsync(long institutionId, CancellationToken cancellationToken) =>
        Context.Campuss
            .Where(x => x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted)
            .OrderByDescending(x => x.IsHeadOffice)
            .ThenBy(x => x.AddedDateTime)
            .ThenBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<PagedResponseDTO<Campus>> GetPagedAsync(long institutionId, CampusFilterRequestDTO filter, CancellationToken cancellationToken)
    {
        var (pageNumber, pageSize) = NormalizePage(filter.PageNumber, filter.PageSize);
        var query = Context.Campuss.AsNoTracking().Include(x => x.Country).Include(x => x.District).Include(x => x.Locality)
            .Where(x => x.InstitutionId == institutionId && !x.IsSoftDeleted);
        if (!string.IsNullOrWhiteSpace(filter.Search)) { var term = $"%{filter.Search.Trim()}%"; query = query.Where(x => EF.Functions.ILike(x.LocationCode, term) || EF.Functions.ILike(x.LocationName, term) || (x.Address != null && EF.Functions.ILike(x.Address, term))); }
        if (filter.CountryId.HasValue) query = query.Where(x => x.CountryId == filter.CountryId.Value);
        if (filter.StateId.HasValue) query = query.Where(x => x.StateId == filter.StateId.Value);
        if (filter.DistrictId.HasValue) query = query.Where(x => x.DistrictId == filter.DistrictId.Value);
        if (filter.LocalityId.HasValue) query = query.Where(x => x.LocalityId == filter.LocalityId.Value);
        if (filter.LocationType.HasValue) query = query.Where(x => x.LocationType == (short)filter.LocationType.Value);
        if (filter.IsActive.HasValue) query = query.Where(x => x.IsActive == filter.IsActive.Value);
        var count = await query.CountAsync(cancellationToken);
        var data = await query.OrderBy(x => x.LocationName).ThenBy(x => x.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return CreatePage(data, count, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public async Task<PagedResponseDTO<Campus>> GetHostPagedAsync(CampusFilterRequestDTO filter, CancellationToken cancellationToken)
    {
        var (pageNumber, pageSize) = NormalizePage(filter.PageNumber, filter.PageSize);
        var query = Context.Campuss.AsNoTracking().Include(x => x.Country).Include(x => x.District).Include(x => x.Locality)
            .Where(x => !x.IsSoftDeleted);
        if (!string.IsNullOrWhiteSpace(filter.Search)) { var term = $"%{filter.Search.Trim()}%"; query = query.Where(x => EF.Functions.ILike(x.LocationCode, term) || EF.Functions.ILike(x.LocationName, term) || (x.Address != null && EF.Functions.ILike(x.Address, term))); }
        if (filter.CountryId.HasValue) query = query.Where(x => x.CountryId == filter.CountryId.Value);
        if (filter.StateId.HasValue) query = query.Where(x => x.StateId == filter.StateId.Value);
        if (filter.DistrictId.HasValue) query = query.Where(x => x.DistrictId == filter.DistrictId.Value);
        if (filter.LocalityId.HasValue) query = query.Where(x => x.LocalityId == filter.LocalityId.Value);
        if (filter.LocationType.HasValue) query = query.Where(x => x.LocationType == (short)filter.LocationType.Value);
        if (filter.IsActive.HasValue) query = query.Where(x => x.IsActive == filter.IsActive.Value);
        var count = await query.CountAsync(cancellationToken);
        var data = await query.OrderBy(x => x.LocationName).ThenBy(x => x.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return CreatePage(data, count, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public Task<bool> LocationCodeExistsAsync(long institutionId, string locationCode, long? excludeId, CancellationToken cancellationToken) =>
        Context.Campuss.AnyAsync(x => x.InstitutionId == institutionId && !x.IsSoftDeleted && x.LocationCode.ToLower() == locationCode.ToLower() && (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken);

    /// <inheritdoc />
    public async Task<bool> IsValidGeographyAsync(int countryId, int? stateId, int? districtId, int? localityId, CancellationToken cancellationToken)
    {
        var countryExists = await Context.Countries.AnyAsync(x => x.Id == countryId && x.IsActive == true, cancellationToken);
        if (!countryExists) return false;
        if (stateId.HasValue && !await Context.States.AnyAsync(x => x.Id == stateId && x.CountryId == countryId && x.IsActive == true, cancellationToken)) return false;
        if (districtId.HasValue && !await Context.Districts.AnyAsync(
                x => x.Id == districtId && x.IsActive && (!stateId.HasValue || x.StateId == stateId.Value),
                cancellationToken)) return false;
        return !localityId.HasValue || await Context.Localities.AnyAsync(
            x => x.Id == localityId && x.IsActive == true
                && (!stateId.HasValue || x.StateId == stateId.Value)
                && (!districtId.HasValue || x.DistrictId == districtId.Value),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasLiveActiveDependenciesAsync(long institutionId, long locationId, CancellationToken cancellationToken) =>
        await Context.InstitutionDevices.AnyAsync(x => x.InstitutionId == institutionId && x.CampusId == locationId && x.IsActive && !x.IsSoftDeleted, cancellationToken)
        || await Context.StaffMemberLocationAssignments.AnyAsync(x => x.InstitutionId == institutionId && x.CampusId == locationId && x.IsActive && !x.IsSoftDeleted, cancellationToken)
        || await Context.StaffMemberWorkArrangements.AnyAsync(x => x.InstitutionId == institutionId && x.PrimaryCampusId == locationId && x.IsActive && !x.IsSoftDeleted, cancellationToken)
        || await Context.StaffMemberWorkPatterns.AnyAsync(x => x.InstitutionId == institutionId && x.CampusId == locationId && x.IsActive && !x.IsSoftDeleted, cancellationToken)
        || await Context.StaffMemberWorkModeOverrideRequests.AnyAsync(x => x.InstitutionId == institutionId && x.CampusId == locationId && x.IsActive && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> HasAnyDependenciesAsync(long institutionId, long locationId, CancellationToken cancellationToken) =>
        await Context.InstitutionDevices.AnyAsync(x => x.InstitutionId == institutionId && x.CampusId == locationId && !x.IsSoftDeleted, cancellationToken)
        || await Context.StaffMemberLocationAssignments.AnyAsync(x => x.InstitutionId == institutionId && x.CampusId == locationId && !x.IsSoftDeleted, cancellationToken)
        || await Context.StaffMemberWorkArrangements.AnyAsync(x => x.InstitutionId == institutionId && x.PrimaryCampusId == locationId && !x.IsSoftDeleted, cancellationToken)
        || await Context.StaffMemberWorkPatterns.AnyAsync(x => x.InstitutionId == institutionId && x.CampusId == locationId && !x.IsSoftDeleted, cancellationToken)
        || await Context.StaffMemberWorkModeOverrideRequests.AnyAsync(x => x.InstitutionId == institutionId && x.CampusId == locationId && !x.IsSoftDeleted, cancellationToken);

    #endregion

    #region Institution Location Commands

    /// <inheritdoc />
    public Task AddAsync(Campus entity, CancellationToken cancellationToken) => Context.Campuss.AddAsync(entity, cancellationToken).AsTask();

    #endregion
}

/// <summary>Provides AttendancePolicy persistence and lifecycle queries.</summary>
public sealed class AttendancePolicyRepository : InstitutionConfigurationRepositoryBase, IAttendancePolicyRepository
{
    /// <summary>Initializes a new repository over the shared workforce context.</summary>
    public AttendancePolicyRepository(SkolenzaDbContext context) : base(context) { }

    #region Attendance Policy Queries

    /// <inheritdoc />
    public Task<AttendancePolicy?> GetByIdAsync(long institutionId, int id, CancellationToken cancellationToken) =>
        Context.AttendancePolicies.AsNoTracking().Include(x => x.PolicyType).FirstOrDefaultAsync(x => x.Id == id && x.InstitutionId == institutionId && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public Task<AttendancePolicy?> GetForUpdateAsync(long institutionId, int id, CancellationToken cancellationToken) =>
        Context.AttendancePolicies.FirstOrDefaultAsync(x => x.Id == id && x.InstitutionId == institutionId && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public async Task<PagedResponseDTO<AttendancePolicy>> GetPagedAsync(long institutionId, AttendancePolicyFilterRequestDTO filter, CancellationToken cancellationToken)
    {
        var (pageNumber, pageSize) = NormalizePage(filter.PageNumber, filter.PageSize);
        var query = Context.AttendancePolicies.AsNoTracking().Include(x => x.PolicyType).Where(x => x.InstitutionId == institutionId && !x.IsSoftDeleted);
        if (!string.IsNullOrWhiteSpace(filter.Search)) { var term = $"%{filter.Search.Trim()}%"; query = query.Where(x => EF.Functions.ILike(x.PolicyName, term)); }
        if (filter.PolicyTypeId.HasValue) query = query.Where(x => x.PolicyTypeId == filter.PolicyTypeId.Value);
        if (filter.IsActive.HasValue) query = query.Where(x => x.IsActive == filter.IsActive.Value);
        var count = await query.CountAsync(cancellationToken);
        var data = await query.OrderBy(x => x.PolicyName).ThenBy(x => x.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return CreatePage(data, count, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public Task<bool> PolicyNameExistsAsync(long institutionId, string policyName, int? excludeId, CancellationToken cancellationToken) =>
        Context.AttendancePolicies.AnyAsync(x => x.InstitutionId == institutionId && !x.IsSoftDeleted && x.PolicyName.ToLower() == policyName.ToLower() && (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsEligiblePolicyTypeAsync(long institutionId, int policyTypeId, CancellationToken cancellationToken) =>
        Context.PolicyTypes.AnyAsync(x => x.Id == policyTypeId && x.InstitutionId == institutionId && x.IsActive == true && x.IsSoftDelete != true, cancellationToken);

    /// <inheritdoc />
    public Task<bool> HasActiveWorkArrangementsAsync(long institutionId, int policyId, CancellationToken cancellationToken) =>
        Context.StaffMemberWorkArrangements.AnyAsync(x => x.InstitutionId == institutionId && x.AttendancePolicyId == policyId && x.IsActive && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public Task<bool> HasAnyWorkArrangementsAsync(long institutionId, int policyId, CancellationToken cancellationToken) =>
        Context.StaffMemberWorkArrangements.AnyAsync(x => x.InstitutionId == institutionId && x.AttendancePolicyId == policyId && !x.IsSoftDeleted, cancellationToken);

    #endregion

    #region Attendance Policy Commands

    /// <inheritdoc />
    public Task AddAsync(AttendancePolicy entity, CancellationToken cancellationToken) => Context.AttendancePolicies.AddAsync(entity, cancellationToken).AsTask();

    #endregion
}

/// <summary>Provides staffMember-location-assignment persistence and validation queries.</summary>
public sealed class StaffMemberLocationAssignmentRepository : InstitutionConfigurationRepositoryBase, IStaffMemberLocationAssignmentRepository
{
    /// <summary>Initializes a new repository over the shared workforce context.</summary>
    public StaffMemberLocationAssignmentRepository(SkolenzaDbContext context) : base(context) { }

    #region StaffMember Location Assignment Queries

    /// <inheritdoc />
    public Task<StaffMemberLocationAssignment?> GetByIdAsync(long institutionId, long id, CancellationToken cancellationToken) =>
        Context.StaffMemberLocationAssignments.AsNoTracking().Include(x => x.StaffMember).Include(x => x.Campus).FirstOrDefaultAsync(x => x.Id == id && x.InstitutionId == institutionId && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public Task<StaffMemberLocationAssignment?> GetForUpdateAsync(long institutionId, long id, CancellationToken cancellationToken) =>
        Context.StaffMemberLocationAssignments.FirstOrDefaultAsync(x => x.Id == id && x.InstitutionId == institutionId && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public async Task<PagedResponseDTO<StaffMemberLocationAssignment>> GetPagedAsync(
        long institutionId,
        StaffMemberLocationAssignmentFilterRequestDTO filter,
        long requestingStaffMemberId,
        int requestingRoleTypeId,
        CancellationToken cancellationToken)
    {
        var (pageNumber, pageSize) = NormalizePage(filter.PageNumber, filter.PageSize);
        var query = Context.StaffMemberLocationAssignments.AsNoTracking().Include(x => x.StaffMember).Include(x => x.Campus).Where(x => x.InstitutionId == institutionId && !x.IsSoftDeleted);

        // A Manager receives only their own and actively mapped direct reports' locations.
        // StaffMember and unknown role types fail closed to their own location assignments.
        if (requestingRoleTypeId == ConstantValues.RoleTypeManager)
        {
            var today = DateTime.UtcNow.Date;
            query = query.Where(assignment =>
                assignment.StaffMemberId == requestingStaffMemberId ||
                Context.StaffMemberManagerMappings.Any(mapping =>
                    mapping.InstitutionId == institutionId &&
                    mapping.ManagerId == requestingStaffMemberId &&
                    mapping.StaffMemberId == assignment.StaffMemberId &&
                    mapping.IsActive &&
                    mapping.IsSoftDeleted != true &&
                    mapping.EffectiveFrom <= today &&
                    (!mapping.EffectiveTo.HasValue || mapping.EffectiveTo.Value >= today)));
        }
        else if (requestingRoleTypeId != ConstantValues.RoleTypeAdmin)
        {
            query = query.Where(assignment => assignment.StaffMemberId == requestingStaffMemberId);
        }

        if (filter.ResolvedStaffMemberId.HasValue) query = query.Where(x => x.StaffMemberId == filter.ResolvedStaffMemberId.Value);
        if (filter.CampusId.HasValue) query = query.Where(x => x.CampusId == filter.CampusId.Value);
        if (filter.IsPrimary.HasValue) query = query.Where(x => x.IsPrimary == filter.IsPrimary.Value);
        if (filter.IsActive.HasValue) query = query.Where(x => x.IsActive == filter.IsActive.Value);
        var count = await query.CountAsync(cancellationToken);
        var data = await query.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.StaffMemberId).ThenByDescending(x => x.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return CreatePage(data, count, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public Task<bool> IsEligibleStaffMemberAsync(long institutionId, long staffMemberId, CancellationToken cancellationToken) => Context.StaffMembers.AnyAsync(x => x.Id == staffMemberId && x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted, cancellationToken);
    public Task<bool> IsEligibleLocationAsync(long institutionId, long locationId, CancellationToken cancellationToken) => Context.Campuss.AnyAsync(x => x.Id == locationId && x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted, cancellationToken);
    /// <inheritdoc />
    public Task<bool> AssignmentExistsAsync(long institutionId, long staffMemberId, long locationId, DateOnly effectiveFrom, DateOnly? effectiveTo, long? excludeId, CancellationToken cancellationToken) =>
        Context.StaffMemberLocationAssignments.AnyAsync(
            x => x.InstitutionId == institutionId
                && x.StaffMemberId == staffMemberId
                && x.CampusId == locationId
                && x.IsActive
                && !x.IsSoftDeleted
                && (!excludeId.HasValue || x.Id != excludeId.Value)
                && (!effectiveTo.HasValue || x.EffectiveFrom <= effectiveTo.Value)
                && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= effectiveFrom),
            cancellationToken);
    /// <inheritdoc />
    public Task<bool> PrimaryAssignmentExistsAsync(long institutionId, long staffMemberId, DateOnly effectiveFrom, DateOnly? effectiveTo, long? excludeId, CancellationToken cancellationToken) =>
        Context.StaffMemberLocationAssignments.AnyAsync(
            x => x.InstitutionId == institutionId
                && x.StaffMemberId == staffMemberId
                && x.IsPrimary
                && x.IsActive
                && !x.IsSoftDeleted
                && (!excludeId.HasValue || x.Id != excludeId.Value)
                && (!effectiveTo.HasValue || x.EffectiveFrom <= effectiveTo.Value)
                && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= effectiveFrom),
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> WouldInvalidatePrimaryWorkArrangementAsync(long institutionId, long staffMemberId, long locationId, bool isPrimary, bool isAttendanceAllowed, bool isActive, DateOnly effectiveFrom, DateOnly? effectiveTo, CancellationToken cancellationToken)
    {
        // A primary location assignment is part of an arrangement's executable attendance contract.
        // Removing any of its required flags immediately invalidates every live arrangement using it.
        if (!isPrimary || !isAttendanceAllowed || !isActive)
        {
            return Context.StaffMemberWorkArrangements.AnyAsync(
                x => x.InstitutionId == institutionId
                    && x.StaffMemberId == staffMemberId
                    && x.PrimaryCampusId == locationId
                    && x.IsActive
                    && !x.IsSoftDeleted,
                cancellationToken);
        }

        return Context.StaffMemberWorkArrangements.AnyAsync(
            x => x.InstitutionId == institutionId
                && x.StaffMemberId == staffMemberId
                && x.PrimaryCampusId == locationId
                && x.IsActive
                && !x.IsSoftDeleted
                && (effectiveFrom > x.EffectiveFrom
                    || (x.EffectiveTo.HasValue
                        ? !effectiveTo.HasValue || effectiveTo.Value < x.EffectiveTo.Value
                        : effectiveTo.HasValue)),
            cancellationToken);
    }

    #endregion

    #region StaffMember Location Assignment Commands

    /// <inheritdoc />
    public Task AddAsync(StaffMemberLocationAssignment entity, CancellationToken cancellationToken) => Context.StaffMemberLocationAssignments.AddAsync(entity, cancellationToken).AsTask();

    #endregion
}

/// <summary>Provides staffMember-device-enrollment persistence and read-only InstitutionDevice validation.</summary>
public sealed class StaffMemberDeviceEnrollmentRepository : InstitutionConfigurationRepositoryBase, IStaffMemberDeviceEnrollmentRepository
{
    /// <summary>Initializes a new repository over the shared workforce context.</summary>
    public StaffMemberDeviceEnrollmentRepository(SkolenzaDbContext context) : base(context) { }

    #region StaffMember Device Enrollment Queries

    /// <inheritdoc />
    public Task<StaffMemberDeviceEnrollment?> GetByIdAsync(long institutionId, long id, CancellationToken cancellationToken) =>
        Context.StaffMemberDeviceEnrollments.AsNoTracking()
            .Include(x => x.StaffMember)
            .Include(x => x.InstitutionDevice)
            .Include(x => x.Campus)
            .Include(x => x.InstitutionCardMaster)
            .Include(x => x.StaffMemberDeviceAccessWindows)
            .Include(x => x.FaceDeviceCommand).Include(x => x.CardDeviceCommand).Include(x => x.PinDeviceCommand).Include(x => x.UserActivationDeviceCommand)
            .FirstOrDefaultAsync(x => x.Id == id && x.InstitutionId == institutionId && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public Task<StaffMemberDeviceEnrollment?> GetForUpdateAsync(long institutionId, long id, CancellationToken cancellationToken) =>
        Context.StaffMemberDeviceEnrollments
            .Include(x => x.StaffMember)
            .Include(x => x.InstitutionDevice)
            .Include(x => x.Campus)
            .Include(x => x.InstitutionCardMaster)
            .Include(x => x.StaffMemberDeviceAccessWindows)
            .Include(x => x.FaceDeviceCommand).Include(x => x.CardDeviceCommand).Include(x => x.PinDeviceCommand).Include(x => x.UserActivationDeviceCommand)
            .FirstOrDefaultAsync(x => x.Id == id && x.InstitutionId == institutionId && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public async Task<PagedResponseDTO<StaffMemberDeviceEnrollment>> GetPagedAsync(long institutionId, StaffMemberDeviceEnrollmentFilterRequestDTO filter, long requestingStaffMemberId, int requestingRoleTypeId, CancellationToken cancellationToken)
    {
        var (pageNumber, pageSize) = NormalizePage(filter.PageNumber, filter.PageSize);
        var query = Context.StaffMemberDeviceEnrollments.AsNoTracking()
            .Include(x => x.StaffMember)
            .Include(x => x.InstitutionDevice)
            .Include(x => x.Campus)
            .Include(x => x.InstitutionCardMaster)
            .Include(x => x.StaffMemberDeviceAccessWindows)
            .Include(x => x.FaceDeviceCommand).Include(x => x.CardDeviceCommand).Include(x => x.PinDeviceCommand).Include(x => x.UserActivationDeviceCommand)
            .Where(x => x.InstitutionId == institutionId && !x.IsSoftDeleted);
        if (requestingRoleTypeId != ConstantValues.RoleTypeAdmin) query = query.Where(x => x.StaffMemberId == requestingStaffMemberId);
        if (!string.IsNullOrWhiteSpace(filter.Search)) { var term = $"%{filter.Search.Trim()}%"; query = query.Where(x => EF.Functions.ILike(x.EnrollId, term) || (x.InstitutionCardMaster != null && EF.Functions.ILike(x.InstitutionCardMaster.CardReference ?? string.Empty, term)) || EF.Functions.ILike(x.InstitutionDevice.DeviceCode, term)); }
        if (filter.ResolvedStaffMemberId.HasValue) query = query.Where(x => x.StaffMemberId == filter.ResolvedStaffMemberId.Value);
        if (filter.ResolvedInstitutionDeviceId.HasValue) query = query.Where(x => x.InstitutionDeviceId == filter.ResolvedInstitutionDeviceId.Value);
        if (filter.CampusId.HasValue) query = query.Where(x => x.CampusId == filter.CampusId.Value);
        if (filter.IsActive.HasValue) query = query.Where(x => x.IsActive == filter.IsActive.Value);
        var count = await query.CountAsync(cancellationToken);
        var data = await query.OrderByDescending(x => x.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return CreatePage(data, count, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public Task<bool> IsEligibleStaffMemberAsync(long institutionId, long staffMemberId, CancellationToken cancellationToken) => Context.StaffMembers.AnyAsync(x => x.Id == staffMemberId && x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted, cancellationToken);
    /// <inheritdoc />
    public Task<bool> IsEligibleInstitutionDeviceAsync(long institutionId, long institutionDeviceId, CancellationToken cancellationToken) => Context.InstitutionDevices.AnyAsync(x => x.Id == institutionDeviceId && x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted && x.InstitutionDeviceConfiguration != null && x.InstitutionDeviceConfiguration.IsEnrollmentEnabled, cancellationToken);
    /// <inheritdoc />
    public Task<bool> EnrollmentExistsAsync(long institutionId, long staffMemberId, long institutionDeviceId, long? excludeId, CancellationToken cancellationToken) => Context.StaffMemberDeviceEnrollments.AnyAsync(x => x.InstitutionId == institutionId && x.StaffMemberId == staffMemberId && x.InstitutionDeviceId == institutionDeviceId && !x.IsSoftDeleted && (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken);
    /// <inheritdoc />
    public Task<InstitutionDevice?> GetEligibleInstitutionDeviceAsync(long institutionId, long institutionDeviceId, CancellationToken cancellationToken) => Context.InstitutionDevices
        .Include(x => x.InstitutionDeviceConfiguration)
        .FirstOrDefaultAsync(x => x.Id == institutionDeviceId && x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted && x.InstitutionDeviceConfiguration != null && x.InstitutionDeviceConfiguration.IsEnrollmentEnabled, cancellationToken);
    /// <inheritdoc />
    public Task<bool> HasEligibleStaffMemberLocationAssignmentAsync(long institutionId, long staffMemberId, long campusId, DateOnly effectiveOn, CancellationToken cancellationToken) => Context.StaffMemberLocationAssignments.AnyAsync(x => x.InstitutionId == institutionId && x.StaffMemberId == staffMemberId && x.CampusId == campusId && x.IsActive && !x.IsSoftDeleted && x.IsAttendanceAllowed && x.EffectiveFrom <= effectiveOn && (!x.EffectiveTo.HasValue || x.EffectiveTo >= effectiveOn), cancellationToken);
    /// <inheritdoc />
    public Task<InstitutionCardMaster?> GetActiveCardAsync(long institutionId, long cardId, CancellationToken cancellationToken) => Context.InstitutionCardMasters.AsNoTracking().FirstOrDefaultAsync(x => x.Id == cardId && x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted && x.CardStatus == (short)InstitutionCardStatus.Available, cancellationToken);
    /// <inheritdoc />
    public Task<InstitutionCardMaster?> GetCardForUpdateAsync(long institutionId, long cardId, CancellationToken cancellationToken) => Context.InstitutionCardMasters.FirstOrDefaultAsync(x => x.Id == cardId && x.InstitutionId == institutionId && !x.IsSoftDeleted, cancellationToken);

    #endregion

    #region StaffMember Device Enrollment Commands

    /// <inheritdoc />
    public Task AddAsync(StaffMemberDeviceEnrollment entity, CancellationToken cancellationToken) => Context.StaffMemberDeviceEnrollments.AddAsync(entity, cancellationToken).AsTask();

    #endregion
}

/// <summary>Provides Host-only Institution card inventory persistence and assignment checks.</summary>
public sealed class InstitutionCardMasterRepository : InstitutionConfigurationRepositoryBase, IInstitutionCardMasterRepository
{
    public InstitutionCardMasterRepository(SkolenzaDbContext context) : base(context) { }

    public Task<InstitutionCardMaster?> GetByIdAsync(long institutionId, long id, CancellationToken cancellationToken) =>
        Context.InstitutionCardMasters.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.InstitutionId == institutionId && !x.IsSoftDeleted, cancellationToken);

    public Task<InstitutionCardMaster?> GetForUpdateAsync(long institutionId, long id, CancellationToken cancellationToken) =>
        Context.InstitutionCardMasters.FirstOrDefaultAsync(x => x.Id == id && x.InstitutionId == institutionId && !x.IsSoftDeleted, cancellationToken);

    public async Task<PagedResponseDTO<InstitutionCardMaster>> GetPagedAsync(long institutionId, InstitutionCardMasterFilterRequestDTO filter, CancellationToken cancellationToken)
    {
        var (pageNumber, pageSize) = NormalizePage(filter.PageNumber, filter.PageSize);
        var query = Context.InstitutionCardMasters.AsNoTracking().Where(x => x.InstitutionId == institutionId && !x.IsSoftDeleted);
        if (filter.CardStatus.HasValue) query = query.Where(x => x.CardStatus == (short)filter.CardStatus.Value);
        if (filter.IsActive.HasValue) query = query.Where(x => x.IsActive == filter.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = $"%{filter.Search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.CardReference ?? string.Empty, term) || EF.Functions.ILike(x.SupplierName ?? string.Empty, term) || EF.Functions.ILike(x.PurchaseInvoiceNumber ?? string.Empty, term));
        }

        var count = await query.CountAsync(cancellationToken);
        var data = await query.OrderByDescending(x => x.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return CreatePage(data, count, pageNumber, pageSize);
    }

    public Task<bool> CardHashExistsAsync(long institutionId, string lookupHash, long? excludeId, CancellationToken cancellationToken) =>
        Context.InstitutionCardMasters.AnyAsync(x => x.InstitutionId == institutionId && x.CardNumberLookupHash == lookupHash && !x.IsSoftDeleted && (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken);

    public Task<bool> HasActiveEnrollmentAssignmentAsync(long institutionId, long cardId, CancellationToken cancellationToken) =>
        Context.StaffMemberDeviceEnrollments.AnyAsync(x => x.InstitutionId == institutionId && x.InstitutionCardMasterId == cardId && x.IsActive && !x.IsSoftDeleted, cancellationToken);

    public Task AddAsync(InstitutionCardMaster entity, CancellationToken cancellationToken) => Context.InstitutionCardMasters.AddAsync(entity, cancellationToken).AsTask();
}

/// <summary>Provides staffMember-work-arrangement persistence and lifecycle queries.</summary>
public sealed class StaffMemberWorkArrangementRepository : InstitutionConfigurationRepositoryBase, IStaffMemberWorkArrangementRepository
{
    /// <summary>Initializes a new repository over the shared workforce context.</summary>
    public StaffMemberWorkArrangementRepository(SkolenzaDbContext context) : base(context) { }

    #region StaffMember Work Arrangement Queries

    /// <inheritdoc />
    public Task<StaffMemberWorkArrangement?> GetByIdAsync(long institutionId, long id, CancellationToken cancellationToken) =>
        Context.StaffMemberWorkArrangements.AsNoTracking().Include(x => x.StaffMember).Include(x => x.PolicyVersion).ThenInclude(x => x!.Policy).Include(x => x.PrimaryCampus).FirstOrDefaultAsync(x => x.Id == id && x.InstitutionId == institutionId && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public Task<StaffMemberWorkArrangement?> GetForUpdateAsync(long institutionId, long id, CancellationToken cancellationToken) =>
        Context.StaffMemberWorkArrangements.FirstOrDefaultAsync(x => x.Id == id && x.InstitutionId == institutionId && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public async Task<PagedResponseDTO<StaffMemberWorkArrangement>> GetPagedAsync(long institutionId, StaffMemberWorkArrangementFilterRequestDTO filter, long requestingStaffMemberId, int requestingRoleTypeId, CancellationToken cancellationToken)
    {
        var (pageNumber, pageSize) = NormalizePage(filter.PageNumber, filter.PageSize);
        var query = Context.StaffMemberWorkArrangements.AsNoTracking().Include(x => x.StaffMember).Include(x => x.PolicyVersion).ThenInclude(x => x!.Policy).Include(x => x.PrimaryCampus).Where(x => x.InstitutionId == institutionId && !x.IsSoftDeleted);
        if (requestingRoleTypeId != ConstantValues.RoleTypeAdmin) query = query.Where(x => x.StaffMemberId == requestingStaffMemberId);
        if (filter.ResolvedStaffMemberId.HasValue) query = query.Where(x => x.StaffMemberId == filter.ResolvedStaffMemberId.Value);
        if (filter.PolicyVersionId.HasValue) query = query.Where(x => x.PolicyVersionId == filter.PolicyVersionId.Value);
        if (filter.PrimaryCampusId.HasValue) query = query.Where(x => x.PrimaryCampusId == filter.PrimaryCampusId.Value);
        if (filter.WorkMode.HasValue) query = query.Where(x => x.WorkMode == (short)filter.WorkMode.Value);
        if (filter.IsActive.HasValue) query = query.Where(x => x.IsActive == filter.IsActive.Value);
        var count = await query.CountAsync(cancellationToken);
        var data = await query.OrderByDescending(x => x.IsActive).ThenByDescending(x => x.EffectiveFrom).ThenByDescending(x => x.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return CreatePage(data, count, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public async Task<List<AttendancePolicyOptionResponseDTO>> GetAttendancePolicyOptionsAsync(
        long institutionId,
        DateOnly effectiveOn,
        string? search,
        CancellationToken cancellationToken)
    {
        var normalizedSearch = search?.Trim();
        var query =
            from version in Context.PolicyVersions.AsNoTracking()
            join policy in Context.Policies.AsNoTracking() on version.PolicyId equals policy.Id
            join policyType in Context.PolicyTypes.AsNoTracking() on policy.PolicyTypeId equals policyType.Id
            join category in Context.PolicyCategories.AsNoTracking() on policyType.PolicyCategoryId equals category.Id
            join status in Context.PolicyStatuses.AsNoTracking() on version.PolicyStatusId equals status.Id
            where version.InstitutionId == institutionId
                && policy.InstitutionId == institutionId
                && policyType.InstitutionId == institutionId
                && category.CategoryCode == AppConstants.PolicyCategoryCodes.Attendance
                && category.IsActive
                && policyType.IsActive == true
                && policyType.IsSoftDelete != true
                && policy.IsActive
                && !policy.IsSoftDeleted
                && version.IsActive
                && status.IsActive
                && status.StatusCode == AppConstants.PolicyStatusCodes.Published
                && version.EffectiveFrom <= effectiveOn
                && (!version.EffectiveTo.HasValue || version.EffectiveTo.Value >= effectiveOn)
            select new AttendancePolicyOptionResponseDTO
            {
                PolicyId = policy.Id,
                PolicyVersionId = version.Id,
                PolicyCode = policy.PolicyCode,
                PolicyName = policy.PolicyName,
                PolicyTypeId = policyType.Id,
                PolicyTypeCode = policyType.PolicyTypeCode ?? string.Empty,
                PolicyTypeName = policyType.PolicyName,
                VersionNumber = version.VersionNumber,
                EffectiveFrom = version.EffectiveFrom,
                EffectiveTo = version.EffectiveTo
            };

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var pattern = $"%{normalizedSearch}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.PolicyName, pattern)
                || EF.Functions.ILike(x.PolicyCode, pattern)
                || EF.Functions.ILike(x.PolicyTypeName, pattern)
                || EF.Functions.ILike(x.PolicyTypeCode, pattern));
        }

        var candidates = await query.ToListAsync(cancellationToken);
        return candidates
            .GroupBy(x => x.PolicyId)
            .Select(group => group
                .OrderByDescending(x => x.VersionNumber)
                .ThenByDescending(x => x.PolicyVersionId)
                .First())
            .OrderBy(x => x.PolicyName)
            .ThenBy(x => x.PolicyCode)
            .Select(x =>
            {
                x.DisplayName = $"{x.PolicyName} - v{x.VersionNumber}";
                return x;
            })
            .ToList();
    }

    /// <inheritdoc />
    public Task<bool> IsEligibleStaffMemberAsync(long institutionId, long staffMemberId, CancellationToken cancellationToken) => Context.StaffMembers.AnyAsync(x => x.Id == staffMemberId && x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted, cancellationToken);
    /// <inheritdoc />
    public Task<StaffMember?> GetStaffMemberForValidationAsync(long institutionId, long staffMemberId, CancellationToken cancellationToken) =>
        Context.StaffMembers.AsNoTracking().FirstOrDefaultAsync(
            x => x.Id == staffMemberId && x.InstitutionId == institutionId && !x.IsSoftDeleted,
            cancellationToken);
    /// <inheritdoc />
    public Task<bool> IsEligibleAttendancePolicyVersionAsync(long institutionId, long policyVersionId, DateOnly effectiveOn, CancellationToken cancellationToken) =>
        (from version in Context.PolicyVersions
         join policy in Context.Policies on version.PolicyId equals policy.Id
         join policyType in Context.PolicyTypes on policy.PolicyTypeId equals policyType.Id
         join category in Context.PolicyCategories on policyType.PolicyCategoryId equals category.Id
         join status in Context.PolicyStatuses on version.PolicyStatusId equals status.Id
         where version.Id == policyVersionId
             && version.InstitutionId == institutionId
             && policy.InstitutionId == institutionId
             && policyType.InstitutionId == institutionId
             && category.CategoryCode == AppConstants.PolicyCategoryCodes.Attendance
             && category.IsActive
             && policyType.IsActive == true
             && policyType.IsSoftDelete != true
             && policy.IsActive
             && !policy.IsSoftDeleted
             && version.IsActive
             && status.IsActive
             && status.StatusCode == AppConstants.PolicyStatusCodes.Published
             && version.EffectiveFrom <= effectiveOn
             && (!version.EffectiveTo.HasValue || version.EffectiveTo.Value >= effectiveOn)
         select version.Id).AnyAsync(cancellationToken);
    /// <inheritdoc />
    public Task<AttendancePolicyVersionConfiguration?> GetAttendanceConfigurationAsync(long institutionId, long policyVersionId, CancellationToken cancellationToken) =>
        Context.AttendancePolicyVersionConfigurations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.InstitutionId == institutionId && x.PolicyVersionId == policyVersionId, cancellationToken);
    /// <inheritdoc />
    public Task<bool> IsEligibleLocationAsync(long institutionId, long locationId, CancellationToken cancellationToken) => Context.Campuss.AnyAsync(x => x.Id == locationId && x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted, cancellationToken);
    /// <inheritdoc />
    public Task<StaffMemberWorkArrangement?> GetOverlappingArrangementAsync(long institutionId, long staffMemberId, DateOnly effectiveFrom, DateOnly? effectiveTo, long? excludeId, CancellationToken cancellationToken) =>
        Context.StaffMemberWorkArrangements.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId
                && x.StaffMemberId == staffMemberId
                && x.IsActive
                && !x.IsSoftDeleted
                && (!excludeId.HasValue || x.Id != excludeId.Value)
                && (!effectiveTo.HasValue || x.EffectiveFrom <= effectiveTo.Value)
                && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= effectiveFrom))
            .OrderBy(x => x.EffectiveFrom)
            .ThenBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public Task<bool> HasCoveringPrimaryLocationAssignmentAsync(long institutionId, long staffMemberId, long locationId, DateOnly effectiveFrom, DateOnly? effectiveTo, CancellationToken cancellationToken) =>
        Context.StaffMemberLocationAssignments.AnyAsync(
            x => x.InstitutionId == institutionId
                && x.StaffMemberId == staffMemberId
                && x.CampusId == locationId
                && x.IsPrimary
                && x.IsAttendanceAllowed
                && x.IsActive
                && !x.IsSoftDeleted
                && x.EffectiveFrom <= effectiveFrom
                && (effectiveTo.HasValue
                    ? !x.EffectiveTo.HasValue || x.EffectiveTo.Value >= effectiveTo.Value
                    : !x.EffectiveTo.HasValue),
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> HasCoveringAttendanceLocationAssignmentAsync(long institutionId, long staffMemberId, long locationId, DateOnly effectiveFrom, DateOnly effectiveTo, CancellationToken cancellationToken) =>
        Context.StaffMemberLocationAssignments.AnyAsync(
            x => x.InstitutionId == institutionId
                && x.StaffMemberId == staffMemberId
                && x.CampusId == locationId
                && x.IsAttendanceAllowed
                && x.IsActive
                && !x.IsSoftDeleted
                && x.EffectiveFrom <= effectiveFrom
                && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= effectiveTo),
            cancellationToken);

    /// <inheritdoc />
    public Task<List<StaffMemberLocationAssignment>> GetLocationAssignmentsForValidationAsync(
        long institutionId,
        long staffMemberId,
        long locationId,
        CancellationToken cancellationToken) =>
        Context.StaffMemberLocationAssignments.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId
                && x.StaffMemberId == staffMemberId
                && x.CampusId == locationId
                && !x.IsSoftDeleted)
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.IsPrimary)
            .ThenByDescending(x => x.IsAttendanceAllowed)
            .ThenByDescending(x => x.EffectiveFrom)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<Campus?> GetLocationForValidationAsync(long institutionId, long locationId, CancellationToken cancellationToken) =>
        Context.Campuss.AsNoTracking().FirstOrDefaultAsync(
            x => x.Id == locationId && x.InstitutionId == institutionId && !x.IsSoftDeleted,
            cancellationToken);

    /// <inheritdoc />
    public async Task<CampusType?> GetEligibleLocationTypeAsync(long institutionId, long locationId, CancellationToken cancellationToken)
    {
        var locationType = await Context.Campuss
            .Where(x => x.Id == locationId && x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted)
            .Select(x => (short?)x.LocationType)
            .SingleOrDefaultAsync(cancellationToken);

        return locationType.HasValue ? (CampusType)locationType.Value : null;
    }
    /// <inheritdoc />
    public async Task<bool> HasLiveActiveDependenciesAsync(long institutionId, long arrangementId, CancellationToken cancellationToken) =>
        await Context.StaffMemberWorkPatterns.AnyAsync(x => x.InstitutionId == institutionId && x.StaffMemberWorkArrangementId == arrangementId && x.IsActive && !x.IsSoftDeleted, cancellationToken)
        || await Context.StaffMemberWorkModeOverrideRequests.AnyAsync(x => x.InstitutionId == institutionId && x.StaffMemberWorkArrangementId == arrangementId && x.IsActive && !x.IsSoftDeleted, cancellationToken);
    /// <inheritdoc />
    public async Task<bool> HasAnyDependenciesAsync(long institutionId, long arrangementId, CancellationToken cancellationToken) =>
        await Context.StaffMemberWorkPatterns.AnyAsync(x => x.InstitutionId == institutionId && x.StaffMemberWorkArrangementId == arrangementId && !x.IsSoftDeleted, cancellationToken)
        || await Context.StaffMemberWorkModeOverrideRequests.AnyAsync(x => x.InstitutionId == institutionId && x.StaffMemberWorkArrangementId == arrangementId && !x.IsSoftDeleted, cancellationToken);

    #endregion

    #region StaffMember Work Arrangement Commands

    /// <inheritdoc />
    public Task AddAsync(StaffMemberWorkArrangement entity, CancellationToken cancellationToken) => Context.StaffMemberWorkArrangements.AddAsync(entity, cancellationToken).AsTask();

    #endregion
}

/// <summary>Provides staffMember-work-pattern persistence and validation queries.</summary>
public sealed class StaffMemberWorkPatternRepository : InstitutionConfigurationRepositoryBase, IStaffMemberWorkPatternRepository
{
    /// <summary>Initializes a new repository over the shared workforce context.</summary>
    public StaffMemberWorkPatternRepository(SkolenzaDbContext context) : base(context) { }

    #region StaffMember Work Pattern Queries

    /// <inheritdoc />
    public Task<StaffMemberWorkPattern?> GetByIdAsync(long institutionId, long id, CancellationToken cancellationToken) =>
        Context.StaffMemberWorkPatterns.AsNoTracking()
            .Include(x => x.StaffMemberWorkArrangement)
            .ThenInclude(x => x.StaffMember)
            .Include(x => x.Campus)
            .FirstOrDefaultAsync(x => x.Id == id && x.InstitutionId == institutionId && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public Task<StaffMemberWorkPattern?> GetForUpdateAsync(long institutionId, long id, CancellationToken cancellationToken) =>
        Context.StaffMemberWorkPatterns.Include(x => x.StaffMemberWorkArrangement).FirstOrDefaultAsync(x => x.Id == id && x.InstitutionId == institutionId && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public async Task<PagedResponseDTO<StaffMemberWorkPattern>> GetPagedAsync(long institutionId, StaffMemberWorkPatternFilterRequestDTO filter, long requestingStaffMemberId, int requestingRoleTypeId, CancellationToken cancellationToken)
    {
        var (pageNumber, pageSize) = NormalizePage(filter.PageNumber, filter.PageSize);
        var query = Context.StaffMemberWorkPatterns.AsNoTracking()
            .Include(x => x.StaffMemberWorkArrangement)
            .ThenInclude(x => x.StaffMember)
            .Include(x => x.Campus)
            .Where(x => x.InstitutionId == institutionId && !x.IsSoftDeleted);
        if (requestingRoleTypeId != ConstantValues.RoleTypeAdmin) query = query.Where(x => x.StaffMemberWorkArrangement.StaffMemberId == requestingStaffMemberId);
        if (filter.StaffMemberWorkArrangementId.HasValue) query = query.Where(x => x.StaffMemberWorkArrangementId == filter.StaffMemberWorkArrangementId.Value);
        if (filter.DayOfWeek.HasValue) query = query.Where(x => x.DayOfWeek == (short)filter.DayOfWeek.Value);
        if (filter.IsActive.HasValue) query = query.Where(x => x.IsActive == filter.IsActive.Value);
        var count = await query.CountAsync(cancellationToken);
        var data = await query.OrderBy(x => x.DayOfWeek).ThenBy(x => x.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return CreatePage(data, count, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public Task<bool> IsEligibleArrangementAsync(long institutionId, long arrangementId, CancellationToken cancellationToken) => Context.StaffMemberWorkArrangements.AnyAsync(x => x.Id == arrangementId && x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted, cancellationToken);
    /// <inheritdoc />
    public Task<bool> IsEligibleLocationAsync(long institutionId, long locationId, CancellationToken cancellationToken) => Context.Campuss.AnyAsync(x => x.Id == locationId && x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted, cancellationToken);
    /// <inheritdoc />
    public Task<bool> PatternDayExistsAsync(long institutionId, long arrangementId, short dayOfWeek, long? excludeId, CancellationToken cancellationToken) => Context.StaffMemberWorkPatterns.AnyAsync(x => x.InstitutionId == institutionId && x.StaffMemberWorkArrangementId == arrangementId && x.DayOfWeek == dayOfWeek && x.IsActive && !x.IsSoftDeleted && (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken);

    #endregion

    #region StaffMember Work Pattern Commands

    /// <inheritdoc />
    public Task AddAsync(StaffMemberWorkPattern entity, CancellationToken cancellationToken) => Context.StaffMemberWorkPatterns.AddAsync(entity, cancellationToken).AsTask();

    #endregion
}

/// <summary>Provides staffMember work-mode-override persistence and validation queries.</summary>
public sealed class StaffMemberWorkModeOverrideRequestRepository : InstitutionConfigurationRepositoryBase, IStaffMemberWorkModeOverrideRequestRepository
{
    /// <summary>Initializes a new repository over the shared workforce context.</summary>
    public StaffMemberWorkModeOverrideRequestRepository(SkolenzaDbContext context) : base(context) { }

    #region StaffMember Work Mode Override Queries

    /// <inheritdoc />
    public Task<StaffMemberWorkModeOverrideRequest?> GetByIdAsync(long institutionId, long id, CancellationToken cancellationToken) =>
        Context.StaffMemberWorkModeOverrideRequests.AsNoTracking().Include(x => x.StaffMember).Include(x => x.Campus).FirstOrDefaultAsync(x => x.Id == id && x.InstitutionId == institutionId && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public Task<StaffMemberWorkModeOverrideRequest?> GetForUpdateAsync(long institutionId, long id, CancellationToken cancellationToken) =>
        Context.StaffMemberWorkModeOverrideRequests.FirstOrDefaultAsync(x => x.Id == id && x.InstitutionId == institutionId && !x.IsSoftDeleted, cancellationToken);

    /// <inheritdoc />
    public async Task<PagedResponseDTO<StaffMemberWorkModeOverrideRequest>> GetPagedAsync(long institutionId, StaffMemberWorkModeOverrideFilterRequestDTO filter, long requestingStaffMemberId, int requestingRoleTypeId, CancellationToken cancellationToken)
    {
        var (pageNumber, pageSize) = NormalizePage(filter.PageNumber, filter.PageSize);
        var query = Context.StaffMemberWorkModeOverrideRequests.AsNoTracking().Include(x => x.StaffMember).Include(x => x.Campus).Where(x => x.InstitutionId == institutionId && !x.IsSoftDeleted);
        if (requestingRoleTypeId != ConstantValues.RoleTypeAdmin) query = query.Where(x => x.StaffMemberId == requestingStaffMemberId);
        if (!string.IsNullOrWhiteSpace(filter.Search)) { var term = $"%{filter.Search.Trim()}%"; query = query.Where(x => EF.Functions.ILike(x.Reason, term)); }
        if (filter.ResolvedStaffMemberId.HasValue) query = query.Where(x => x.StaffMemberId == filter.ResolvedStaffMemberId.Value);
        if (filter.RequestedWorkMode.HasValue) query = query.Where(x => x.RequestedWorkMode == (short)filter.RequestedWorkMode.Value);
        if (filter.ApprovalStatus.HasValue) query = query.Where(x => x.ApprovalStatus == (short)filter.ApprovalStatus.Value);
        if (filter.FromDate.HasValue) query = query.Where(x => x.FromDate >= filter.FromDate.Value);
        if (filter.ToDate.HasValue) query = query.Where(x => x.ToDate <= filter.ToDate.Value);
        if (filter.IsActive.HasValue) query = query.Where(x => x.IsActive == filter.IsActive.Value);
        var count = await query.CountAsync(cancellationToken);
        var data = await query.OrderByDescending(x => x.FromDate).ThenByDescending(x => x.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return CreatePage(data, count, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public Task<bool> IsEligibleStaffMemberAsync(long institutionId, long staffMemberId, CancellationToken cancellationToken) => Context.StaffMembers.AnyAsync(x => x.Id == staffMemberId && x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted, cancellationToken);
    /// <inheritdoc />
    public Task<bool> IsEligibleArrangementAsync(long institutionId, long arrangementId, CancellationToken cancellationToken) => Context.StaffMemberWorkArrangements.AnyAsync(x => x.Id == arrangementId && x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted, cancellationToken);
    /// <inheritdoc />
    public Task<bool> IsEligibleLocationAsync(long institutionId, long locationId, CancellationToken cancellationToken) => Context.Campuss.AnyAsync(x => x.Id == locationId && x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted, cancellationToken);
    /// <inheritdoc />
    public Task<StaffMemberWorkModeOverrideRequest?> GetOverlappingOverrideAsync(long institutionId, long staffMemberId, DateOnly fromDate, DateOnly toDate, long? excludeId, CancellationToken cancellationToken) =>
        Context.StaffMemberWorkModeOverrideRequests.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId
                && x.StaffMemberId == staffMemberId
                && x.IsActive
                && !x.IsSoftDeleted
                && (x.ApprovalStatus == (short)WorkModeOverrideApprovalStatus.Pending
                    || x.ApprovalStatus == (short)WorkModeOverrideApprovalStatus.Approved)
                && (!excludeId.HasValue || x.Id != excludeId.Value)
                && x.FromDate <= toDate
                && x.ToDate >= fromDate)
            .OrderBy(x => x.FromDate)
            .ThenBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    #endregion

    #region StaffMember Work Mode Override Commands

    /// <inheritdoc />
    public Task AddAsync(StaffMemberWorkModeOverrideRequest entity, CancellationToken cancellationToken) => Context.StaffMemberWorkModeOverrideRequests.AddAsync(entity, cancellationToken).AsTask();

    #endregion
}
