using System.Data;
using static skolenza.application.Constants.ConstantValues;
using skolenza.application.DTOs.Attendance;
using skolenza.application.Exceptions;
using skolenza.application.Features.AttendanceCmd;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Repositories;

public sealed class AttendanceRepository(SkolenzaDbContext context) : IAttendanceRepository
{
    public async Task<AttendancePunchResponseDTO> MarkAsync(long institutionId, long staffMemberId,
        AttendanceRequestDTO request, DateTime serverUtcNow, CancellationToken cancellationToken)
    {
        var existing = await context.StaffMemberAttendancePunches.AsNoTracking()
            .Include(x => x.AttendanceDeviceType)
            .FirstOrDefaultAsync(x => x.InstitutionId == institutionId && x.StaffMemberId == staffMemberId
                && x.IdempotencyKey == request.IdempotencyKey, cancellationToken);
        if (existing != null) return Map(existing);

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var attendanceLockKey = unchecked((institutionId * 397) ^ staffMemberId);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({attendanceLockKey})", cancellationToken);

        existing = await context.StaffMemberAttendancePunches.AsNoTracking()
            .Include(x => x.AttendanceDeviceType)
            .FirstOrDefaultAsync(x => x.InstitutionId == institutionId && x.StaffMemberId == staffMemberId
                && x.IdempotencyKey == request.IdempotencyKey, cancellationToken);
        if (existing != null) return Map(existing);

        if (!await context.StaffMembers.AsNoTracking().AnyAsync(x => x.Id == staffMemberId
            && x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted, cancellationToken))
            throw new ValidationErrorException("The authenticated staffMember is not active.");

        var utc = DateTime.SpecifyKind(serverUtcNow, DateTimeKind.Utc);
        // Effective-dated attendance rules follow the staffMember's business day, not the UTC calendar day.
        var initialTimeZoneId = request.CampusId.HasValue
            ? await context.Campuss.AsNoTracking()
                .Where(x => x.Id == request.CampusId.Value && x.InstitutionId == institutionId
                    && x.IsActive && !x.IsSoftDeleted)
                .Select(x => x.TimeZoneId).FirstOrDefaultAsync(cancellationToken)
            : null;
        initialTimeZoneId ??= await GetHeadOfficeTimeZoneAsync(institutionId, cancellationToken);
        var utcDate = ConvertToWorkDate(utc, initialTimeZoneId);
        var arrangements = await context.StaffMemberWorkArrangements.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.StaffMemberId == staffMemberId && x.IsActive
                && !x.IsSoftDeleted && x.PolicyVersionId.HasValue
                && x.EffectiveFrom <= utcDate && (!x.EffectiveTo.HasValue || x.EffectiveTo >= utcDate))
            .ToListAsync(cancellationToken);
        if (arrangements.Count != 1)
            throw new ConflictException(arrangements.Count == 0
                ? "No effective work arrangement is configured for today."
                : "More than one effective work arrangement exists for today.");
        var arrangement = arrangements[0];

