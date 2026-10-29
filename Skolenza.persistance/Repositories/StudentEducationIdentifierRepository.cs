using Microsoft.EntityFrameworkCore;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.Constants;
using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Student;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;

namespace skolenza.persistance.Repositories;

public sealed class StudentEducationIdentifierRepository(
    SkolenzaDbContext context,
    IEncryptionService encryptionService)
    : IStudentEducationIdentifierRepository
{
    public async Task<PagedResponseDTO<StudentEducationIdentifierResponseDTO>> GetPageAsync(
        long institutionId,
        StudentEducationIdentifierFilterDTO filter,
        CancellationToken cancellationToken)
    {
        if (filter.StudentId <= 0)
        {
            throw new ValidationErrorException("StudentId is required.");
        }

        await EnsureStudentExistsAsync(institutionId, filter.StudentId, cancellationToken);
        var page = await BuildResponseQuery(institutionId, filter.StudentId)
            .OrderBy(item => item.IdentifierName)
            .ThenBy(item => item.Id)
            .ToPageAsync(filter.PageNumber, filter.PageSize, cancellationToken);

        return new PagedResponseDTO<StudentEducationIdentifierResponseDTO>(
            page.Data, page.TotalCount, page.PageNumber, page.PageSize)
        {
            TotalPages = page.TotalPages
        };
    }

    public async Task<StudentEducationIdentifierResponseDTO> CreateAsync(
        long institutionId,
        long actorStaffMemberId,
        StudentEducationIdentifierRequestDTO request,
        string institutionEncryptionKey,
        CancellationToken cancellationToken)
    {
        var value = NormalizeValue(request.Value);
        var student = await EnsureStudentExistsAsync(institutionId, request.StudentId, cancellationToken);
        var master = await context.StudentEducationIdentifierMasters.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.IdentifierMasterId &&
                item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted,
                cancellationToken)
            ?? throw new NotFoundException("The active education identifier type was not found.");

        if (await context.StudentEducationIdentifiers.AnyAsync(item =>
                item.InstitutionId == institutionId &&
                item.StudentId == student.Id &&
                item.IdentifierMasterId == master.Id,
                cancellationToken))
        {
            throw new ConflictException("This student already has a value for the selected identifier type. Update the existing record.");
        }

        var now = DateTime.UtcNow;
        var entity = new StudentEducationIdentifier
        {
            InstitutionId = institutionId,
            StudentId = student.Id,
            IdentifierMasterId = master.Id,
            CountryId = master.CountryId,
            EncryptedValue = encryptionService.Encrypt(value, institutionEncryptionKey),
            MaskedValue = Mask(value),
            VerificationStatus = (short)StudentEducationIdentifierVerificationStatus.Unverified,
            IssuerReference = NormalizeOptional(request.IssuerReference),
            IssuedOn = request.IssuedOn,
            AddedById = actorStaffMemberId,
            AddedDateTime = now
        };

        context.StudentEducationIdentifiers.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return await GetResponseAsync(institutionId, entity.Id, cancellationToken);
    }

    public async Task<StudentEducationIdentifierResponseDTO> UpdateAsync(
        long institutionId,
        long actorStaffMemberId,
        long identifierId,
        StudentEducationIdentifierUpdateDTO request,
        string institutionEncryptionKey,
        CancellationToken cancellationToken)
    {
        var value = NormalizeValue(request.Value);
        var entity = await context.StudentEducationIdentifiers.SingleOrDefaultAsync(item =>
                item.Id == identifierId && item.InstitutionId == institutionId,
                cancellationToken)
            ?? throw new NotFoundException("Student education identifier was not found.");

        entity.EncryptedValue = encryptionService.Encrypt(value, institutionEncryptionKey);
        entity.MaskedValue = Mask(value);
        entity.IssuerReference = NormalizeOptional(request.IssuerReference);
        entity.IssuedOn = request.IssuedOn;
        var previousStatus = entity.VerificationStatus;
        entity.VerificationStatus = (short)StudentEducationIdentifierVerificationStatus.Unverified;
        entity.VerifiedById = null;
        entity.VerifiedDateTime = null;
        entity.UpdatedById = actorStaffMemberId;
        entity.UpdatedDateTime = DateTime.UtcNow;
        if (previousStatus != (short)StudentEducationIdentifierVerificationStatus.Unverified)
        {
            context.StudentEducationIdentifierVerificationHistories.Add(
                new StudentEducationIdentifierVerificationHistory
                {
                    InstitutionId = institutionId,
                    StudentEducationIdentifierId = entity.Id,
                    FromStatus = previousStatus,
                    ToStatus = (short)StudentEducationIdentifierVerificationStatus.Unverified,
                    Reason = "Identifier value updated; verification reset.",
                    ChangedById = actorStaffMemberId,
                    ChangedDateTime = entity.UpdatedDateTime.Value
                });
        }

        await context.SaveChangesAsync(cancellationToken);
        return await GetResponseAsync(institutionId, entity.Id, cancellationToken);
    }

    public async Task<StudentEducationIdentifierResponseDTO> SetVerificationStatusAsync(
        long institutionId,
        long actorStaffMemberId,
        long identifierId,
        short verificationStatus,
        string? reason,
        CancellationToken cancellationToken)
    {
        if (verificationStatus is not ((short)StudentEducationIdentifierVerificationStatus.Verified or
            (short)StudentEducationIdentifierVerificationStatus.Rejected))
        {
            throw new ValidationErrorException("VerificationStatus must be Verified (2) or Rejected (3).");
        }

        var entity = await context.StudentEducationIdentifiers.SingleOrDefaultAsync(item =>
                item.Id == identifierId && item.InstitutionId == institutionId,
                cancellationToken)
            ?? throw new NotFoundException("Student education identifier was not found.");

        if (entity.VerificationStatus != (short)StudentEducationIdentifierVerificationStatus.Unverified)
        {
            throw new ConflictException("Only an unverified education identifier can be reviewed.");
        }

        var previousStatus = entity.VerificationStatus;
        var now = DateTime.UtcNow;
        entity.VerificationStatus = verificationStatus;
        entity.VerifiedById = actorStaffMemberId;
        entity.VerifiedDateTime = now;
        entity.UpdatedById = actorStaffMemberId;
        entity.UpdatedDateTime = now;
        context.StudentEducationIdentifierVerificationHistories.Add(
            new StudentEducationIdentifierVerificationHistory
            {
                InstitutionId = institutionId,
                StudentEducationIdentifierId = entity.Id,
                FromStatus = previousStatus,
                ToStatus = verificationStatus,
                Reason = NormalizeOptional(reason),
                ChangedById = actorStaffMemberId,
                ChangedDateTime = now
            });

        await context.SaveChangesAsync(cancellationToken);
        return await GetResponseAsync(institutionId, entity.Id, cancellationToken);
    }

    private IQueryable<StudentEducationIdentifierResponseDTO> BuildResponseQuery(
        long institutionId,
        long studentId) =>
        from identifier in context.StudentEducationIdentifiers.AsNoTracking()
        join master in context.StudentEducationIdentifierMasters.AsNoTracking()
            on identifier.IdentifierMasterId equals master.Id
        join country in context.Countries.AsNoTracking()
            on identifier.CountryId equals country.Id
        where identifier.InstitutionId == institutionId &&
              identifier.StudentId == studentId &&
              master.InstitutionId == institutionId &&
              !master.IsSoftDeleted
        select new StudentEducationIdentifierResponseDTO
        {
            Id = identifier.Id,
            StudentId = identifier.StudentId,
            IdentifierMasterId = identifier.IdentifierMasterId,
            CountryId = identifier.CountryId,
            CountryName = country.CountryName,
            IdentifierCode = master.Code,
            IdentifierName = master.Name,
            IssuingAuthority = master.IssuingAuthority,
            MaskedValue = identifier.MaskedValue,
            VerificationStatus = identifier.VerificationStatus,
            IssuerReference = identifier.IssuerReference,
            IssuedOn = identifier.IssuedOn,
            AddedById = identifier.AddedById,
            AddedDateTime = identifier.AddedDateTime,
            VerifiedById = identifier.VerifiedById,
            VerifiedDateTime = identifier.VerifiedDateTime,
            UpdatedById = identifier.UpdatedById,
            UpdatedDateTime = identifier.UpdatedDateTime
        };

    private async Task<StudentEducationIdentifierResponseDTO> GetResponseAsync(
        long institutionId,
        long identifierId,
        CancellationToken cancellationToken) =>
        await (from identifier in context.StudentEducationIdentifiers.AsNoTracking()
               join master in context.StudentEducationIdentifierMasters.AsNoTracking()
                   on identifier.IdentifierMasterId equals master.Id
               join country in context.Countries.AsNoTracking()
                   on identifier.CountryId equals country.Id
               where identifier.InstitutionId == institutionId &&
                     identifier.Id == identifierId &&
                     master.InstitutionId == institutionId &&
                     !master.IsSoftDeleted
               select new StudentEducationIdentifierResponseDTO
               {
                   Id = identifier.Id,
                   StudentId = identifier.StudentId,
                   IdentifierMasterId = identifier.IdentifierMasterId,
                   CountryId = identifier.CountryId,
                   CountryName = country.CountryName,
                   IdentifierCode = master.Code,
                   IdentifierName = master.Name,
                   IssuingAuthority = master.IssuingAuthority,
                   MaskedValue = identifier.MaskedValue,
                   VerificationStatus = identifier.VerificationStatus,
                   IssuerReference = identifier.IssuerReference,
                   IssuedOn = identifier.IssuedOn,
                   AddedById = identifier.AddedById,
                   AddedDateTime = identifier.AddedDateTime,
                   VerifiedById = identifier.VerifiedById,
                   VerifiedDateTime = identifier.VerifiedDateTime,
                   UpdatedById = identifier.UpdatedById,
                   UpdatedDateTime = identifier.UpdatedDateTime
               }).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Student education identifier was not found.");

    private async Task<Student> EnsureStudentExistsAsync(
        long institutionId,
        long studentId,
        CancellationToken cancellationToken)
    {
        if (studentId <= 0)
        {
            throw new ValidationErrorException("StudentId is required.");
        }

        return await context.Students.SingleOrDefaultAsync(student =>
                student.Id == studentId &&
                student.InstitutionId == institutionId &&
                !student.IsSoftDeleted,
                cancellationToken)
            ?? throw new NotFoundException("Student was not found.");
    }

    private static string NormalizeValue(string value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) ||
            normalized.Length > StudentConstants.EducationIdentifierMaximumLength)
        {
            throw new ValidationErrorException("Identifier value is required and must not exceed the configured maximum length.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Mask(string value)
    {
        if (value.Length <= StudentConstants.EducationIdentifierVisibleSuffixLength)
        {
            return new string(StudentConstants.EducationIdentifierMaskCharacter, value.Length);
        }

        var visibleSuffixLength = Math.Min(
            StudentConstants.EducationIdentifierVisibleSuffixLength,
            value.Length);
        var maskedLength = value.Length - visibleSuffixLength;
        return new string(StudentConstants.EducationIdentifierMaskCharacter, maskedLength) +
               value[^visibleSuffixLength..];
    }
}
