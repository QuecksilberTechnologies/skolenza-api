using System.Text.Json;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using skolenza.application.Common.Enums;
using skolenza.application.Constants;
using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Student;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;

namespace skolenza.persistance.Repositories;

public sealed class StudentPortalDocumentRepository(
    SkolenzaDbContext context,
    IStudentPortalAuthRepository portalAuth)
    : IStudentPortalDocumentRepository
{
    public async Task<IReadOnlyList<StudentPortalDocumentRequirementResponseDTO>> GetRequirementsAsync(
        long institutionId,
        long accountId,
        long studentId,
        StudentDocumentPersonScope personScope,
        CancellationToken cancellationToken)
    {
        if (personScope is not StudentDocumentPersonScope.Student and not StudentDocumentPersonScope.Guardian)
        {
            throw new ValidationErrorException("PersonScope must be Student or Guardian.");
        }

        var enrollment = await GetAuthorizedEnrollmentAsync(
            institutionId, accountId, studentId, personScope, null, cancellationToken);
        return await context.StudentDocumentRequirementMasters.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted &&
                (!item.CampusId.HasValue || item.CampusId == enrollment.CampusId) &&
                (!item.AcademicSessionId.HasValue || item.AcademicSessionId == enrollment.AcademicSessionId) &&
                (item.AppliesTo == (short)personScope || item.AppliesTo == (short)StudentDocumentPersonScope.StudentAndGuardian))
            .OrderBy(item => item.DisplayOrder).ThenBy(item => item.Id)
            .Select(item => new StudentPortalDocumentRequirementResponseDTO
            {
                Id = item.Id,
                Code = item.Code,
                Name = item.Name,
                AppliesTo = (StudentDocumentPersonScope)item.AppliesTo,
                IsRequired = item.IsRequired,
                AllowedMimeTypes = JsonSerializer.Deserialize<List<string>>(item.AllowedMimeTypesJson) ?? new List<string>(),
                MaxFileSizeBytes = item.MaxFileSizeBytes,
                RequiresExpiryDate = item.RequiresExpiryDate
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<StudentPortalDocumentRequirementResponseDTO> GetUploadRequirementAsync(
        long institutionId,
        long accountId,
        long studentId,
        StudentPortalDocumentUploadDTO request,
        CancellationToken cancellationToken)
    {
        ValidateUploadPerson(request);
        var enrollment = await GetAuthorizedEnrollmentAsync(
            institutionId, accountId, studentId, request.PersonScope, request.GuardianId, cancellationToken);
        var requirement = await context.StudentDocumentRequirementMasters.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.RequirementId &&
                item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted,
                cancellationToken) ?? throw new ValidationErrorException("The selected document requirement is unavailable.");
        ValidateRequirement(requirement, enrollment, request.PersonScope, request.ExpiryDate);
        await EnsureNewVersionAllowedAsync(institutionId, studentId, request, cancellationToken);
        return MapRequirement(requirement);
    }

    public async Task<StudentRecordDocumentResponseDTO> CreateVersionAsync(
        long institutionId,
        long accountId,
        long studentId,
        StudentPortalDocumentUploadDTO request,
        string objectKey,
        string contentType,
        string checksumSha256,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await CreateVersionCoreAsync(institutionId, accountId, studentId, request,
                    objectKey, contentType, checksumSha256, cancellationToken);
            }
            catch (Exception exception) when (IsSerializationFailure(exception))
            {
                context.ChangeTracker.Clear();
                if (attempt >= StudentConstants.PortalDocumentTransactionMaximumAttempts)
                {
                    throw new ConflictException(StudentConstants.PortalDocumentConcurrencyMessage);
                }
            }
        }
    }

    private async Task<StudentRecordDocumentResponseDTO> CreateVersionCoreAsync(
        long institutionId,
        long accountId,
        long studentId,
        StudentPortalDocumentUploadDTO request,
        string objectKey,
        string contentType,
        string checksumSha256,
        CancellationToken cancellationToken)
    {
        ValidateUploadPerson(request);
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        await LockUploadAccessAsync(institutionId, accountId, studentId, request.RequirementId, cancellationToken);
        var enrollment = await GetAuthorizedEnrollmentAsync(
            institutionId, accountId, studentId, request.PersonScope, request.GuardianId, cancellationToken);
        var requirement = await context.StudentDocumentRequirementMasters.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.RequirementId &&
                item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted,
                cancellationToken) ?? throw new ValidationErrorException("The selected document requirement is unavailable.");
        ValidateRequirement(requirement, enrollment, request.PersonScope, request.ExpiryDate);

        var versionKey = $"student-document:{institutionId}:{studentId}:{request.RequirementId}:{(short)request.PersonScope}:{request.GuardianId}";
        await context.Database.SqlQuery<bool>($"SELECT pg_advisory_xact_lock(hashtextextended({versionKey}, 0)) IS NULL AS \"Value\"")
            .SingleAsync(cancellationToken);

        var latest = await LatestDocumentQuery(institutionId, studentId, request)
            .OrderByDescending(item => item.VersionNumber).ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (latest is not null && latest.Status != (short)StudentDocumentReviewStatus.Rejected &&
            latest.Status != (short)StudentDocumentReviewStatus.ReturnedForCorrection)
        {
            throw new ConflictException("A new document version is allowed only after rejection or return for correction.");
        }

        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        var document = new StudentRecordDocument
        {
            InstitutionId = institutionId,
            StudentId = studentId,
            RequirementId = requirement.Id,
            GuardianId = request.PersonScope == StudentDocumentPersonScope.Student ? null : request.GuardianId,
            SubmittedByPortalAccountId = accountId,
            PersonScope = (short)request.PersonScope,
            VersionNumber = (latest?.VersionNumber ?? 0) + 1,
            SupersedesDocumentId = latest?.Id,
            OriginalFileName = Path.GetFileName(request.File.FileName),
            StorageProvider = "S3",
            ObjectKey = objectKey,
            ContentType = contentType,
            FileSizeBytes = request.File.Length,
            ChecksumSha256 = checksumSha256,
            ExpiryDate = request.ExpiryDate,
            Status = (short)StudentDocumentReviewStatus.AwaitingReview,
            SubmittedDateTime = now
        };

        context.StudentRecordDocuments.Add(document);
        await context.SaveChangesAsync(cancellationToken);
        context.StudentRecordDocumentReviewHistories.Add(new StudentRecordDocumentReviewHistory
        {
            InstitutionId = institutionId,
            StudentRecordDocumentId = document.Id,
            FromStatus = 0,
            ToStatus = document.Status,
            ChangedByPortalAccountId = accountId,
            ChangedDateTime = now
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(document, requirement);
    }

    private async Task LockUploadAccessAsync(
        long institutionId,
        long accountId,
        long studentId,
        long requirementId,
        CancellationToken cancellationToken)
    {
        // Match the Student-first ordering used by exit and Guardian-link changes.
        await context.Database.SqlQuery<long>($"SELECT \"Id\" AS \"Value\" FROM skolenza.\"Student\" WHERE \"InstitutionId\" = {institutionId} AND \"Id\" = {studentId} FOR UPDATE")
            .ToListAsync(cancellationToken);
        await context.Database.SqlQuery<long>($"SELECT \"Id\" AS \"Value\" FROM skolenza.\"StudentPortalAccount\" WHERE \"InstitutionId\" = {institutionId} AND \"Id\" = {accountId} FOR SHARE")
            .ToListAsync(cancellationToken);
        await context.Database.SqlQuery<long>($"SELECT sg.\"Id\" AS \"Value\" FROM skolenza.\"StudentGuardian\" sg JOIN skolenza.\"StudentPortalAccountGuardian\" ag ON ag.\"GuardianId\" = sg.\"GuardianId\" AND ag.\"InstitutionId\" = sg.\"InstitutionId\" JOIN skolenza.\"Guardian\" g ON g.\"Id\" = sg.\"GuardianId\" AND g.\"InstitutionId\" = sg.\"InstitutionId\" WHERE sg.\"InstitutionId\" = {institutionId} AND sg.\"StudentId\" = {studentId} AND ag.\"StudentPortalAccountId\" = {accountId} ORDER BY sg.\"Id\" FOR SHARE OF sg, ag, g")
            .ToListAsync(cancellationToken);
        await context.Database.SqlQuery<long>($"SELECT \"Id\" AS \"Value\" FROM skolenza.\"StudentPortalAccountStudent\" WHERE \"InstitutionId\" = {institutionId} AND \"StudentPortalAccountId\" = {accountId} AND \"StudentId\" = {studentId} FOR SHARE")
            .ToListAsync(cancellationToken);
        await context.Database.SqlQuery<long>($"SELECT \"Id\" AS \"Value\" FROM skolenza.\"StudentEnrollment\" WHERE \"InstitutionId\" = {institutionId} AND \"StudentId\" = {studentId} ORDER BY \"Id\" FOR SHARE")
            .ToListAsync(cancellationToken);
        await context.Database.SqlQuery<long>($"SELECT \"Id\" AS \"Value\" FROM skolenza.\"StudentDocumentRequirementMaster\" WHERE \"InstitutionId\" = {institutionId} AND \"Id\" = {requirementId} FOR SHARE")
            .ToListAsync(cancellationToken);
    }

    private static bool IsSerializationFailure(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure })
            {
                return true;
            }
        }

        return false;
    }

    public async Task<PagedResponseDTO<StudentRecordDocumentResponseDTO>> GetPageAsync(
        long institutionId,
        long accountId,
        long studentId,
        StudentPortalDocumentFilterDTO filter,
        CancellationToken cancellationToken)
    {
        await GetAuthorizedEnrollmentAsync(
            institutionId, accountId, studentId, StudentDocumentPersonScope.Student, null, cancellationToken);
        var query = DocumentQuery(institutionId, studentId, accountId);
        if (filter.PersonScope.HasValue)
        {
            query = query.Where(item => item.Document.PersonScope == (short)filter.PersonScope.Value);
        }
        if (filter.Status.HasValue)
        {
            query = query.Where(item => item.Document.Status == (short)filter.Status.Value);
        }

        var page = await query.OrderByDescending(item => item.Document.SubmittedDateTime)
            .ThenByDescending(item => item.Document.Id)
            .ToPageAsync(filter.PageNumber, filter.PageSize, cancellationToken);
        var rows = page.Data.Select(item => Map(item.Document, item.Requirement)).ToList();
        return new PagedResponseDTO<StudentRecordDocumentResponseDTO>(rows, page.TotalCount, page.PageNumber, page.PageSize)
        {
            TotalPages = page.TotalPages
        };
    }

    public async Task<string> GetDownloadKeyAsync(
        long institutionId,
        long accountId,
        long studentId,
        long documentId,
        CancellationToken cancellationToken)
    {
        await GetAuthorizedEnrollmentAsync(
            institutionId, accountId, studentId, StudentDocumentPersonScope.Student, null, cancellationToken);
        var row = await DocumentQuery(institutionId, studentId, accountId)
            .Where(item => item.Document.Id == documentId)
            .Select(item => item.Document.ObjectKey)
            .SingleOrDefaultAsync(cancellationToken);
        return row ?? throw new KeyNotFoundException("Student document was not found.");
    }

    public async Task<StudentRecordDocumentResponseDTO> ReviewAsync(
        long institutionId,
        long staffMemberId,
        long studentId,
        long documentId,
        StudentAdmissionDocumentReviewDTO request,
        CancellationToken cancellationToken)
    {
        if (request.Status is not StudentDocumentReviewStatus.Approved and
            not StudentDocumentReviewStatus.Rejected and not StudentDocumentReviewStatus.ReturnedForCorrection)
        {
            throw new ValidationErrorException("The selected document review status is not allowed.");
        }
        if (request.Status is StudentDocumentReviewStatus.Rejected or StudentDocumentReviewStatus.ReturnedForCorrection &&
            string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ValidationErrorException("A reason is required when rejecting or returning a document.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var document = await context.StudentRecordDocuments
            .FromSqlInterpolated($"SELECT * FROM skolenza.\"StudentRecordDocument\" WHERE \"Id\" = {documentId} AND \"InstitutionId\" = {institutionId} AND \"StudentId\" = {studentId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Student document was not found.");
        if (document.Status != (short)StudentDocumentReviewStatus.AwaitingReview)
        {
            throw new ConflictException("Only documents awaiting review can be reviewed.");
        }
        if (request.Status == StudentDocumentReviewStatus.Approved &&
            document.ExpiryDate.HasValue && document.ExpiryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ConflictException("An expired document cannot be approved. Request a current document version.");
        }

        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        var oldStatus = document.Status;
        document.Status = (short)request.Status;
        document.ReviewedByStaffMemberId = staffMemberId;
        document.ReviewedDateTime = now;
        document.ReviewReason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        context.StudentRecordDocumentReviewHistories.Add(new StudentRecordDocumentReviewHistory
        {
            InstitutionId = institutionId,
            StudentRecordDocumentId = document.Id,
            FromStatus = oldStatus,
            ToStatus = document.Status,
            Reason = document.ReviewReason,
            ChangedByStaffMemberId = staffMemberId,
            ChangedDateTime = now
        });
        await context.SaveChangesAsync(cancellationToken);
        var requirement = await context.StudentDocumentRequirementMasters.AsNoTracking()
            .SingleAsync(item => item.Id == document.RequirementId && item.InstitutionId == institutionId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(document, requirement);
    }

    private async Task<StudentEnrollment> GetAuthorizedEnrollmentAsync(
        long institutionId,
        long accountId,
        long studentId,
        StudentDocumentPersonScope personScope,
        long? guardianId,
        CancellationToken cancellationToken)
    {
        if (personScope is not StudentDocumentPersonScope.Student and not StudentDocumentPersonScope.Guardian)
        {
            throw new ValidationErrorException("PersonScope must be Student or Guardian.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var profile = await portalAuth.GetProfileAsync(institutionId, accountId, cancellationToken);
        var allowed = profile?.Personas.Any(persona =>
            (personScope == StudentDocumentPersonScope.Student &&
                persona.Type == StudentPortalPersonaType.Student && persona.Id == studentId) ||
            (persona.Type == StudentPortalPersonaType.Guardian &&
                (!guardianId.HasValue || persona.Id == guardianId.Value) &&
                persona.LinkedStudents.Any(student => student.StudentId == studentId))) == true;
        if (!allowed)
        {
            throw new KeyNotFoundException("Student was not found for this portal account.");
        }

        var studentExists = await context.Students.AsNoTracking().AnyAsync(item =>
            item.Id == studentId && item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted,
            cancellationToken);
        if (!studentExists)
        {
            throw new KeyNotFoundException("Student was not found for this portal account.");
        }

        return await context.StudentEnrollments.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && item.StudentId == studentId && item.IsActive &&
                item.Status == (short)StudentEnrollmentStatus.Active &&
                item.EffectiveFrom <= today && (!item.EffectiveTo.HasValue || item.EffectiveTo.Value >= today))
            .OrderByDescending(item => item.EffectiveFrom).ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ConflictException("Student has no current active enrollment for location-specific document requirements.");
    }

    private IQueryable<DocumentProjection> DocumentQuery(
        long institutionId,
        long studentId,
        long accountId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var accountGuardians = context.StudentPortalAccountGuardians.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && item.StudentPortalAccountId == accountId && item.IsActive)
            .Select(item => item.GuardianId);
        var activeStudentGuardian = context.StudentGuardians.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && item.StudentId == studentId && item.IsActive &&
                item.HasPortalAccess && item.EffectiveFrom <= today &&
                context.Guardians.Any(guardian => guardian.Id == item.GuardianId &&
                    guardian.InstitutionId == institutionId && guardian.IsActive && !guardian.IsSoftDeleted) &&
                (!item.EffectiveTo.HasValue || item.EffectiveTo.Value >= today) &&
                accountGuardians.Contains(item.GuardianId))
            .Select(item => item.GuardianId);
        var studentPersona = context.StudentPortalAccountStudents.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && item.StudentPortalAccountId == accountId &&
                item.StudentId == studentId && item.IsActive).Select(item => item.StudentId);

        return from document in context.StudentRecordDocuments.AsNoTracking()
               join requirement in context.StudentDocumentRequirementMasters.AsNoTracking()
                   on document.RequirementId equals requirement.Id
               where document.InstitutionId == institutionId && document.StudentId == studentId &&
                   (document.PersonScope == (short)StudentDocumentPersonScope.Student
                       ? studentPersona.Contains(studentId) || activeStudentGuardian.Any()
                       : document.GuardianId.HasValue && activeStudentGuardian.Contains(document.GuardianId.Value))
               select new DocumentProjection { Document = document, Requirement = requirement };
    }

    private IQueryable<StudentRecordDocument> LatestDocumentQuery(
        long institutionId,
        long studentId,
        StudentPortalDocumentUploadDTO request)
    {
        var query = context.StudentRecordDocuments.AsNoTracking().Where(item =>
            item.InstitutionId == institutionId && item.StudentId == studentId &&
            item.RequirementId == request.RequirementId && item.PersonScope == (short)request.PersonScope);
        return request.PersonScope == StudentDocumentPersonScope.Student
            ? query.Where(item => item.GuardianId == null)
            : query.Where(item => item.GuardianId == request.GuardianId);
    }

    private async Task EnsureNewVersionAllowedAsync(
        long institutionId,
        long studentId,
        StudentPortalDocumentUploadDTO request,
        CancellationToken cancellationToken)
    {
        var latest = await LatestDocumentQuery(institutionId, studentId, request)
            .OrderByDescending(item => item.VersionNumber).FirstOrDefaultAsync(cancellationToken);
        if (latest is not null && latest.Status != (short)StudentDocumentReviewStatus.Rejected &&
            latest.Status != (short)StudentDocumentReviewStatus.ReturnedForCorrection)
        {
            throw new ConflictException("A new document version is allowed only after rejection or return for correction.");
        }
    }

    private static void ValidateUploadPerson(StudentPortalDocumentUploadDTO request)
    {
        if (request.PersonScope == StudentDocumentPersonScope.Guardian && !request.GuardianId.HasValue)
        {
            throw new ValidationErrorException("GuardianId is required for a Guardian document.");
        }
    }

    private static void ValidateRequirement(
        StudentDocumentRequirementMaster requirement,
        StudentEnrollment enrollment,
        StudentDocumentPersonScope personScope,
        DateOnly? expiryDate)
    {
        if ((requirement.CampusId.HasValue && requirement.CampusId != enrollment.CampusId) ||
            (requirement.AcademicSessionId.HasValue && requirement.AcademicSessionId != enrollment.AcademicSessionId) ||
            (requirement.AppliesTo != (short)personScope && requirement.AppliesTo != (short)StudentDocumentPersonScope.StudentAndGuardian))
        {
            throw new ValidationErrorException("The selected document requirement does not apply to this Student enrollment or person.");
        }
        if (requirement.RequiresExpiryDate && !expiryDate.HasValue)
        {
            throw new ValidationErrorException("An expiry date is required for this document.");
        }
        if (expiryDate.HasValue && expiryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ValidationErrorException("The document expiry date cannot be in the past.");
        }
    }

    private static StudentPortalDocumentRequirementResponseDTO MapRequirement(StudentDocumentRequirementMaster entity) => new()
    {
        Id = entity.Id,
        Code = entity.Code,
        Name = entity.Name,
        AppliesTo = (StudentDocumentPersonScope)entity.AppliesTo,
        IsRequired = entity.IsRequired,
        AllowedMimeTypes = JsonSerializer.Deserialize<List<string>>(entity.AllowedMimeTypesJson) ?? [],
        MaxFileSizeBytes = entity.MaxFileSizeBytes,
        RequiresExpiryDate = entity.RequiresExpiryDate
    };

    private static StudentRecordDocumentResponseDTO Map(
        StudentRecordDocument document,
        StudentDocumentRequirementMaster requirement) => new()
    {
        Id = document.Id,
        RequirementId = requirement.Id,
        RequirementCode = requirement.Code,
        RequirementName = requirement.Name,
        StudentId = document.StudentId,
        PersonScope = (StudentDocumentPersonScope)document.PersonScope,
        GuardianId = document.GuardianId,
        VersionNumber = document.VersionNumber,
        SupersedesDocumentId = document.SupersedesDocumentId,
        OriginalFileName = document.OriginalFileName,
        ContentType = document.ContentType,
        FileSizeBytes = document.FileSizeBytes,
        ChecksumSha256 = document.ChecksumSha256,
        ExpiryDate = document.ExpiryDate,
        Status = (StudentDocumentReviewStatus)document.Status,
        SubmittedDateTime = document.SubmittedDateTime,
        ReviewedDateTime = document.ReviewedDateTime,
        ReviewReason = document.ReviewReason
    };

    private sealed class DocumentProjection
    {
        public StudentRecordDocument Document { get; init; } = null!;
        public StudentDocumentRequirementMaster Requirement { get; init; } = null!;
    }
}
