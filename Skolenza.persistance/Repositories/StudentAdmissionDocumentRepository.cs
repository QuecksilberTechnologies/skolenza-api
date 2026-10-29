using System.Text.Json;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Student;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Repositories;

public sealed class StudentAdmissionDocumentRepository(SkolenzaDbContext context)
    : IStudentAdmissionDocumentRepository
{
    public async Task<StudentDocumentRequirementResponseDTO> GetUploadRequirementAsync(
        long institutionId,
        long admissionId,
        StudentAdmissionDocumentUploadDTO request,
        CancellationToken cancellationToken)
    {
        var admission = await GetEditableAdmissionAsync(institutionId, admissionId, cancellationToken);
        var requirement = await GetRequirementAsync(institutionId, admission, request, cancellationToken);
        await ValidatePersonScope(institutionId, admissionId, request, cancellationToken);
        ValidateExpiry(requirement, request.ExpiryDate);
        await EnsureNewVersionAllowedAsync(institutionId, admissionId, request, cancellationToken);
        return MapRequirement(requirement);
    }

    public async Task<StudentAdmissionDocumentResponseDTO> CreateVersionAsync(
        long institutionId,
        long actorId,
        long admissionId,
        StudentAdmissionDocumentUploadDTO request,
        string objectKey,
        string contentType,
        string checksumSha256,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var admission = await GetEditableAdmissionAsync(institutionId, admissionId, cancellationToken, lockForUpdate: true);
        var requirement = await GetRequirementAsync(institutionId, admission, request, cancellationToken);
        await ValidatePersonScope(institutionId, admissionId, request, cancellationToken);
        ValidateExpiry(requirement, request.ExpiryDate);

        var latest = await LatestDocumentQuery(institutionId, admissionId, request)
            .OrderByDescending(item => item.VersionNumber)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (latest is not null && latest.Status != (short)StudentDocumentReviewStatus.Rejected
            && latest.Status != (short)StudentDocumentReviewStatus.ReturnedForCorrection)
        {
            throw new ConflictException("A new document version is allowed only after rejection or return for correction.");
        }

        var now = DateTime.UtcNow;
        var entity = new StudentAdmissionDocument
        {
            InstitutionId = institutionId,
            StudentAdmissionId = admissionId,
            RequirementId = requirement.Id,
            GuardianId = request.PersonScope == StudentDocumentPersonScope.Student ? null : request.GuardianId,
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
            SubmittedById = actorId,
            SubmittedDateTime = now
        };

        context.StudentAdmissionDocuments.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        context.StudentAdmissionDocumentReviewHistories.Add(new StudentAdmissionDocumentReviewHistory
        {
            InstitutionId = institutionId,
            StudentAdmissionDocumentId = entity.Id,
            FromStatus = 0,
            ToStatus = entity.Status,
            ChangedById = actorId,
            ChangedDateTime = now
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(entity, requirement);
    }

    public async Task<PagedResponseDTO<StudentAdmissionDocumentResponseDTO>> GetPageAsync(
        long institutionId,
        long admissionId,
        StudentAdmissionDocumentFilterDTO filter,
        CancellationToken cancellationToken)
    {
        if (!await context.StudentAdmissions.AsNoTracking().AnyAsync(
                item => item.Id == admissionId && item.InstitutionId == institutionId,
                cancellationToken))
        {
            throw new KeyNotFoundException("Student admission was not found.");
        }

        var query = context.StudentAdmissionDocuments.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && item.StudentAdmissionId == admissionId);
        if (filter.PersonScope.HasValue)
        {
            query = query.Where(item => item.PersonScope == (short)filter.PersonScope.Value);
        }

        if (filter.GuardianId.HasValue)
        {
            query = query.Where(item => item.GuardianId == filter.GuardianId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(item => item.Status == (short)filter.Status.Value);
        }

        var page = await query
            .OrderByDescending(item => item.SubmittedDateTime)
            .ThenByDescending(item => item.Id)
            .ToPageAsync(filter.PageNumber, filter.PageSize, cancellationToken);
        var requirementIds = page.Data.Select(item => item.RequirementId).Distinct().ToArray();
        var requirements = await context.StudentDocumentRequirementMasters.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && requirementIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var rows = page.Data.Select(item =>
        {
            requirements.TryGetValue(item.RequirementId, out var requirement);
            return Map(item, requirement);
        }).ToList();

        return new PagedResponseDTO<StudentAdmissionDocumentResponseDTO>(rows, page.TotalCount, page.PageNumber, page.PageSize)
        {
            TotalPages = page.TotalPages
        };
    }

    public async Task<string> GetDownloadKeyAsync(
        long institutionId,
        long admissionId,
        long documentId,
        CancellationToken cancellationToken)
    {
        return await context.StudentAdmissionDocuments.AsNoTracking()
            .Where(item => item.Id == documentId && item.StudentAdmissionId == admissionId && item.InstitutionId == institutionId)
            .Select(item => item.ObjectKey)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Admission document was not found.");
    }

    public async Task<StudentAdmissionDocumentResponseDTO> ReviewAsync(
        long institutionId,
        long actorId,
        long admissionId,
        long documentId,
        StudentAdmissionDocumentReviewDTO request,
        CancellationToken cancellationToken)
    {
        if (request.Status is not StudentDocumentReviewStatus.Approved
            and not StudentDocumentReviewStatus.Rejected
            and not StudentDocumentReviewStatus.ReturnedForCorrection)
        {
            throw new ValidationErrorException("A document review decision must be Approved, Rejected, or ReturnedForCorrection.");
        }

        if ((request.Status is StudentDocumentReviewStatus.Rejected or StudentDocumentReviewStatus.ReturnedForCorrection) &&
            string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ValidationErrorException("A reason is required when rejecting or returning a document.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        // Admission approval and document decisions must observe a consistent order.
        _ = await context.StudentAdmissions
            .FromSqlInterpolated($"SELECT * FROM skolenza.\"StudentAdmission\" WHERE \"Id\" = {admissionId} AND \"InstitutionId\" = {institutionId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Student admission was not found.");
        var entity = await context.StudentAdmissionDocuments.SingleOrDefaultAsync(
            item => item.Id == documentId && item.StudentAdmissionId == admissionId && item.InstitutionId == institutionId,
            cancellationToken) ?? throw new KeyNotFoundException("Admission document was not found.");
        if (entity.Status != (short)StudentDocumentReviewStatus.AwaitingReview)
        {
            throw new ConflictException("Only documents awaiting review can receive a review decision.");
        }

        var requirement = await context.StudentDocumentRequirementMasters.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == entity.RequirementId && item.InstitutionId == institutionId, cancellationToken)
            ?? throw new KeyNotFoundException("The document requirement was not found.");
        var now = DateTime.UtcNow;
        var priorStatus = entity.Status;
        entity.Status = (short)request.Status;
        entity.ReviewedById = actorId;
        entity.ReviewedDateTime = now;
        entity.ReviewReason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        context.StudentAdmissionDocumentReviewHistories.Add(new StudentAdmissionDocumentReviewHistory
        {
            InstitutionId = institutionId,
            StudentAdmissionDocumentId = entity.Id,
            FromStatus = priorStatus,
            ToStatus = entity.Status,
            Reason = entity.ReviewReason,
            ChangedById = actorId,
            ChangedDateTime = now
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(entity, requirement);
    }

    private async Task<StudentAdmission> GetEditableAdmissionAsync(
        long institutionId,
        long admissionId,
        CancellationToken cancellationToken,
        bool lockForUpdate = false)
    {
        var query = lockForUpdate
            ? context.StudentAdmissions.FromSqlInterpolated($"SELECT * FROM skolenza.\"StudentAdmission\" WHERE \"Id\" = {admissionId} AND \"InstitutionId\" = {institutionId} FOR UPDATE")
            : context.StudentAdmissions.AsNoTracking().Where(item => item.Id == admissionId && item.InstitutionId == institutionId);
        var admission = await query.SingleOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Student admission was not found.");
        if (admission.Status is (short)StudentAdmissionStatus.Approved or (short)StudentAdmissionStatus.Rejected or (short)StudentAdmissionStatus.Cancelled)
        {
            throw new ConflictException("Documents cannot be changed for a terminal admission.");
        }

        return admission;
    }

    private async Task<StudentDocumentRequirementMaster> GetRequirementAsync(
        long institutionId,
        StudentAdmission admission,
        StudentAdmissionDocumentUploadDTO request,
        CancellationToken cancellationToken)
    {
        if (request.PersonScope is not StudentDocumentPersonScope.Student and not StudentDocumentPersonScope.Guardian)
        {
            throw new ValidationErrorException("PersonScope must be Student or Guardian for an uploaded document.");
        }

        var requirement = await context.StudentDocumentRequirementMasters.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.RequirementId && item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted,
                cancellationToken) ?? throw new ValidationErrorException("The selected document requirement is unavailable.");
        if (requirement.CampusId.HasValue && requirement.CampusId != admission.CampusId ||
            requirement.AcademicSessionId.HasValue && requirement.AcademicSessionId != admission.AcademicSessionId)
        {
            throw new ValidationErrorException("The selected document requirement does not apply to this admission's campus or session.");
        }

        if (requirement.AppliesTo != (short)request.PersonScope &&
            requirement.AppliesTo != (short)StudentDocumentPersonScope.StudentAndGuardian)
        {
            throw new ValidationErrorException("The selected document requirement does not apply to this person.");
        }

        return requirement;
    }

    private async Task ValidatePersonScope(
        long institutionId,
        long admissionId,
        StudentAdmissionDocumentUploadDTO request,
        CancellationToken cancellationToken)
    {
        if (request.PersonScope == StudentDocumentPersonScope.Student)
        {
            if (request.GuardianId.HasValue)
            {
                throw new ValidationErrorException("GuardianId must be empty for a Student document.");
            }

            return;
        }

        if (!request.GuardianId.HasValue || !await context.StudentAdmissionGuardians.AsNoTracking().AnyAsync(link =>
                link.InstitutionId == institutionId && link.StudentAdmissionId == admissionId &&
                link.GuardianId == request.GuardianId.Value && link.IsActive, cancellationToken))
        {
            throw new ValidationErrorException("Guardian documents must belong to an active Guardian captured on this admission.");
        }
    }

    private static void ValidateExpiry(StudentDocumentRequirementMaster requirement, DateOnly? expiryDate)
    {
        if (requirement.RequiresExpiryDate && !expiryDate.HasValue)
        {
            throw new ValidationErrorException("An expiry date is required for this document.");
        }

        if (expiryDate.HasValue && expiryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ValidationErrorException("The document expiry date cannot be in the past.");
        }
    }

    private async Task EnsureNewVersionAllowedAsync(
        long institutionId,
        long admissionId,
        StudentAdmissionDocumentUploadDTO request,
        CancellationToken cancellationToken)
    {
        var latest = await LatestDocumentQuery(institutionId, admissionId, request)
            .OrderByDescending(item => item.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);
        if (latest is not null && latest.Status != (short)StudentDocumentReviewStatus.Rejected
            && latest.Status != (short)StudentDocumentReviewStatus.ReturnedForCorrection)
        {
            throw new ConflictException("A new document version is allowed only after rejection or return for correction.");
        }
    }

    private IQueryable<StudentAdmissionDocument> LatestDocumentQuery(
        long institutionId,
        long admissionId,
        StudentAdmissionDocumentUploadDTO request)
    {
        var query = context.StudentAdmissionDocuments.AsNoTracking().Where(item =>
            item.InstitutionId == institutionId && item.StudentAdmissionId == admissionId &&
            item.RequirementId == request.RequirementId && item.PersonScope == (short)request.PersonScope);
        return request.PersonScope == StudentDocumentPersonScope.Student
            ? query.Where(item => item.GuardianId == null)
            : query.Where(item => item.GuardianId == request.GuardianId);
    }

    private static StudentDocumentRequirementResponseDTO MapRequirement(StudentDocumentRequirementMaster entity)
    {
        return new StudentDocumentRequirementResponseDTO
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            CampusId = entity.CampusId,
            AcademicSessionId = entity.AcademicSessionId,
            AppliesTo = (StudentDocumentPersonScope)entity.AppliesTo,
            IsRequired = entity.IsRequired,
            AllowedMimeTypes = JsonSerializer.Deserialize<List<string>>(entity.AllowedMimeTypesJson) ?? [],
            MaxFileSizeBytes = entity.MaxFileSizeBytes,
            RequiresExpiryDate = entity.RequiresExpiryDate,
            IsActive = entity.IsActive
        };
    }

    private static StudentAdmissionDocumentResponseDTO Map(
        StudentAdmissionDocument entity,
        StudentDocumentRequirementMaster? requirement)
    {
        return new StudentAdmissionDocumentResponseDTO
        {
            Id = entity.Id,
            RequirementId = entity.RequirementId,
            RequirementCode = requirement?.Code ?? string.Empty,
            RequirementName = requirement?.Name ?? string.Empty,
            PersonScope = (StudentDocumentPersonScope)entity.PersonScope,
            GuardianId = entity.GuardianId,
            VersionNumber = entity.VersionNumber,
            SupersedesDocumentId = entity.SupersedesDocumentId,
            OriginalFileName = entity.OriginalFileName,
            ContentType = entity.ContentType,
            FileSizeBytes = entity.FileSizeBytes,
            ChecksumSha256 = entity.ChecksumSha256,
            ExpiryDate = entity.ExpiryDate,
            Status = (StudentDocumentReviewStatus)entity.Status,
            SubmittedById = entity.SubmittedById,
            SubmittedDateTime = entity.SubmittedDateTime,
            ReviewedById = entity.ReviewedById,
            ReviewedDateTime = entity.ReviewedDateTime,
            ReviewReason = entity.ReviewReason
        };
    }
}