        var configuration = await context.AttendancePolicyVersionConfigurations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.InstitutionId == institutionId
                && x.PolicyVersionId == arrangement.PolicyVersionId!.Value, cancellationToken)
            ?? throw new ValidationErrorException("The effective Attendance policy has no execution configuration.");
        var deviceType = await context.AttendanceDeviceTypes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.AttendanceDeviceTypeId
                && x.IsActive == true, cancellationToken)
            ?? throw new ValidationErrorException("The selected Attendance device type is invalid or inactive.");
        var deviceTypeCode = deviceType.DeviceTypeCode.Trim().ToUpperInvariant();
        var isAllowedSelfServiceType = deviceTypeCode is AttendanceDeviceMobile or AttendanceDeviceWeb;
        if (!isAllowedSelfServiceType)
            throw new ValidationErrorException("This endpoint accepts Mobile or Web attendance only.");
        if (deviceTypeCode == AttendanceDeviceMobile && !configuration.AllowMobile
            || deviceTypeCode == AttendanceDeviceWeb && !configuration.AllowWeb)
            throw new ForbiddenAccessException("The selected attendance device type is disabled by policy.");
        // Attach the catalogue row as existing so adding a punch can never insert or modify seed data.
        context.Attach(deviceType);

        var locationId = request.CampusId ?? arrangement.PrimaryCampusId;
        var workMode = (WorkMode)arrangement.WorkMode;
        Campus? location = null;
        if (workMode == WorkMode.WorkFromHome)
        {
            if (!configuration.AllowWorkFromHome || request.CampusId.HasValue)
                throw new ForbiddenAccessException("Work From Home attendance is not allowed by policy.");
        }
        else
        {
            if (!locationId.HasValue) throw new ValidationErrorException("A work location is required.");
            location = await context.Campuss.AsNoTracking().FirstOrDefaultAsync(x =>
                x.Id == locationId.Value && x.InstitutionId == institutionId && x.IsActive
                && !x.IsSoftDeleted && x.IsAttendanceAllowed, cancellationToken)
                ?? throw new ValidationErrorException("The attendance location is invalid or disabled.");
            await ValidateLocationScopeAsync(configuration, arrangement, staffMemberId, location.Id, utcDate, cancellationToken);
        }

        var requireGps = workMode == WorkMode.WorkFromHome
            ? configuration.RequireGpsForRemote
            : configuration.RequireGeoFenceForOffice;
        decimal? distance = null;
        if (requireGps)
        {
            if (!request.Latitude.HasValue || !request.Longitude.HasValue)
                throw new ValidationErrorException("GPS coordinates are required by the Attendance policy.");
            if (location != null && configuration.RequireGeoFenceForOffice)
            {
                if (!location.Latitude.HasValue || !location.Longitude.HasValue
                    || !location.GeoFenceRadiusMeters.HasValue || location.GeoFenceRadiusMeters <= 0)
                    throw new ValidationErrorException("The selected location has no complete geofence configuration.");
                distance = (decimal)DistanceMeters((double)request.Latitude.Value, (double)request.Longitude.Value,
                    (double)location.Latitude.Value, (double)location.Longitude.Value);
                if (distance > location.GeoFenceRadiusMeters.Value + (request.AccuracyMeters ?? 0))
                    throw new ForbiddenAccessException("You are outside the allowed attendance geofence.");
            }
        }

        var timeZoneId = location?.TimeZoneId ?? await context.Campuss.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.IsHeadOffice && x.IsActive && !x.IsSoftDeleted)
            .Select(x => x.TimeZoneId).FirstOrDefaultAsync(cancellationToken) ?? "UTC";
        var workDate = ConvertToWorkDate(utc, timeZoneId);
        if (workDate != utcDate)
            throw new ConflictException("The location business date differs from the effective work arrangement date. Refresh and try again.");

        var last = await context.StaffMemberAttendancePunches.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.StaffMemberId == staffMemberId && x.WorkDate == workDate)
            .OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var lastAction = last == null ? (AttendancePunchAction?)null : (AttendancePunchAction)last.PunchAction;
        var checkedIn = lastAction == AttendancePunchAction.CheckIn;
        if (!AttendancePunchRules.IsAllowedTransition(lastAction, request.Action)
            && request.Action == AttendancePunchAction.CheckIn)
            throw new ConflictException("StaffMember is already checked in.");
        if (!AttendancePunchRules.IsAllowedTransition(lastAction, request.Action)
            && request.Action == AttendancePunchAction.CheckOut)
            throw new ConflictException("Check-in is required before check-out.");

        var entity = new StaffMemberAttendancePunch
        {
            InstitutionId = institutionId, StaffMemberId = staffMemberId, StaffMemberWorkArrangementId = arrangement.Id,
            PolicyVersionId = arrangement.PolicyVersionId.Value, CampusId = locationId,
            WorkDate = workDate, OccurredAtUtc = utc, ClientOccurredAt = request.ClientOccurredAt,
            AttendanceDeviceTypeId = deviceType.Id, AttendanceDeviceType = deviceType,
            PunchAction = (short)request.Action,
            Latitude = request.Latitude, Longitude = request.Longitude, AccuracyMeters = request.AccuracyMeters,
            DistanceFromLocationMeters = distance, IdempotencyKey = request.IdempotencyKey,
            AddedById = staffMemberId, AddedDateTime = utc
        };
        context.StaffMemberAttendancePunches.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<AttendanceTodayResponseDTO> GetTodayAsync(long institutionId, long staffMemberId,
        DateTime serverUtcNow, CancellationToken cancellationToken)
    {
        if (!await context.StaffMembers.AsNoTracking().AnyAsync(x => x.Id == staffMemberId
            && x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted, cancellationToken))
            throw new ValidationErrorException("The authenticated staffMember is not active.");

        var utc = DateTime.SpecifyKind(serverUtcNow, DateTimeKind.Utc);
        var workDate = ConvertToWorkDate(utc,
            await GetHeadOfficeTimeZoneAsync(institutionId, cancellationToken));
        var punches = await context.StaffMemberAttendancePunches.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.StaffMemberId == staffMemberId
                && x.WorkDate == workDate)
            .OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.Id)
            .Select(x => new AttendancePunchItemDTO(x.Id,
                (AttendancePunchAction)x.PunchAction, x.AttendanceDeviceTypeId,
                x.AttendanceDeviceType.DeviceTypeCode, x.AttendanceDeviceType.DeviceType ?? string.Empty,
                x.OccurredAtUtc, x.CampusId))
            .ToListAsync(cancellationToken);

        return new AttendanceTodayResponseDTO(workDate,
            punches.LastOrDefault()?.Action == AttendancePunchAction.CheckIn, punches);
    }

    public async Task<IReadOnlyList<AttendanceDeviceTypeOptionDTO>> GetActiveDeviceTypesAsync(
        CancellationToken cancellationToken)
    {
        return await context.AttendanceDeviceTypes.AsNoTracking()
            .Where(x => x.IsActive == true)
            .OrderBy(x => x.Id)
            .Select(x => new AttendanceDeviceTypeOptionDTO(x.Id, x.DeviceTypeCode,
                x.DeviceType ?? string.Empty, x.IsDeviceRegister == true))
            .ToListAsync(cancellationToken);
    }

    private async Task ValidateLocationScopeAsync(AttendancePolicyVersionConfiguration configuration,
        StaffMemberWorkArrangement arrangement, long staffMemberId, long locationId, DateOnly date,
        CancellationToken cancellationToken)
    {
        var scope = (AttendanceLocationScope)configuration.AttendanceLocationScope;
        if (scope == AttendanceLocationScope.RemoteAnywhere)
            throw new ForbiddenAccessException("This policy permits remote attendance only.");
        if (scope == AttendanceLocationScope.PrimaryLocationOnly
            && arrangement.PrimaryCampusId != locationId)
            throw new ForbiddenAccessException("Attendance is allowed only at the primary location.");
        if (scope == AttendanceLocationScope.AssignedLocations
            && !await context.StaffMemberLocationAssignments.AsNoTracking().AnyAsync(x =>
                x.InstitutionId == arrangement.InstitutionId && x.StaffMemberId == staffMemberId
                && x.CampusId == locationId && x.IsAttendanceAllowed && x.IsActive
                && !x.IsSoftDeleted && x.EffectiveFrom <= date
                && (!x.EffectiveTo.HasValue || x.EffectiveTo >= date), cancellationToken))
            throw new ForbiddenAccessException("The location is not assigned to this staffMember.");
    }

    private static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double radius = 6371000;
        static double Rad(double value) => value * Math.PI / 180;
        var dLat = Rad(lat2 - lat1); var dLon = Rad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return radius * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private async Task<string> GetHeadOfficeTimeZoneAsync(long institutionId,
        CancellationToken cancellationToken)
    {
        return await context.Campuss.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.IsHeadOffice && x.IsActive && !x.IsSoftDeleted)
            .Select(x => x.TimeZoneId).FirstOrDefaultAsync(cancellationToken) ?? "UTC";
    }

    private static DateOnly ConvertToWorkDate(DateTime utc, string timeZoneId)
    {
        try
        {
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(utc, timeZoneId));
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ValidationErrorException("The work location time zone is invalid.");
        }
        catch (InvalidTimeZoneException)
        {
            throw new ValidationErrorException("The work location time zone is invalid.");
        }
    }

    private static AttendancePunchResponseDTO Map(StaffMemberAttendancePunch x) => new(x.Id,
        (AttendancePunchAction)x.PunchAction, x.AttendanceDeviceTypeId,
        x.AttendanceDeviceType.DeviceTypeCode, x.AttendanceDeviceType.DeviceType ?? string.Empty, x.WorkDate,
        x.OccurredAtUtc, x.CampusId, x.DistanceFromLocationMeters,
        x.PunchAction == (short)AttendancePunchAction.CheckIn);
}
