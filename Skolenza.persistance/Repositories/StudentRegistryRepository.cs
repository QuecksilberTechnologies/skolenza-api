using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.Constants;
using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Student;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Repositories;

public sealed class StudentRegistryRepository(
    SkolenzaDbContext context,
    IStudentPortalAuthRepository studentPortalAuthRepository) : IStudentRegistryRepository
{
    public async Task<PagedResponseDTO<StudentRecordResponseDTO>> GetStudentsAsync(
        long institutionId,
        StudentRecordFilterDTO filter,
        CancellationToken cancellationToken)
    {
        var query = context.Students.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && !item.IsSoftDeleted);
        if (!filter.IncludeExited)
        {
            query = query.Where(item => item.LifecycleStatus != (short)StudentLifecycleStatus.Exited);
        }

        if (filter.AcademicSessionId.HasValue || filter.CampusId.HasValue || filter.AcademicGroupId.HasValue)
        {
            var enrollmentQuery = context.StudentEnrollments.AsNoTracking()
                .Where(item => item.InstitutionId == institutionId);
            if (filter.AcademicSessionId.HasValue)
            {
                enrollmentQuery = enrollmentQuery.Where(item => item.AcademicSessionId == filter.AcademicSessionId.Value);
            }

            if (filter.CampusId.HasValue)
            {
                enrollmentQuery = enrollmentQuery.Where(item => item.CampusId == filter.CampusId.Value);
            }

            if (filter.AcademicGroupId.HasValue)
            {
                enrollmentQuery = enrollmentQuery.Where(item => item.AcademicGroupId == filter.AcademicGroupId.Value);
            }

            var studentIds = enrollmentQuery.Select(item => item.StudentId).Distinct();
            query = query.Where(item => studentIds.Contains(item.Id));
        }

        var page = await query
            .OrderBy(item => item.StudentCode)
            .ThenBy(item => item.Id)
            .ToPageAsync(filter.PageNumber, filter.PageSize, cancellationToken);
        var studentIdsOnPage = page.Data.Select(item => item.Id).ToArray();
        var currentEnrollments = await context.StudentEnrollments.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && studentIdsOnPage.Contains(item.StudentId))
            .OrderByDescending(item => item.IsActive)
            .ThenByDescending(item => item.EffectiveFrom)
            .ThenByDescending(item => item.Id)
            .ToListAsync(cancellationToken);
        var enrollmentByStudent = currentEnrollments
            .GroupBy(item => item.StudentId)
            .ToDictionary(group => group.Key, group => group.First());
        var rows = page.Data.Select(student =>
        {
            enrollmentByStudent.TryGetValue(student.Id, out var enrollment);
            return new StudentRecordResponseDTO
            {
                Id = student.Id,
                StudentCode = student.StudentCode,
                AdmissionNumber = student.AdmissionNumber,
                FirstName = student.FirstName,
                MiddleName = student.MiddleName,
                LastName = student.LastName,
                DateOfBirth = student.DateOfBirth,
                LifecycleStatus = student.LifecycleStatus,
                CurrentEnrollmentId = enrollment?.Id,
                AcademicSessionId = enrollment?.AcademicSessionId,
                CampusId = enrollment?.CampusId,
                AcademicGroupId = enrollment?.AcademicGroupId
            };
        }).ToList();

        return new PagedResponseDTO<StudentRecordResponseDTO>(rows, page.TotalCount, page.PageNumber, page.PageSize)
        {
            TotalPages = page.TotalPages
        };
    }

    public async Task<PagedResponseDTO<StudentAdmissionResponseDTO>> GetAdmissionsAsync(
        long institutionId,
        StudentAdmissionFilterDTO filter,
        CancellationToken cancellationToken)
    {
        var query = context.StudentAdmissions.AsNoTracking().Where(item => item.InstitutionId == institutionId);
        if (filter.AcademicSessionId.HasValue)
        {
            query = query.Where(item => item.AcademicSessionId == filter.AcademicSessionId.Value);
        }

        if (filter.CampusId.HasValue)
        {
            query = query.Where(item => item.CampusId == filter.CampusId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(item => item.Status == filter.Status.Value);
        }

        var page = await query
            .OrderByDescending(item => item.AddedDateTime)
            .ThenByDescending(item => item.Id)
            .ToPageAsync(filter.PageNumber, filter.PageSize, cancellationToken);
        var studentCodes = await context.Students.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && page.Data.Select(admission => admission.StudentId).Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.StudentCode, cancellationToken);
        var rows = page.Data.Select(item => Map(item, item.StudentId.HasValue && studentCodes.TryGetValue(item.StudentId.Value, out var code) ? code : null)).ToList();

        return new PagedResponseDTO<StudentAdmissionResponseDTO>(rows, page.TotalCount, page.PageNumber, page.PageSize)
        {
            TotalPages = page.TotalPages
        };
    }

    public async Task<StudentAdmissionResponseDTO?> GetAdmissionAsync(
        long institutionId,
        long admissionId,
        CancellationToken cancellationToken)
    {
        var admission = await context.StudentAdmissions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.InstitutionId == institutionId && item.Id == admissionId, cancellationToken);
        if (admission is null)
        {
            return null;
        }

        var studentCode = admission.StudentId.HasValue
            ? await context.Students.AsNoTracking()
                .Where(item => item.InstitutionId == institutionId && item.Id == admission.StudentId.Value)
                .Select(item => item.StudentCode)
                .SingleOrDefaultAsync(cancellationToken)
            : null;

        return Map(admission, studentCode);
    }

    public async Task<PagedResponseDTO<StudentAdmissionStatusHistoryResponseDTO>> GetAdmissionStatusHistoryAsync(
        long institutionId,
        long admissionId,
        StudentAdmissionStatusHistoryFilterDTO filter,
        CancellationToken cancellationToken)
    {
        var admissionExists = await context.StudentAdmissions.AsNoTracking()
            .AnyAsync(item => item.InstitutionId == institutionId && item.Id == admissionId, cancellationToken);
        if (!admissionExists)
        {
            throw new NotFoundException("Student admission was not found.");
        }

        var page = await context.StudentAdmissionStatusHistories.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && item.StudentAdmissionId == admissionId)
            .OrderBy(item => item.ChangedDateTime)
            .ThenBy(item => item.Id)
            .Select(item => new StudentAdmissionStatusHistoryResponseDTO
            {
                Id = item.Id,
                AdmissionId = item.StudentAdmissionId,
                FromStatus = item.FromStatus == 0 ? null : (StudentAdmissionStatus?)item.FromStatus,
                ToStatus = (StudentAdmissionStatus)item.ToStatus,
                Reason = item.Reason,
                ChangedById = item.ChangedById,
                ChangedDateTime = item.ChangedDateTime
            })
            .ToPageAsync(filter.PageNumber, filter.PageSize, cancellationToken);

        return new PagedResponseDTO<StudentAdmissionStatusHistoryResponseDTO>(
            page.Data, page.TotalCount, page.PageNumber, page.PageSize)
        {
            TotalPages = page.TotalPages
        };
    }

    public async Task<StudentAdmissionResponseDTO> CreateAdmissionAsync(
        long institutionId,
        long actorId,
        StudentAdmissionRequestDTO request,
        CancellationToken cancellationToken)
    {
        await ValidateReferencesAsync(institutionId, request, cancellationToken);
        ValidateStudentDetails(request);

        var now = DateTime.UtcNow;
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var entity = new StudentAdmission
        {
            InstitutionId = institutionId,
            ApplicationNumber = CreateApplicationNumber(),
            Status = (short)StudentAdmissionStatus.Draft,
            AddedById = actorId,
            AddedDateTime = now
        };
        Apply(entity, request);
        context.StudentAdmissions.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        context.StudentAdmissionStatusHistories.Add(new StudentAdmissionStatusHistory
        {
            InstitutionId = institutionId,
            StudentAdmissionId = entity.Id,
            FromStatus = 0,
            ToStatus = entity.Status,
            ChangedById = actorId,
            ChangedDateTime = now
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<StudentAdmissionResponseDTO> UpdateAdmissionAsync(
        long institutionId,
        long actorId,
        long admissionId,
        StudentAdmissionRequestDTO request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        // Recheck editability after any competing review transition has finished.
        var entity = await context.StudentAdmissions
            .FromSqlInterpolated($"SELECT * FROM skolenza.\"StudentAdmission\" WHERE \"Id\" = {admissionId} AND \"InstitutionId\" = {institutionId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Student admission was not found.");
        if (entity.Status != (short)StudentAdmissionStatus.Draft &&
            entity.Status != (short)StudentAdmissionStatus.ReturnedForCorrection)
        {
            throw new ConflictException("Only Draft or Returned for Correction admissions can be edited.");
        }

        await ValidateReferencesAsync(institutionId, request, cancellationToken);
        ValidateStudentDetails(request);
        Apply(entity, request);
        entity.UpdatedById = actorId;
        entity.UpdatedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<IReadOnlyList<StudentAdmissionGuardianResponseDTO>> GetAdmissionGuardiansAsync(
        long institutionId,
        long admissionId,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var admissionExists = await context.StudentAdmissions.AsNoTracking().AnyAsync(item =>
            item.Id == admissionId && item.InstitutionId == institutionId, cancellationToken);
        if (!admissionExists)
        {
            throw new KeyNotFoundException("Student admission was not found.");
        }

        var query = from link in context.StudentAdmissionGuardians.AsNoTracking()
                    join guardian in context.Guardians.AsNoTracking() on link.GuardianId equals guardian.Id
                    where link.InstitutionId == institutionId && link.StudentAdmissionId == admissionId && !guardian.IsSoftDeleted
                    select new { Link = link, Guardian = guardian };
        if (!includeInactive)
        {
            query = query.Where(item => item.Link.IsActive && item.Guardian.IsActive);
        }

        var rows = await query.OrderByDescending(item => item.Link.IsPrimary)
            .ThenBy(item => item.Guardian.LastName)
            .ThenBy(item => item.Guardian.FirstName)
            .ToListAsync(cancellationToken);
        return rows.Select(item => Map(item.Link, item.Guardian)).ToList();
    }

    public async Task<StudentAdmissionGuardianResponseDTO> AddAdmissionGuardianAsync(
        long institutionId,
        long actorId,
        long admissionId,
        StudentAdmissionGuardianRequestDTO request,
        CancellationToken cancellationToken)
    {
        ValidateAdmissionGuardian(request);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await GetEditableAdmissionAsync(institutionId, admissionId, cancellationToken);
        var guardian = await context.Guardians.SingleOrDefaultAsync(item =>
            item.Id == request.GuardianId && item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted,
            cancellationToken) ?? throw new KeyNotFoundException("Active Guardian was not found in this institution.");

        await EnsureAdmissionPrimaryAvailableAsync(
            institutionId, admissionId, request.IsPrimary, null, cancellationToken);
        var entity = new StudentAdmissionGuardian
        {
            InstitutionId = institutionId,
            StudentAdmissionId = admissionId,
            GuardianId = guardian.Id,
            RelationshipType = (short)request.RelationshipType,
            IsPrimary = request.IsPrimary,
            IsEmergencyContact = request.IsEmergencyContact,
            IsPickupAuthorized = request.IsPickupAuthorized,
            HasPortalAccess = request.HasPortalAccess,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            IsActive = true,
            AddedById = actorId,
            AddedDateTime = DateTime.UtcNow
        };
        context.StudentAdmissionGuardians.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(entity, guardian);
    }

    public async Task<StudentAdmissionGuardianResponseDTO> UpdateAdmissionGuardianAsync(
        long institutionId,
        long actorId,
        long admissionId,
        long linkId,
        StudentAdmissionGuardianRequestDTO request,
        CancellationToken cancellationToken)
    {
        ValidateAdmissionGuardian(request);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await GetEditableAdmissionAsync(institutionId, admissionId, cancellationToken);
        var entity = await context.StudentAdmissionGuardians.SingleOrDefaultAsync(item =>
            item.Id == linkId && item.InstitutionId == institutionId && item.StudentAdmissionId == admissionId && item.IsActive,
            cancellationToken) ?? throw new KeyNotFoundException("Active admission Guardian link was not found.");
        if (request.GuardianId != entity.GuardianId)
        {
            throw new ValidationErrorException("GuardianId cannot be changed on an existing link; close it and add a new link instead.");
        }

        var guardian = await context.Guardians.SingleAsync(item =>
            item.Id == entity.GuardianId && item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted,
            cancellationToken);
        await EnsureAdmissionPrimaryAvailableAsync(
            institutionId, admissionId, request.IsPrimary, linkId, cancellationToken);
        entity.RelationshipType = (short)request.RelationshipType;
        entity.IsPrimary = request.IsPrimary;
        entity.IsEmergencyContact = request.IsEmergencyContact;
        entity.IsPickupAuthorized = request.IsPickupAuthorized;
        entity.HasPortalAccess = request.HasPortalAccess;
        entity.EffectiveFrom = request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo;
        entity.UpdatedById = actorId;
        entity.UpdatedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(entity, guardian);
    }

    public async Task RemoveAdmissionGuardianAsync(
        long institutionId,
        long admissionId,
        long linkId,
        DateOnly effectiveTo,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await GetEditableAdmissionAsync(institutionId, admissionId, cancellationToken);
        var entity = await context.StudentAdmissionGuardians.SingleOrDefaultAsync(item =>
            item.Id == linkId && item.InstitutionId == institutionId && item.StudentAdmissionId == admissionId && item.IsActive,
            cancellationToken) ?? throw new KeyNotFoundException("Active admission Guardian link was not found.");
        if (effectiveTo < entity.EffectiveFrom)
        {
            throw new ValidationErrorException("Guardian link end date cannot precede its start date.");
        }

        entity.IsActive = false;
        entity.IsPrimary = false;
        entity.HasPortalAccess = false;
        entity.EffectiveTo = effectiveTo;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<StudentAdmissionResponseDTO> ReviewAdmissionAsync(
        long institutionId,
        long actorId,
        long admissionId,
        StudentAdmissionReviewDTO request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Status))
        {
            throw new ValidationErrorException("Admission status is invalid.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        // Serialize competing transitions before checking the current status.
        var entity = await context.StudentAdmissions
            .FromSqlInterpolated($"SELECT * FROM skolenza.\"StudentAdmission\" WHERE \"Id\" = {admissionId} AND \"InstitutionId\" = {institutionId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Student admission was not found.");
        var current = (StudentAdmissionStatus)entity.Status;
        EnsureTransition(current, request.Status, request.Reason);

        var now = DateTime.UtcNow;
        if (request.Status == StudentAdmissionStatus.Approved)
        {
            if (entity.StudentId.HasValue)
            {
                throw new ConflictException("This admission already created a Student record.");
            }

            await ValidateAdmissionReadyForApprovalAsync(institutionId, entity, cancellationToken);

            var group = await context.AcademicGroups.AsNoTracking()
                .SingleAsync(item => item.Id == entity.AcademicGroupId && item.InstitutionId == institutionId, cancellationToken);
            var student = new Student
            {
                InstitutionId = institutionId,
                StudentCode = $"{StudentConstants.StudentCodePrefix}-{entity.ApplicationNumber}",
                FirstName = entity.FirstName,
                MiddleName = entity.MiddleName,
                LastName = entity.LastName,
                DateOfBirth = entity.DateOfBirth,
                GenderId = entity.GenderId,
                CountryId = entity.CountryId,
                LifecycleStatus = (short)StudentLifecycleStatus.Active,
                IsActive = true,
                AddedById = actorId,
                AddedDateTime = now
            };
            context.Students.Add(student);
            await context.SaveChangesAsync(cancellationToken);

            var session = await context.AcademicSessions.AsNoTracking()
                .SingleAsync(item => item.Id == entity.AcademicSessionId && item.InstitutionId == institutionId, cancellationToken);
            context.StudentEnrollments.Add(new StudentEnrollment
            {
                InstitutionId = institutionId,
                StudentId = student.Id,
                AcademicSessionId = entity.AcademicSessionId,
                CampusId = entity.CampusId,
                AcademicGroupId = entity.AcademicGroupId,
                AcademicProgrammeId = group.AcademicProgrammeId,
                Status = (short)StudentEnrollmentStatus.Active,
                EffectiveFrom = session.StartDate,
                IsActive = true,
                AddedById = actorId,
                AddedDateTime = now
            });

            var admissionGuardians = await context.StudentAdmissionGuardians
                .Where(item => item.InstitutionId == institutionId && item.StudentAdmissionId == entity.Id && item.IsActive)
                .ToListAsync(cancellationToken);
            var guardianIds = admissionGuardians.Select(item => item.GuardianId).Distinct().ToArray();
            var activeGuardianIds = await context.Guardians.AsNoTracking()
                .Where(item => item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted && guardianIds.Contains(item.Id))
                .Select(item => item.Id)
                .ToListAsync(cancellationToken);
            if (activeGuardianIds.Count != guardianIds.Length)
            {
                throw new ConflictException("An admission Guardian is no longer active in this institution; update the admission before approval.");
            }

            context.StudentGuardians.AddRange(admissionGuardians.Select(link => new StudentGuardian
            {
                InstitutionId = institutionId,
                StudentId = student.Id,
                GuardianId = link.GuardianId,
                RelationshipType = link.RelationshipType,
                IsPrimary = link.IsPrimary,
                IsEmergencyContact = link.IsEmergencyContact,
                IsPickupAuthorized = link.IsPickupAuthorized,
                HasPortalAccess = link.HasPortalAccess,
                EffectiveFrom = link.EffectiveFrom,
                EffectiveTo = link.EffectiveTo,
                IsActive = true,
                AddedById = actorId,
                AddedDateTime = now
            }));
            var primaryGuardianId = admissionGuardians.Single(item => item.IsPrimary).GuardianId;
            await studentPortalAuthRepository.QueueGuardianInvitationAsync(
                institutionId, primaryGuardianId, now, cancellationToken);
            entity.StudentId = student.Id;
        }

        entity.Status = (short)request.Status;
        entity.ReviewReason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        entity.UpdatedById = actorId;
        entity.UpdatedDateTime = now;
        context.StudentAdmissionStatusHistories.Add(new StudentAdmissionStatusHistory
        {
            InstitutionId = institutionId,
            StudentAdmissionId = entity.Id,
            FromStatus = (short)current,
            ToStatus = (short)request.Status,
            Reason = entity.ReviewReason,
            ChangedById = actorId,
            ChangedDateTime = now
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var studentCode = entity.StudentId.HasValue
            ? await context.Students.AsNoTracking().Where(item => item.Id == entity.StudentId.Value).Select(item => item.StudentCode).SingleAsync(cancellationToken)
            : null;
        return Map(entity, studentCode);
    }

    private async Task ValidateAdmissionReadyForApprovalAsync(
        long institutionId,
        StudentAdmission admission,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var guardians = await context.StudentAdmissionGuardians.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId &&
                           item.StudentAdmissionId == admission.Id &&
                           item.IsActive &&
                           item.EffectiveFrom <= today &&
                           (item.EffectiveTo == null || item.EffectiveTo >= today))
            .Select(item => new { item.GuardianId, item.IsPrimary })
            .ToListAsync(cancellationToken);
        if (guardians.Count == 0 || guardians.Count(item => item.IsPrimary) != 1)
        {
            throw new ConflictException("Admission approval requires exactly one active primary Guardian link.");
        }

        var guardianIds = guardians.Select(item => item.GuardianId).Distinct().ToArray();
        var activeGuardianCount = await context.Guardians.AsNoTracking()
            .CountAsync(item => item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted && guardianIds.Contains(item.Id), cancellationToken);
        if (activeGuardianCount != guardianIds.Length)
        {
            throw new ConflictException("An admission Guardian is no longer active in this institution; update the admission before approval.");
        }

        var requirements = await context.StudentDocumentRequirementMasters.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId &&
                           !item.IsSoftDeleted &&
                           item.IsActive &&
                           item.IsRequired &&
                           (item.CampusId == null || item.CampusId == admission.CampusId) &&
                           (item.AcademicSessionId == null || item.AcademicSessionId == admission.AcademicSessionId))
            .ToListAsync(cancellationToken);
        var guardianScopeRequired = requirements.Where(item => item.AppliesTo == (short)StudentDocumentPersonScope.Guardian ||
                                                               item.AppliesTo == (short)StudentDocumentPersonScope.StudentAndGuardian).ToArray();
        var studentScopeRequired = requirements.Where(item => item.AppliesTo == (short)StudentDocumentPersonScope.Student ||
                                                              item.AppliesTo == (short)StudentDocumentPersonScope.StudentAndGuardian).ToArray();
        foreach (var requirement in studentScopeRequired)
        {
            if (!await HasCurrentApprovedDocumentAsync(institutionId, admission.Id, requirement.Id, null, today, cancellationToken))
            {
                throw new ConflictException($"Required Student document '{requirement.Name}' must have a current approved version before admission approval.");
            }
        }

        foreach (var requirement in guardianScopeRequired)
        {
            foreach (var guardianId in guardianIds)
            {
                if (!await HasCurrentApprovedDocumentAsync(institutionId, admission.Id, requirement.Id, guardianId, today, cancellationToken))
                {
                    throw new ConflictException($"Required Guardian document '{requirement.Name}' must have a current approved version for every linked Guardian before admission approval.");
                }
            }
        }

        var quote = await context.StudentAdmissionFeeQuotes.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && item.StudentAdmissionId == admission.Id)
            .Select(item => new { item.Id, item.InstallmentsPerYear })
            .SingleOrDefaultAsync(cancellationToken);
        if (quote is null || quote.InstallmentsPerYear <= 0)
        {
            throw new ConflictException("A fee quote and installment schedule are required before admission approval.");
        }

        var installmentCount = await context.StudentFeeInstallments.AsNoTracking()
            .CountAsync(item => item.InstitutionId == institutionId && item.StudentAdmissionFeeQuoteId == quote.Id, cancellationToken);
        if (installmentCount != quote.InstallmentsPerYear)
        {
            throw new ConflictException("The fee quote must have a persisted installment schedule before admission approval.");
        }
    }

    private Task<bool> HasCurrentApprovedDocumentAsync(
        long institutionId,
        long admissionId,
        long requirementId,
        long? guardianId,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var latest = context.StudentAdmissionDocuments.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId &&
                           item.StudentAdmissionId == admissionId &&
                           item.RequirementId == requirementId &&
                           item.GuardianId == guardianId)
            .OrderByDescending(item => item.VersionNumber)
            .ThenByDescending(item => item.Id);

        return latest.Select(item => item.Status == (short)StudentDocumentReviewStatus.Approved &&
                                     (!item.ExpiryDate.HasValue || item.ExpiryDate >= today))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<StudentExitResponseDTO> ExitStudentAsync(
        long institutionId,
        long actorId,
        long studentId,
        StudentExitRequestDTO request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.ExitReason))
        {
            throw new ValidationErrorException("Exit reason is invalid.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var student = await context.Students
            .FromSqlInterpolated($"SELECT * FROM skolenza.\"Student\" WHERE \"Id\" = {studentId} AND \"InstitutionId\" = {institutionId} AND NOT \"IsSoftDeleted\" FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken) ?? throw new KeyNotFoundException("Student was not found.");
        await context.Entry(student).ReloadAsync(cancellationToken);
        if (student.LifecycleStatus == (short)StudentLifecycleStatus.Exited)
        {
            throw new ConflictException("Student exit has already been recorded.");
        }

        var enrollment = await context.StudentEnrollments
            .Where(item => item.StudentId == studentId && item.InstitutionId == institutionId && item.IsActive)
            .OrderByDescending(item => item.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
        if (enrollment is not null && request.EffectiveExitDate < enrollment.EffectiveFrom)
        {
            throw new ValidationErrorException("Exit date cannot precede the active enrollment start date.");
        }

        await ValidateCountryAsync(request.PreviousSchoolCountryId, cancellationToken);
        await ValidateCountryAsync(request.DestinationSchoolCountryId, cancellationToken);
        var now = DateTime.UtcNow;
        var history = new StudentExitHistory
        {
            InstitutionId = institutionId,
            StudentId = studentId,
            StudentEnrollmentId = enrollment?.Id,
            PreviousSchoolName = TrimToNull(request.PreviousSchoolName),
            PreviousSchoolCountryId = request.PreviousSchoolCountryId,
            DestinationSchoolName = TrimToNull(request.DestinationSchoolName),
            DestinationSchoolCountryId = request.DestinationSchoolCountryId,
            EffectiveExitDate = request.EffectiveExitDate,
            ExitReason = (short)request.ExitReason,
            Notes = TrimToNull(request.Notes),
            AddedById = actorId,
            AddedDateTime = now
        };
        context.StudentExitHistories.Add(history);

        if (enrollment is not null)
        {
            enrollment.Status = (short)StudentEnrollmentStatus.Withdrawn;
            enrollment.IsActive = false;
            enrollment.EffectiveTo = request.EffectiveExitDate;
            enrollment.UpdatedById = actorId;
            enrollment.UpdatedDateTime = now;
        }

        student.LifecycleStatus = (short)StudentLifecycleStatus.Exited;
        student.IsActive = false;
        student.UpdatedById = actorId;
        student.UpdatedDateTime = now;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new StudentExitResponseDTO
        {
            StudentId = student.Id,
            ExitHistoryId = history.Id,
            EffectiveExitDate = history.EffectiveExitDate,
            ExitReason = (StudentExitReason)history.ExitReason,
            StudentCode = student.StudentCode
        };
    }

    public async Task<PagedResponseDTO<StudentExitHistoryResponseDTO>> GetExitHistoryAsync(
        long institutionId,
        long studentId,
        StudentExitHistoryFilterDTO filter,
        CancellationToken cancellationToken)
    {
        var studentExists = await context.Students.AsNoTracking().AnyAsync(
            item => item.Id == studentId && item.InstitutionId == institutionId && !item.IsSoftDeleted,
            cancellationToken);
        if (!studentExists)
        {
            throw new KeyNotFoundException("Student was not found.");
        }

        var page = await context.StudentExitHistories.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && item.StudentId == studentId)
            .OrderByDescending(item => item.EffectiveExitDate)
            .ThenByDescending(item => item.Id)
            .ToPageAsync(filter.PageNumber, filter.PageSize, cancellationToken);
        var rows = page.Data.Select(item => new StudentExitHistoryResponseDTO
        {
            Id = item.Id,
            StudentEnrollmentId = item.StudentEnrollmentId,
            PreviousSchoolName = item.PreviousSchoolName,
            PreviousSchoolCountryId = item.PreviousSchoolCountryId,
            DestinationSchoolName = item.DestinationSchoolName,
            DestinationSchoolCountryId = item.DestinationSchoolCountryId,
            EffectiveExitDate = item.EffectiveExitDate,
            ExitReason = (StudentExitReason)item.ExitReason,
            Notes = item.Notes,
            AddedDateTime = item.AddedDateTime
        }).ToList();

        return new PagedResponseDTO<StudentExitHistoryResponseDTO>(rows, page.TotalCount, page.PageNumber, page.PageSize)
        {
            TotalPages = page.TotalPages
        };
    }

    public async Task<PagedResponseDTO<StudentGuardianProfileResponseDTO>> GetGuardiansAsync(
        long institutionId,
        StudentGuardianFilterDTO filter,
        CancellationToken cancellationToken)
    {
        var query = context.Guardians.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && !item.IsSoftDeleted);
        if (filter.ActiveOnly)
        {
            query = query.Where(item => item.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLowerInvariant();
            query = query.Where(item => item.FirstName.ToLower().Contains(search) ||
                item.LastName.ToLower().Contains(search) ||
                (item.Email != null && item.Email.ToLower().Contains(search)) ||
                (item.PhoneNumber != null && item.PhoneNumber.Contains(filter.Search.Trim())));
        }

        var page = await query.OrderBy(item => item.LastName).ThenBy(item => item.FirstName).ThenBy(item => item.Id)
            .ToPageAsync(filter.PageNumber, filter.PageSize, cancellationToken);
        return new PagedResponseDTO<StudentGuardianProfileResponseDTO>(
            page.Data.Select(Map).ToList(), page.TotalCount, page.PageNumber, page.PageSize)
        {
            TotalPages = page.TotalPages
        };
    }

    public async Task<StudentGuardianProfileResponseDTO> CreateGuardianAsync(
        long institutionId,
        long actorId,
        StudentGuardianProfileRequestDTO request,
        CancellationToken cancellationToken)
    {
        ValidateGuardianProfile(request);
        var normalizedEmail = NormalizeEmail(request.Email);
        if (normalizedEmail is not null && await context.Guardians.AnyAsync(item =>
                item.InstitutionId == institutionId && !item.IsSoftDeleted && item.Email != null &&
                item.Email.ToLower() == normalizedEmail, cancellationToken))
        {
            throw new ConflictException("A Guardian with this email already exists in the institution.");
        }

        var now = DateTime.UtcNow;
        var entity = new Guardian
        {
            InstitutionId = institutionId,
            FirstName = request.FirstName.Trim(),
            MiddleName = TrimToNull(request.MiddleName),
            LastName = request.LastName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = TrimToNull(request.PhoneNumber),
            IsActive = true,
            AddedById = actorId,
            AddedDateTime = now
        };
        context.Guardians.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<StudentGuardianProfileResponseDTO> UpdateGuardianAsync(
        long institutionId,
        long actorId,
        long guardianId,
        StudentGuardianProfileRequestDTO request,
        CancellationToken cancellationToken)
    {
        ValidateGuardianProfile(request);
        var entity = await context.Guardians.SingleOrDefaultAsync(item =>
            item.Id == guardianId && item.InstitutionId == institutionId && !item.IsSoftDeleted,
            cancellationToken) ?? throw new KeyNotFoundException("Guardian was not found.");
        var normalizedEmail = NormalizeEmail(request.Email);
        if (normalizedEmail is not null && await context.Guardians.AnyAsync(item =>
                item.Id != guardianId && item.InstitutionId == institutionId && !item.IsSoftDeleted &&
                item.Email != null && item.Email.ToLower() == normalizedEmail, cancellationToken))
        {
            throw new ConflictException("A Guardian with this email already exists in the institution.");
        }

        entity.FirstName = request.FirstName.Trim();
        entity.MiddleName = TrimToNull(request.MiddleName);
        entity.LastName = request.LastName.Trim();
        entity.Email = normalizedEmail;
        entity.PhoneNumber = TrimToNull(request.PhoneNumber);
        entity.UpdatedById = actorId;
        entity.UpdatedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<IReadOnlyList<StudentGuardianLinkResponseDTO>> GetStudentGuardiansAsync(
        long institutionId,
        long studentId,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var studentExists = await context.Students.AsNoTracking().AnyAsync(item =>
            item.Id == studentId && item.InstitutionId == institutionId && !item.IsSoftDeleted,
            cancellationToken);
        if (!studentExists)
        {
            throw new KeyNotFoundException("Student was not found.");
        }

        var query = from link in context.StudentGuardians.AsNoTracking()
                    join guardian in context.Guardians.AsNoTracking() on link.GuardianId equals guardian.Id
                    where link.InstitutionId == institutionId && link.StudentId == studentId && !guardian.IsSoftDeleted
                    select new { Link = link, Guardian = guardian };
        if (!includeInactive)
        {
            query = query.Where(item => item.Link.IsActive && item.Guardian.IsActive);
        }

        var rows = await query.OrderByDescending(item => item.Link.IsPrimary)
            .ThenBy(item => item.Guardian.LastName).ThenBy(item => item.Guardian.FirstName)
            .Select(item => Map(item.Link, item.Guardian)).ToListAsync(cancellationToken);
        return rows;
    }

    public async Task<StudentGuardianLinkResponseDTO> LinkGuardianAsync(
        long institutionId,
        long actorId,
        long studentId,
        StudentGuardianLinkRequestDTO request,
        CancellationToken cancellationToken)
    {
        ValidateGuardianLink(request);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var student = await context.Students
            .FromSqlInterpolated($"SELECT * FROM skolenza.\"Student\" WHERE \"Id\" = {studentId} AND \"InstitutionId\" = {institutionId} AND NOT \"IsSoftDeleted\" FOR UPDATE")
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);
        var guardian = await context.Guardians.SingleOrDefaultAsync(item =>
            item.Id == request.GuardianId && item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted,
            cancellationToken);
        if (student is null || student.LifecycleStatus == (short)StudentLifecycleStatus.Exited || guardian is null)
        {
            throw new KeyNotFoundException("Student or active Guardian was not found in this institution.");
        }

        await EnsurePrimaryAvailableAsync(institutionId, studentId, request.IsPrimary, null, cancellationToken);
        var entity = new StudentGuardian
        {
            InstitutionId = institutionId,
            StudentId = studentId,
            GuardianId = guardian.Id,
            RelationshipType = (short)request.RelationshipType,
            IsPrimary = request.IsPrimary,
            IsEmergencyContact = request.IsEmergencyContact,
            IsPickupAuthorized = request.IsPickupAuthorized,
            HasPortalAccess = request.HasPortalAccess,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            IsActive = true,
            AddedById = actorId,
            AddedDateTime = DateTime.UtcNow
        };
        context.StudentGuardians.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(entity, guardian);
    }

    public async Task<StudentGuardianLinkResponseDTO> UpdateGuardianLinkAsync(
        long institutionId,
        long actorId,
        long studentId,
        long linkId,
        StudentGuardianLinkRequestDTO request,
        CancellationToken cancellationToken)
    {
        ValidateGuardianLink(request);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var student = await context.Students
            .FromSqlInterpolated($"SELECT * FROM skolenza.\"Student\" WHERE \"Id\" = {studentId} AND \"InstitutionId\" = {institutionId} AND NOT \"IsSoftDeleted\" FOR UPDATE")
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);
        if (student is null)
        {
            throw new KeyNotFoundException("Active Guardian link was not found.");
        }

        var entity = await context.StudentGuardians.SingleOrDefaultAsync(item =>
            item.Id == linkId && item.InstitutionId == institutionId && item.StudentId == studentId && item.IsActive,
            cancellationToken) ?? throw new KeyNotFoundException("Active Guardian link was not found.");
        var guardian = await context.Guardians.SingleAsync(item =>
            item.Id == entity.GuardianId && item.InstitutionId == institutionId && !item.IsSoftDeleted,
            cancellationToken);
        if (request.GuardianId != entity.GuardianId)
        {
            throw new ValidationErrorException("GuardianId cannot be changed on an existing link; create a new link instead.");
        }

        await EnsurePrimaryAvailableAsync(institutionId, studentId, request.IsPrimary, linkId, cancellationToken);
        entity.RelationshipType = (short)request.RelationshipType;
        entity.IsPrimary = request.IsPrimary;
        entity.IsEmergencyContact = request.IsEmergencyContact;
        entity.IsPickupAuthorized = request.IsPickupAuthorized;
        entity.HasPortalAccess = request.HasPortalAccess;
        entity.EffectiveFrom = request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(entity, guardian);
    }

    public async Task RemoveGuardianLinkAsync(
        long institutionId,
        long studentId,
        long linkId,
        DateOnly effectiveTo,
        CancellationToken cancellationToken)
    {
        var entity = await context.StudentGuardians.SingleOrDefaultAsync(item =>
            item.Id == linkId && item.InstitutionId == institutionId && item.StudentId == studentId && item.IsActive,
            cancellationToken) ?? throw new KeyNotFoundException("Active Guardian link was not found.");
        if (effectiveTo < entity.EffectiveFrom)
        {
            throw new ValidationErrorException("Guardian link end date cannot precede its start date.");
        }

        entity.IsActive = false;
        entity.IsPrimary = false;
        entity.HasPortalAccess = false;
        entity.EffectiveTo = effectiveTo;
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsurePrimaryAvailableAsync(
        long institutionId,
        long studentId,
        bool isPrimary,
        long? excludingLinkId,
        CancellationToken cancellationToken)
    {
        if (isPrimary && await context.StudentGuardians.AnyAsync(item =>
                item.InstitutionId == institutionId && item.StudentId == studentId && item.IsActive && item.IsPrimary &&
                (!excludingLinkId.HasValue || item.Id != excludingLinkId.Value), cancellationToken))
        {
            throw new ConflictException("This Student already has an active primary Guardian.");
        }
    }

    private async Task EnsureAdmissionPrimaryAvailableAsync(
        long institutionId,
        long admissionId,
        bool isPrimary,
        long? excludingLinkId,
        CancellationToken cancellationToken)
    {
        if (isPrimary && await context.StudentAdmissionGuardians.AnyAsync(item =>
                item.InstitutionId == institutionId && item.StudentAdmissionId == admissionId && item.IsActive && item.IsPrimary &&
                (!excludingLinkId.HasValue || item.Id != excludingLinkId.Value), cancellationToken))
        {
            throw new ConflictException("This admission already has an active primary Guardian.");
        }
    }

    private async Task<StudentAdmission> GetEditableAdmissionAsync(
        long institutionId,
        long admissionId,
        CancellationToken cancellationToken)
    {
        var admission = await context.StudentAdmissions
            .FromSqlInterpolated($"SELECT * FROM skolenza.\"StudentAdmission\" WHERE \"Id\" = {admissionId} AND \"InstitutionId\" = {institutionId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Student admission was not found.");
        if (admission.Status != (short)StudentAdmissionStatus.Draft &&
            admission.Status != (short)StudentAdmissionStatus.ReturnedForCorrection)
        {
            throw new ConflictException("Guardian links can only be changed on Draft or Returned for Correction admissions.");
        }

        return admission;
    }

    private static void ValidateGuardianProfile(StudentGuardianProfileRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new ValidationErrorException("Guardian first name and last name are required.");
        }
    }

    private static void ValidateGuardianLink(StudentGuardianLinkRequestDTO request)
    {
        if (!Enum.IsDefined(request.RelationshipType))
        {
            throw new ValidationErrorException("Guardian relationship type is invalid.");
        }

        if (request.GuardianId <= 0 || request.EffectiveFrom == default ||
            (request.EffectiveTo.HasValue && request.EffectiveTo.Value < request.EffectiveFrom))
        {
            throw new ValidationErrorException("Guardian ID and valid effective dates are required.");
        }
    }

    private static void ValidateAdmissionGuardian(StudentAdmissionGuardianRequestDTO request)
    {
        if (!Enum.IsDefined(request.RelationshipType))
        {
            throw new ValidationErrorException("Guardian relationship type is invalid.");
        }

        if (request.GuardianId <= 0 || request.EffectiveFrom == default ||
            (request.EffectiveTo.HasValue && request.EffectiveTo.Value < request.EffectiveFrom))
        {
            throw new ValidationErrorException("Guardian ID and valid effective dates are required.");
        }
    }

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    private static StudentGuardianProfileResponseDTO Map(Guardian guardian) => new()
    {
        Id = guardian.Id,
        FirstName = guardian.FirstName,
        MiddleName = guardian.MiddleName,
        LastName = guardian.LastName,
        Email = guardian.Email,
        PhoneNumber = guardian.PhoneNumber,
        IsActive = guardian.IsActive
    };

    private static StudentGuardianLinkResponseDTO Map(StudentGuardian link, Guardian guardian) => new()
    {
        Id = link.Id,
        StudentId = link.StudentId,
        Guardian = Map(guardian),
        RelationshipType = (StudentGuardianRelationshipType)link.RelationshipType,
        IsPrimary = link.IsPrimary,
        IsEmergencyContact = link.IsEmergencyContact,
        IsPickupAuthorized = link.IsPickupAuthorized,
        HasPortalAccess = link.HasPortalAccess,
        EffectiveFrom = link.EffectiveFrom,
        EffectiveTo = link.EffectiveTo,
        IsActive = link.IsActive
    };

    private static StudentAdmissionGuardianResponseDTO Map(StudentAdmissionGuardian link, Guardian guardian) => new()
    {
        Id = link.Id,
        StudentAdmissionId = link.StudentAdmissionId,
        Guardian = Map(guardian),
        RelationshipType = (StudentGuardianRelationshipType)link.RelationshipType,
        IsPrimary = link.IsPrimary,
        IsEmergencyContact = link.IsEmergencyContact,
        IsPickupAuthorized = link.IsPickupAuthorized,
        HasPortalAccess = link.HasPortalAccess,
        EffectiveFrom = link.EffectiveFrom,
        EffectiveTo = link.EffectiveTo,
        IsActive = link.IsActive
    };

    private async Task ValidateReferencesAsync(
        long institutionId,
        StudentAdmissionRequestDTO request,
        CancellationToken cancellationToken)
    {
        var valid = await context.AcademicSessions.AnyAsync(item =>
                item.Id == request.AcademicSessionId && item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted,
                cancellationToken) &&
            await context.Campuss.AnyAsync(item =>
                item.Id == request.CampusId && item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted,
                cancellationToken) &&
            await context.AcademicGroups.AnyAsync(item =>
                item.Id == request.AcademicGroupId && item.InstitutionId == institutionId &&
                item.AcademicSessionId == request.AcademicSessionId && item.IsActive && !item.IsSoftDeleted,
                cancellationToken) &&
            await context.Countries.AnyAsync(item => item.Id == request.CountryId, cancellationToken) &&
            await context.Genders.AnyAsync(item => item.Id == request.GenderId, cancellationToken);
        if (!valid)
        {
            throw new ValidationErrorException("One or more selected academic, campus, country, or gender records are unavailable for this institution.");
        }
    }

    private async Task ValidateCountryAsync(int? countryId, CancellationToken cancellationToken)
    {
        if (countryId.HasValue && !await context.Countries.AnyAsync(item => item.Id == countryId, cancellationToken))
        {
            throw new ValidationErrorException("The selected country is unavailable.");
        }
    }

    private static void ValidateStudentDetails(StudentAdmissionRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new ValidationErrorException("Student first name and last name are required.");
        }

        if (request.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ValidationErrorException("Student date of birth cannot be in the future.");
        }
    }

    private static void EnsureTransition(StudentAdmissionStatus current, StudentAdmissionStatus target, string? reason)
    {
        var allowed = (current, target) switch
        {
            (StudentAdmissionStatus.Draft, StudentAdmissionStatus.Submitted) => true,
            (StudentAdmissionStatus.Draft, StudentAdmissionStatus.Cancelled) => true,
            (StudentAdmissionStatus.Submitted, StudentAdmissionStatus.UnderReview) => true,
            (StudentAdmissionStatus.Submitted, StudentAdmissionStatus.Cancelled) => true,
            (StudentAdmissionStatus.UnderReview, StudentAdmissionStatus.Approved) => true,
            (StudentAdmissionStatus.UnderReview, StudentAdmissionStatus.Rejected) => true,
            (StudentAdmissionStatus.UnderReview, StudentAdmissionStatus.ReturnedForCorrection) => true,
            (StudentAdmissionStatus.ReturnedForCorrection, StudentAdmissionStatus.Submitted) => true,
            (StudentAdmissionStatus.ReturnedForCorrection, StudentAdmissionStatus.Cancelled) => true,
            _ => false
        };
        if (!allowed)
        {
            throw new ConflictException($"Admission cannot transition from {current} to {target}.");
        }

        if ((target is StudentAdmissionStatus.Rejected or StudentAdmissionStatus.ReturnedForCorrection or StudentAdmissionStatus.Cancelled) &&
            string.IsNullOrWhiteSpace(reason))
        {
            throw new ValidationErrorException("A reason is required for rejection, return for correction, and cancellation.");
        }
    }

    private static void Apply(StudentAdmission entity, StudentAdmissionRequestDTO request)
    {
        entity.AcademicSessionId = request.AcademicSessionId;
        entity.CampusId = request.CampusId;
        entity.AcademicGroupId = request.AcademicGroupId;
        entity.FirstName = request.FirstName.Trim();
        entity.MiddleName = TrimToNull(request.MiddleName);
        entity.LastName = request.LastName.Trim();
        entity.DateOfBirth = request.DateOfBirth;
        entity.GenderId = request.GenderId;
        entity.CountryId = request.CountryId;
    }

    private static StudentAdmissionResponseDTO Map(StudentAdmission entity, string? studentCode = null) => new()
    {
        Id = entity.Id,
        ApplicationNumber = entity.ApplicationNumber,
        AcademicSessionId = entity.AcademicSessionId,
        CampusId = entity.CampusId,
        AcademicGroupId = entity.AcademicGroupId,
        FirstName = entity.FirstName,
        MiddleName = entity.MiddleName,
        LastName = entity.LastName,
        DateOfBirth = entity.DateOfBirth,
        GenderId = entity.GenderId,
        CountryId = entity.CountryId,
        Status = (StudentAdmissionStatus)entity.Status,
        ReviewReason = entity.ReviewReason,
        StudentId = entity.StudentId,
        StudentCode = studentCode
    };

    private static string CreateApplicationNumber() =>
        $"{StudentConstants.AdmissionApplicationNumberPrefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}";

    private static string? TrimToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
