using skolenza.application.DTOS.Dashboard;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IRepositories;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Repositories;

public sealed class SuperAdminDashboardRepository(
    SkolenzaDbContext context,
    IIdEncoderService idEncoderService) : ISuperAdminDashboardRepository
{
    public async Task<DashboardSummaryDTO> GetSummaryAsync(long institutionId, DateTime today, CancellationToken cancellationToken)
    {
        var staffMembers = context.StaffMembers.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && !x.IsSoftDeleted);
        var total = await staffMembers.CountAsync(cancellationToken);
        var newHires = await staffMembers.CountAsync(x => x.DateOfOnBoarding.HasValue
            && x.DateOfOnBoarding.Value.Year == today.Year
            && x.DateOfOnBoarding.Value.Month == today.Month, cancellationToken);
        return new DashboardSummaryDTO(total, newHires, 10, 3);
    }

    public async Task<StaffMemberOverviewDTO> GetStaffMemberOverviewAsync(long institutionId, DateOnly today, CancellationToken cancellationToken)
    {
        var staffMembers = context.StaffMembers.AsNoTracking().Where(x => x.InstitutionId == institutionId && !x.IsSoftDeleted);
        var total = await staffMembers.CountAsync(cancellationToken);
        var active = await staffMembers.CountAsync(x => x.IsActive, cancellationToken);
        var onLeave = await context.LeaveRequests.AsNoTracking().CountAsync(x => x.InstitutionId == institutionId
            && x.ApprovedById.HasValue && !x.CancellationDate.HasValue
            && x.FromDate <= today && x.ToDate >= today, cancellationToken);
        return new StaffMemberOverviewDTO(total, active, total - active, onLeave);
    }

    public async Task<IReadOnlyList<DashboardStaffMemberDTO>> GetUpcomingBirthdaysAsync(long institutionId, DateTime today, int days, CancellationToken cancellationToken)
    {
        var staffMemberQuery = ActiveStaffMembers(institutionId)
            .Where(x => x.DateOfBirth.HasValue);
        var staffMembers = await BaseStaffMemberQuery(institutionId, staffMemberQuery)
            .ToListAsync(cancellationToken);
        return staffMembers.Select(x => new { Row = x, Next = NextOccurrence(x.StaffMember.DateOfBirth!.Value, today) })
            .Where(x => x.Next <= today.Date.AddDays(days)).OrderBy(x => x.Next).ThenBy(x => x.Row.StaffMember.FirstName)
            .Select(x => MapStaffMember(x.Row, x.Next)).ToList();
    }

    public async Task<IReadOnlyList<DashboardStaffMemberDTO>> GetRecentOnboardingAsync(long institutionId, int limit, CancellationToken cancellationToken)
    {
        var staffMemberQuery = ActiveStaffMembers(institutionId)
            .Where(x => x.DateOfOnBoarding.HasValue)
            .OrderByDescending(x => x.DateOfOnBoarding)
            .Take(limit);
        var rows = await BaseStaffMemberQuery(institutionId, staffMemberQuery)
            .ToListAsync(cancellationToken);
        return rows.Select(x => MapStaffMember(x, x.StaffMember.DateOfOnBoarding)).ToList();
    }

    public async Task<IReadOnlyList<DashboardStaffMemberDTO>> GetRecentOnboardingAsync(
        long institutionId,
        DateTime fromDate,
        int limit,
        CancellationToken cancellationToken)
    {
        var staffMemberQuery = ActiveStaffMembers(institutionId)
            .Where(x => x.DateOfOnBoarding.HasValue &&
                x.DateOfOnBoarding.Value >= fromDate)
            .OrderByDescending(x => x.DateOfOnBoarding)
            .Take(limit);
        var rows = await BaseStaffMemberQuery(institutionId, staffMemberQuery)
            .ToListAsync(cancellationToken);
        return rows.Select(x => MapStaffMember(x, x.StaffMember.DateOfOnBoarding)).ToList();
    }

    public async Task<IReadOnlyList<RecentExitedStaffMemberDTO>> GetRecentExitedStaffMembersAsync(
        long institutionId,
        DateTime fromDate,
        DateTime throughDate,
        int limit,
        CancellationToken cancellationToken)
    {
        var rows = await (from staffMember in context.StaffMembers.AsNoTracking()
                          join department in context.Departments.AsNoTracking()
                                  .Where(x => x.InstitutionId == institutionId && !x.IsSoftDeleted)
                              on staffMember.DepartmentId equals (int?)department.Id into departments
                          from department in departments.DefaultIfEmpty()
                          join designation in context.Designations.AsNoTracking()
                                  .Where(x => x.InstitutionId == institutionId && !x.IsSoftDeleted)
                              on staffMember.DesignationId equals (int?)designation.Id into designations
                          from designation in designations.DefaultIfEmpty()
                          where staffMember.InstitutionId == institutionId &&
                              !staffMember.IsSoftDeleted &&
                              staffMember.DateOfExit.HasValue &&
                              staffMember.DateOfExit.Value >= fromDate &&
                              staffMember.DateOfExit.Value <= throughDate
                          orderby staffMember.DateOfExit descending
                          select new
                          {
                              staffMember.Id,
                              staffMember.FirstName,
                              staffMember.MiddleName,
                              staffMember.LastName,
                              DepartmentName = department == null ? null : department.DepartmentName,
                              DesignationName = designation == null ? null : designation.DesignationName,
                              DateOfExit = staffMember.DateOfExit!.Value
                          })
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows.Select(row => new RecentExitedStaffMemberDTO(
            Encode(row.Id),
            FullName(row.FirstName, row.MiddleName, row.LastName),
            row.DepartmentName,
            row.DesignationName,
            row.DateOfExit)).ToList();
    }

    public async Task<IReadOnlyList<DepartmentHeadcountDTO>> GetDepartmentHeadcountAsync(
        long institutionId,
        CancellationToken cancellationToken)
    {
        return await context.Departments.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted)
            .GroupJoin(context.StaffMembers.AsNoTracking().Where(x => x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted),
                department => department.Id,
                staffMember => staffMember.DepartmentId,
                (department, staffMembers) => new DepartmentHeadcountDTO(
                    department.Id,
                    department.DepartmentName,
                    staffMembers.Count()))
            .OrderByDescending(x => x.Headcount).ThenBy(x => x.DepartmentName).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DashboardLeaveDTO>> GetCurrentlyOnLeaveAsync(long institutionId, DateOnly today, CancellationToken cancellationToken)
    {
        var rows = await (from leave in context.LeaveRequests.AsNoTracking()
                          join staffMember in context.StaffMembers.AsNoTracking() on leave.StaffMemberId equals staffMember.Id
                          join department in context.Departments.AsNoTracking().Where(x => x.InstitutionId == institutionId && !x.IsSoftDeleted)
                              on staffMember.DepartmentId equals (int?)department.Id into departments
                          from department in departments.DefaultIfEmpty()
                          where leave.InstitutionId == institutionId && staffMember.InstitutionId == institutionId && !staffMember.IsSoftDeleted
                              && leave.ApprovedById.HasValue && !leave.CancellationDate.HasValue
                              && leave.FromDate <= today && leave.ToDate >= today
                          orderby leave.ToDate, staffMember.FirstName
                          select new { StaffMember = staffMember, DepartmentName = department == null ? null : department.DepartmentName, leave.FromDate, leave.ToDate }).ToListAsync(cancellationToken);
        return rows.Select(x => new DashboardLeaveDTO(
            Encode(x.StaffMember.Id),
            FullName(x.StaffMember.FirstName, x.StaffMember.MiddleName, x.StaffMember.LastName),
            x.DepartmentName,
            x.FromDate,
            x.ToDate,
            x.StaffMember.MobileNumber)).ToList();
    }

    public async Task<LocationOverviewDTO> GetLocationsAsync(long institutionId, int limit, CancellationToken cancellationToken)
    {
        var query = context.Campuss.AsNoTracking().Where(x => x.InstitutionId == institutionId && !x.IsSoftDeleted);
        var total = await query.CountAsync(cancellationToken);
        var active = await query.CountAsync(x => x.IsActive, cancellationToken);
        var headOffice = await query.CountAsync(x => x.IsHeadOffice, cancellationToken);
        var rows = await query.OrderByDescending(x => x.IsHeadOffice).ThenBy(x => x.LocationName).Take(limit)
            .Select(x => new DashboardLocationDTO(x.Id, x.LocationCode, x.LocationName, x.LocationType, x.IsHeadOffice, x.IsActive)).ToListAsync(cancellationToken);
        return new LocationOverviewDTO(total, active, headOffice, rows);
    }

    private IQueryable<skolenza.domain.Entity.StaffMember> ActiveStaffMembers(long institutionId) =>
        context.StaffMembers.AsNoTracking()
            .Where(staffMember => staffMember.InstitutionId == institutionId &&
                staffMember.IsActive &&
                !staffMember.IsSoftDeleted);

    private IQueryable<StaffMemberDashboardRow> BaseStaffMemberQuery(
        long institutionId,
        IQueryable<skolenza.domain.Entity.StaffMember> staffMembers) =>
        from staffMember in staffMembers
        join department in context.Departments.AsNoTracking().Where(x => x.InstitutionId == institutionId && !x.IsSoftDeleted)
            on staffMember.DepartmentId equals (int?)department.Id into departments
        from department in departments.DefaultIfEmpty()
        join designation in context.Designations.AsNoTracking().Where(x => x.InstitutionId == institutionId && !x.IsSoftDeleted)
            on staffMember.DesignationId equals (int?)designation.Id into designations
        from designation in designations.DefaultIfEmpty()
        let roleName = (from userRole in context.UserRoles.AsNoTracking()
                        join role in context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                        where userRole.StaffMemberId == staffMember.Id && userRole.IsActive && userRole.IsSoftDeleted != true
                            && role.InstitutionId == institutionId && role.IsActive && role.IsSoftDeleted != true
                        orderby userRole.IsPrimaryRole descending, userRole.Id
                        select role.RoleName).FirstOrDefault()
        select new StaffMemberDashboardRow
        {
            StaffMember = staffMember,
            DepartmentName = department == null ? null : department.DepartmentName,
            DesignationName = designation == null ? null : designation.DesignationName,
            RoleName = roleName
        };

    private DashboardStaffMemberDTO MapStaffMember(StaffMemberDashboardRow row, DateTime? date)
    {
        return new DashboardStaffMemberDTO(
            Encode(row.StaffMember.Id),
            FullName(row.StaffMember.FirstName, row.StaffMember.MiddleName, row.StaffMember.LastName),
            row.DepartmentName,
            row.DesignationName,
            row.RoleName,
            date);
    }

    private string Encode(long id)
    {
        return idEncoderService.EncodeId_long(id, string.Empty);
    }

    private static string FullName(string? first, string? middle, string? last)
    {
        return string.Join(' ', new[] { first, middle, last }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static DateTime NextOccurrence(DateTime birthday, DateTime today)
    {
        var day = Math.Min(birthday.Day, DateTime.DaysInMonth(today.Year, birthday.Month));
        var next = new DateTime(today.Year, birthday.Month, day);
        if (next < today.Date)
        {
            var year = today.Year + 1;
            next = new DateTime(
                year,
                birthday.Month,
                Math.Min(birthday.Day, DateTime.DaysInMonth(year, birthday.Month)));
        }

        return next;
    }
    private sealed class StaffMemberDashboardRow
    {
        public required skolenza.domain.Entity.StaffMember StaffMember { get; init; }
        public string? DepartmentName { get; init; }
        public string? DesignationName { get; init; }
        public string? RoleName { get; init; }
    }
}
