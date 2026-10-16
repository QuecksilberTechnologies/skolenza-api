using System.ComponentModel.DataAnnotations;
using System.Globalization;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.Common.Models.Security;
using skolenza.application.Constants;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.application.Exceptions;
using skolenza.domain.Entity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace skolenza.persistance.Repositories;

public sealed partial class BulkImportRepository
{
    #region StaffMember Preview

    public async Task<BulkImportPreviewResponseDTO> PreviewStaffMembersAsync(
        BulkImportTableDTO table, string? mappingJson, CommonDecodedResult actor,
        CancellationToken cancellationToken)
    {
        if (!actor.Success || actor.InstitutionId <= 0 || actor.LoggedInStaffMemberId <= 0)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }
        var mapping = BulkImportPreviewService.ResolveColumnMapping(table, mappingJson, BulkImportConstants.StaffMemberColumns);
        var preview = new BulkImportPreviewResponseDTO
        {
            Master = BulkImportMaster.StaffMember,
            SourceColumns = table.Columns.ToList(),
            ColumnMapping = mapping
        };
        var required = typeof(CreateBaseStaffMemberRequestDTO).GetProperties()
            .Where(property => Attribute.IsDefined(property, typeof(RequiredAttribute)))
            .Select(property => property.Name)
            .Concat(new[] { nameof(StaffMember.DateOfOnBoarding), nameof(StaffMember.HasPermanent), nameof(StaffMember.IsActive) });
        foreach (var field in required.Where(field => !mapping.ContainsKey(field)))
        {
            preview.Errors.Add($"Map the required column {field}.");
        }
        foreach (var source in table.Columns.Where(column => !mapping.Values.Contains(column)))
        {
            preview.Errors.Add($"Map or remove the unrecognized source column {source}; StaffMember data is never silently discarded.");
        }
        foreach (var source in table.Rows)
        {
            preview.Rows.Add(new BulkImportPreviewRowDTO
            {
                RowNumber = source.RowNumber,
                Values = mapping.ToDictionary(pair => pair.Key,
                    pair => source.Values[table.Columns.IndexOf(pair.Value)].Trim()),
                Status = BulkImportRowStatus.Ready
            });
        }
        var snapshot = await StaffMemberSnapshotAsync(actor.InstitutionId, preview.Rows, cancellationToken);
        if (snapshot.Pattern is null)
        {
            preview.Errors.Add("Configure exactly one active staffMember-code pattern and review existing staffMember changes first.");
        }
        else
        {
            preview.StaffMemberPatternHash = PatternHash(actor.InstitutionId, snapshot.Pattern);
        }
        foreach (var row in preview.Rows)
        {
            ValidateStaffMemberRow(row, snapshot, table.IsExcel, table.Uses1904DateSystem);
        }
        foreach (var field in new[] { nameof(StaffMember.OfficialEmail), BulkImportConstants.StaffMemberCode })
        {
            foreach (var duplicate in preview.Rows.Where(row => !string.IsNullOrWhiteSpace(row.Values.GetValueOrDefault(field)))
                         .GroupBy(row => row.Values[field], StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
            {
                foreach (var row in duplicate)
                {
                    row.Errors.Add($"Duplicate {field} in the uploaded file.");
                    row.Status = BulkImportRowStatus.Invalid;
                }
            }
        }
        if (snapshot.Pattern is not null)
        {
            AssignPreviewCodes(preview, snapshot);
        }
        if (snapshot.AvailableSeats < preview.ReadyCount)
        {
            preview.Errors.Add($"Subscription has {Math.Max(0, snapshot.AvailableSeats)} available seats; {preview.ReadyCount} new staffMembers requested. Admin counts toward MaxUsers.");
        }
        return preview;
    }

    private void AssignPreviewCodes(BulkImportPreviewResponseDTO preview, StaffMemberSnapshot snapshot)
    {
        var pattern = snapshot.Pattern!;
        var last = pattern.LastUsedNumber;
        foreach (var staffMember in snapshot.StaffMembers)
        {
            try
            {
                if (StaffMemberCodePatternFormatter.TryGetRunningNumber(pattern, staffMember.DateOfOnBoarding ?? default,
                    staffMember.DepartmentId, staffMember.EmployementCode, out var number))
                {
                    last = Math.Max(last, number);
                }
            }
            catch (ArgumentException)
            {
                // Old records are never repaired by import. The persisted counter still protects issued numbers.
            }
        }
        foreach (var row in preview.Rows.Where(row => row.Status == BulkImportRowStatus.Ready))
        {
            var staffMember = StaffMemberImportRowMapper.Map(row).StaffMember;
            if (StaffMemberCodePatternFormatter.TryGetRunningNumber(pattern, staffMember.DateOfOnBoarding!.Value,
                staffMember.DepartmentId, row.Values.GetValueOrDefault(BulkImportConstants.StaffMemberCode), out var number))
            {
                row.ProposedStaffMemberCode = row.Values[BulkImportConstants.StaffMemberCode];
                last = Math.Max(last, number);
            }
        }
        foreach (var row in preview.Rows.Where(row => row.Status == BulkImportRowStatus.Ready))
        {
            var staffMember = StaffMemberImportRowMapper.Map(row).StaffMember;
            try
            {
                row.ProposedStaffMemberCode ??= StaffMemberCodePatternFormatter.Format(pattern,
                    staffMember.DateOfOnBoarding!.Value, staffMember.DepartmentId, checked(++last));
                if (row.ProposedStaffMemberCode.Length > 50 || snapshot.StaffMembers.Any(item =>
                    string.Equals(item.EmployementCode, row.ProposedStaffMemberCode, StringComparison.OrdinalIgnoreCase)))
                {
                    row.Errors.Add("Proposed staffMember code conflicts with an existing record or exceeds its field length.");
                    row.Status = BulkImportRowStatus.Invalid;
                }
            }
            catch (OverflowException)
            {
                row.Errors.Add("StaffMember code sequence is exhausted.");
                row.Status = BulkImportRowStatus.Invalid;
            }
        }
        preview.ReservedStaffMemberSequence = last;
    }

    #endregion

    #region Institution References And Capacity

    private sealed record StaffMemberSnapshot(
        long InstitutionId, StaffMemberCodePattern? Pattern, List<StaffMember> StaffMembers, List<LoginCredential> Logins,
        List<Department> Departments, List<InstitutionStaffCategoryMaster> Categories,
        List<StaffPositionMaster> Positions, List<StaffEmploymentType> Types,
        List<Role> Roles, List<Country> Countries, List<Gender> Genders, List<State> States,
        List<District> Districts, int AvailableSeats);

    private async Task<StaffMemberSnapshot> StaffMemberSnapshotAsync(
        long institutionId, IReadOnlyList<BulkImportPreviewRowDTO> rows, CancellationToken token)
    {
        var patterns = await context.StaffMemberCodePatterns.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && item.IsActive).ToListAsync(token);
        var staffMembers = await context.StaffMembers.AsNoTracking().Where(item => item.InstitutionId == institutionId).ToListAsync(token);
        var emails = rows.Select(row => row.Values.GetValueOrDefault(nameof(StaffMember.OfficialEmail), string.Empty).ToUpperInvariant()).Distinct().ToList();
        var logins = await context.LoginCredentials.AsNoTracking().Where(item => emails.Contains(item.LoginId.ToUpper()))
            .Select(item => new LoginCredential { StaffMemberId = item.StaffMemberId, InstitutionId = item.InstitutionId, LoginId = item.LoginId, IsSoftDeleted = item.IsSoftDeleted }).ToListAsync(token);
        var departments = await context.Departments.AsNoTracking().Where(item => item.InstitutionId == institutionId && item.IsActive == true && item.IsSoftDeleted != true).ToListAsync(token);
        var categories = await context.InstitutionStaffCategoryMasters.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted).ToListAsync(token);
        var positions = await context.StaffPositionMasters.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted).ToListAsync(token);
        var types = await context.StaffEmploymentTypes.AsNoTracking().Where(item => item.InstitutionId == institutionId && item.IsActive == true && item.IsSoftDeleted != true).ToListAsync(token);
        var roles = await context.Roles.AsNoTracking().Where(item => item.InstitutionId == institutionId && item.IsActive && item.IsSoftDeleted != true).ToListAsync(token);
        var countries = await context.Countries.AsNoTracking().Where(item => item.IsActive == true).ToListAsync(token);
        var genders = await context.Genders.AsNoTracking().ToListAsync(token);
        var states = await context.States.AsNoTracking().Where(item => item.IsActive == true).ToListAsync(token);
        var districts = await context.Districts.AsNoTracking().Where(item => item.IsActive).ToListAsync(token);
        var available = await StaffMemberCapacityGuard.AvailableSeatsAsync(context, institutionId, token);
        return new StaffMemberSnapshot(institutionId, patterns.Count == 1 ? patterns[0] : null, staffMembers, logins,
            departments, categories, positions, types, roles, countries, genders, states, districts, available);
    }

    private void ValidateStaffMemberRow(BulkImportPreviewRowDTO row, StaffMemberSnapshot snapshot, bool excel = false, bool date1904 = false)
    {
        var (dto, contactDto) = StaffMemberImportRowMapper.Map(row, excel, date1904);
        if (row.Values.GetValueOrDefault(BulkImportConstants.StaffMemberCode, string.Empty).Length > 50)
        {
            row.Errors.Add("StaffMemberCode exceeds the 50-character field limit.");
        }
        if (!snapshot.Departments.Any(item => item.Id == dto.DepartmentId))
            row.Errors.Add("DepartmentId must be active in this institution.");
        if (!snapshot.Categories.Any(item => item.Id == dto.InstitutionStaffCategoryMasterId))
            row.Errors.Add("InstitutionStaffCategoryMasterId must be active in this institution.");
        if (!snapshot.Positions.Any(item => item.Id == dto.StaffPositionMasterId &&
            item.InstitutionStaffCategoryMasterId == dto.InstitutionStaffCategoryMasterId &&
            (!item.DepartmentId.HasValue || item.DepartmentId == dto.DepartmentId)))
            row.Errors.Add("StaffPositionMasterId must be active in this institution and match the category and department.");
        if (!snapshot.Types.Any(item => item.Id == dto.StaffEmploymentTypeId))
            row.Errors.Add("StaffEmploymentTypeId must be active in this institution.");
        if (dto.RoleId == 0)
        {
            var defaultRole = snapshot.Roles.Where(item => item.RoleType == ConstantValues.RoleTypeStaffMember).OrderBy(item => item.Id).FirstOrDefault();
            dto.RoleId = defaultRole?.Id ?? 0;
            if (dto.RoleId > 0)
                row.Values[nameof(dto.RoleId)] = dto.RoleId.ToString(CultureInfo.InvariantCulture);
        }
        if (!snapshot.Roles.Any(item => item.Id == dto.RoleId))
            row.Errors.Add("RoleId must be active in this institution, or a default StaffMember role must exist.");
        if (!snapshot.Countries.Any(item => item.Id == dto.CountryId))
            row.Errors.Add("CountryId must be active.");
        if (!snapshot.Genders.Any(item => item.Id == dto.GenderId))
            row.Errors.Add("GenderId must exist.");
        var staffMember = mapper.Map<StaffMember>(dto);
        staffMember.MobileNumber = row.Values.GetValueOrDefault(nameof(StaffMember.MobileNumber));
        ValidateLengths(staffMember, row);
        if (contactDto is not null)
        {
            if (contactDto.CountryId.HasValue && !snapshot.Countries.Any(item => item.Id == contactDto.CountryId))
                row.Errors.Add("ContactCountryId must be active.");
            if (contactDto.StateId.HasValue && !snapshot.States.Any(item => item.Id == contactDto.StateId && item.CountryId == contactDto.CountryId))
                row.Errors.Add("StateId must be active and belong to ContactCountryId.");
            if (contactDto.DistrictId.HasValue && !snapshot.Districts.Any(item => item.Id == contactDto.DistrictId && item.StateId == contactDto.StateId))
                row.Errors.Add("DistrictId must be active and belong to StateId.");
            ValidateLengths(mapper.Map<StaffMemberContact>(contactDto), row);
        }
        var existing = snapshot.StaffMembers.Where(item => string.Equals(item.OfficialEmail, dto.OfficialEmail, StringComparison.OrdinalIgnoreCase)).ToList();
        if (existing.Count == 1 && !existing[0].IsSoftDeleted)
        {
            var suppliedCode = row.Values.GetValueOrDefault(BulkImportConstants.StaffMemberCode);
            if (!string.IsNullOrEmpty(suppliedCode) && !string.Equals(suppliedCode, existing[0].EmployementCode, StringComparison.Ordinal))
                row.Errors.Add("Email already belongs to an staffMember with a different code. Existing records are not overwritten.");
            row.ProposedStaffMemberCode = existing[0].EmployementCode;
            row.Status = BulkImportRowStatus.Existing;
        }
        else if (existing.Count > 0 || snapshot.Logins.Any(item => string.Equals(item.LoginId, dto.OfficialEmail, StringComparison.OrdinalIgnoreCase)))
        {
            row.Errors.Add("OfficialEmail is unavailable. Import will not replace an existing account.");
        }
        if (row.Errors.Count > 0)
            row.Status = BulkImportRowStatus.Invalid;
    }

    private void ValidateLengths<TEntity>(TEntity entity, BulkImportPreviewRowDTO row) where TEntity : class
    {
        foreach (var property in context.Model.FindEntityType(typeof(TEntity))!.GetProperties())
        {
            if (property.GetMaxLength() is int max && property.PropertyInfo?.GetValue(entity) is string value && value.Length > max)
                row.Errors.Add($"{property.Name} exceeds its {max}-character field limit.");
        }
    }

    private static string PatternHash(long institutionId, StaffMemberCodePattern pattern)
    {
        return StaffMemberCodePatternFormatter.PreviewChanges(institutionId, pattern, pattern, Array.Empty<StaffMember>()).PreviewHash;
    }

    #endregion

    #region Confirmation And Worker

    private Task LockStaffMemberCodesAsync(long institutionId, CancellationToken token)
    {
        return context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"{BulkImportConstants.StaffMemberCodeLockPrefix}{institutionId}"}, 0))", token);
    }

    private async Task ConfirmStaffMembersAsync(BulkImportJob job, BulkImportPreviewResponseDTO preview, CancellationToken token)
    {
        await LockStaffMemberCodesAsync(job.InstitutionId, token);
        var fresh = await StaffMemberSnapshotAsync(job.InstitutionId, preview.Rows, token);
        if (fresh.Pattern is null || PatternHash(job.InstitutionId, fresh.Pattern) != preview.StaffMemberPatternHash)
            throw new ConflictException("StaffMember code pattern or sequence changed. Review a fresh preview.");
        if (fresh.AvailableSeats < preview.ReadyCount)
            throw new ConflictException("Subscription capacity changed. Review a fresh preview.");
        foreach (var saved in preview.Rows)
        {
            var row = new BulkImportPreviewRowDTO { Values = new(saved.Values), Status = BulkImportRowStatus.Ready };
            ValidateStaffMemberRow(row, fresh);
            if (row.Status != saved.Status || row.Errors.Count > 0 ||
                (saved.Status == BulkImportRowStatus.Ready && fresh.StaffMembers.Any(item => item.EmployementCode == saved.ProposedStaffMemberCode)))
                throw new ConflictException("StaffMember data or references changed. Review a fresh preview.");
        }
        // Reserve the approved range before queueing so a concurrent single-create cannot consume it.
        await context.StaffMemberCodePatterns.Where(item => item.Id == fresh.Pattern.Id).ExecuteUpdateAsync(setters => setters
            .SetProperty(item => item.LastUsedNumber, preview.ReservedStaffMemberSequence ?? fresh.Pattern.LastUsedNumber)
            .SetProperty(item => item.UpdatedDateTime, DateTime.UtcNow), token);
    }

    private async Task ProcessStaffMemberBatchAsync(BulkImportJob job, BulkImportPreviewResponseDTO preview, CancellationToken token)
    {
        await LockStaffMemberCodesAsync(job.InstitutionId, token);
        foreach (var table in new[] { "Department", "StaffEmploymentType", "Role" })
        {
            // Table names are the fixed existing master tables, never request data.
            await context.Database.ExecuteSqlRawAsync(
                $"SELECT \"Id\" FROM skolenza.\"{table}\" WHERE \"InstitutionId\" = {{0}} FOR SHARE",
                new object[] { job.InstitutionId }, token);
        }
        var end = Math.Min(preview.Rows.Count, job.NextRow + Math.Clamp(options.Value.BatchSize, 1, 200));
        // A deterministic order prevents cross-institution batches with reversed email lists from deadlocking.
        foreach (var email in preview.Rows.Skip(job.NextRow).Take(end - job.NextRow)
                     .Select(row => row.Values.GetValueOrDefault(nameof(StaffMember.OfficialEmail), string.Empty).Trim().ToUpperInvariant())
                     .Distinct().OrderBy(value => value, StringComparer.Ordinal))
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({$"{BulkImportConstants.StaffMemberLoginLockPrefix}{email}"}, 0))", token);
        }
        for (var index = job.NextRow; index < end; index++)
        {
            var row = preview.Rows[index];
            if (row.Status is BulkImportRowStatus.Created || row.Processed && row.Status == BulkImportRowStatus.Existing)
            {
                job.NextRow = index + 1;
                continue;
            }
            var approvedCode = row.ProposedStaffMemberCode;
            var fresh = await StaffMemberSnapshotAsync(job.InstitutionId, new[] { row }, token);
            row.Errors.Clear();
            row.Status = BulkImportRowStatus.Ready;
            ValidateStaffMemberRow(row, fresh);
            if (row.Status == BulkImportRowStatus.Ready)
            {
                var dto = StaffMemberImportRowMapper.Map(row).StaffMember;
                if (fresh.Pattern is null || !StaffMemberCodePatternFormatter.TryGetRunningNumber(fresh.Pattern,
                    dto.DateOfOnBoarding!.Value, dto.DepartmentId, approvedCode, out _))
                    row.Errors.Add("Pattern changed after approval. Create a fresh preview for these rows.");
                if (fresh.AvailableSeats <= 0)
                    row.Errors.Add("Subscription MaxUsers capacity is exhausted, including the initial Admin seat.");
                if (fresh.StaffMembers.Any(item => string.Equals(item.EmployementCode, approvedCode, StringComparison.OrdinalIgnoreCase)))
                    row.Errors.Add("Approved staffMember code was taken after preview. Create a fresh preview.");
                if (row.Errors.Count == 0)
                {
                    await context.Database.CurrentTransaction!.CreateSavepointAsync("staffMember_row", token);
                    try
                    {
                        await InsertStaffMemberAsync(job, row, token);
                        row.Status = BulkImportRowStatus.Created;
                    }
                    catch (Exception error) when (error is ValidationErrorException || error is DbUpdateException
                        { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation or PostgresErrorCodes.CheckViolation } })
                    {
                        await context.Database.CurrentTransaction!.RollbackToSavepointAsync("staffMember_row", token);
                        context.ChangeTracker.Clear();
                        row.Errors.Add("Data changed during account creation. Review the row before retrying.");
                    }
                    await context.Database.CurrentTransaction!.ReleaseSavepointAsync("staffMember_row", token);
                }
            }
            if (row.Errors.Count > 0 || row.Status == BulkImportRowStatus.Invalid)
                row.Status = BulkImportRowStatus.Failed;
            row.Processed = true;
            job.NextRow = index + 1;
        }
        job.Status = job.NextRow >= preview.Rows.Count
            ? (int)(preview.Rows.Any(row => row.Status == BulkImportRowStatus.Failed) ? BulkImportJobStatus.CompletedWithErrors : BulkImportJobStatus.Completed)
            : (int)BulkImportJobStatus.Running;
        await SaveProgress(job, preview, token);
    }

    private async Task InsertStaffMemberAsync(BulkImportJob job, BulkImportPreviewRowDTO row, CancellationToken token)
    {
        var (dto, contactDto) = StaffMemberImportRowMapper.Map(row);
        var staffMember = mapper.Map<StaffMember>(dto);
        staffMember.InstitutionId = job.InstitutionId;
        var category = await context.InstitutionStaffCategoryMasters.AsNoTracking()
            .SingleAsync(item => item.Id == dto.InstitutionStaffCategoryMasterId &&
                item.InstitutionId == job.InstitutionId && item.IsActive && !item.IsSoftDeleted, token);
        staffMember.StaffCategoryId = category.StaffCategoryTypeId;
        staffMember.InstitutionStaffCategoryMasterId = category.Id;
        staffMember.EmployementCode = row.ProposedStaffMemberCode;
        staffMember.MobileNumber = row.Values.GetValueOrDefault(nameof(StaffMember.MobileNumber));
        staffMember.AddedById = job.ActorId;
        staffMember.AddedDateTime = DateTime.UtcNow;
        staffMember.IsEditAllowed = true;
        staffMember.IsInfoVerified = false;
        staffMember.IsSoftDeleted = false;
        if (contactDto is not null)
        {
            var contact = mapper.Map<StaffMemberContact>(contactDto);
            contact.StaffMember = staffMember;
            contact.AddedById = job.ActorId;
            contact.AddedDateTime = DateTime.UtcNow;
            contact.IsActive = true;
            contact.IsSoftDeleted = false;
            contact.IsEditAllowed = true;
            contact.IsInfoVerified = false;
            staffMember.StaffMemberContact.Add(contact);
        }
        var login = new LoginCredential
        {
            StaffMember = staffMember, InstitutionId = job.InstitutionId, LoginId = dto.OfficialEmail,
            HasFirstLogin = true, IsPasswordChangeRequired = true, IsActive = dto.IsActive,
            AddedById = job.ActorId, AddedDateTime = DateTime.UtcNow, IsSoftDeleted = false
        };
        var role = new UserRole
        {
            StaffMember = staffMember, RoleId = dto.RoleId, IsActive = true, IsPrimaryRole = true,
            RoleStartDate = staffMember.DateOfOnBoarding, AddedById = job.ActorId, AddedDateTime = DateTime.UtcNow
        };
        await (staffMemberRepository ?? throw new InvalidOperationException("StaffMember repository is not configured."))
            .CreateStaffMemberAsync(staffMember, login, role);
        // Reuse the canonical graph inside the worker's existing row transaction.
        await new StaffProfessionalStructureRepository(context).AssignPrimaryPositionAsync(
            job.InstitutionId, staffMember.Id, dto.StaffPositionMasterId,
            DateOnly.FromDateTime(dto.DateOfOnBoarding!.Value), job.ActorId, token);
        row.ImportedStaffMemberId = staffMember.Id;
        row.InvitationStatus = BulkImportInvitationStatus.Pending;
        token.ThrowIfCancellationRequested();
        // Welcome email is deliberately absent. Invitation dispatch is a separate explicit action.
    }

    #endregion
}
