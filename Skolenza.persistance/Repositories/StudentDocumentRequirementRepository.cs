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

public sealed class StudentDocumentRequirementRepository(SkolenzaDbContext context)
    : IStudentDocumentRequirementRepository
{
    public async Task<PagedResponseDTO<StudentDocumentRequirementResponseDTO>> GetPageAsync(
        long institutionId,
        StudentDocumentRequirementFilterDTO filter,
        CancellationToken cancellationToken)
    {
        var query = context.StudentDocumentRequirementMasters
            .AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && !item.IsSoftDeleted);

        if (filter.CampusId.HasValue)
        {
            query = query.Where(item => item.CampusId == null || item.CampusId == filter.CampusId);
        }

        if (filter.AcademicSessionId.HasValue)
        {
            query = query.Where(item => item.AcademicSessionId == null || item.AcademicSessionId == filter.AcademicSessionId);
        }

        if (filter.ActiveOnly)
        {
            query = query.Where(item => item.IsActive);
        }

        if (filter.AppliesTo.HasValue)
        {
            var scope = (short)filter.AppliesTo.Value;
            query = query.Where(item => item.AppliesTo == scope || item.AppliesTo == (short)StudentDocumentPersonScope.StudentAndGuardian);
        }

        var page = await query
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .ThenBy(item => item.Id)
            .ToPageAsync(filter.PageNumber, filter.PageSize, cancellationToken);

        var data = page.Data.Select(Map).ToList();
        return new PagedResponseDTO<StudentDocumentRequirementResponseDTO>(data, page.TotalCount, page.PageNumber, page.PageSize)
        {
            TotalPages = page.TotalPages
        };
    }

    public async Task<StudentDocumentRequirementResponseDTO> CreateAsync(
        long institutionId,
        long actorStaffMemberId,
        StudentDocumentRequirementRequestDTO request,
        CancellationToken cancellationToken)
    {
        var entity = new StudentDocumentRequirementMaster
        {
            InstitutionId = institutionId,
            Code = NormalizeCode(request.Code),
            AddedById = actorStaffMemberId,
            AddedDateTime = DateTime.UtcNow
        };

        Apply(entity, request);
        await ValidateReferencesAsync(institutionId, request, cancellationToken);
        await EnsureCodeAvailableAsync(institutionId, request, null, cancellationToken);

        context.StudentDocumentRequirementMasters.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<StudentDocumentRequirementResponseDTO> UpdateAsync(
        long id,
        long institutionId,
        long actorStaffMemberId,
        StudentDocumentRequirementRequestDTO request,
        CancellationToken cancellationToken)
    {
        var entity = await context.StudentDocumentRequirementMasters
            .SingleOrDefaultAsync(item => item.Id == id && item.InstitutionId == institutionId && !item.IsSoftDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Student document requirement was not found.");

        await ValidateReferencesAsync(institutionId, request, cancellationToken);
        await EnsureCodeAvailableAsync(institutionId, request, id, cancellationToken);

        Apply(entity, request);
        entity.UpdatedById = actorStaffMemberId;
        entity.UpdatedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task DeleteAsync(
        long id,
        long institutionId,
        long actorStaffMemberId,
        CancellationToken cancellationToken)
    {
        var entity = await context.StudentDocumentRequirementMasters
            .SingleOrDefaultAsync(item => item.Id == id && item.InstitutionId == institutionId && !item.IsSoftDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Student document requirement was not found.");

        entity.IsActive = false;
        entity.IsSoftDeleted = true;
        entity.SoftDeletedById = actorStaffMemberId;
        entity.DeletedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateReferencesAsync(
        long institutionId,
        StudentDocumentRequirementRequestDTO request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.AppliesTo))
        {
            throw new ValidationErrorException("AppliesTo must identify Student, Guardian, or both.");
        }

        if (request.AllowedMimeTypes.Count == 0 ||
            request.AllowedMimeTypes.Any(value => string.IsNullOrWhiteSpace(value) || !value.Contains('/')))
        {
            throw new ValidationErrorException("At least one valid allowed MIME type is required.");
        }

        if (request.CampusId.HasValue && !await context.Campuss.AnyAsync(
                campus => campus.Id == request.CampusId && campus.InstitutionId == institutionId && campus.IsActive && !campus.IsSoftDeleted,
                cancellationToken))
        {
            throw new ValidationErrorException("The selected campus is unavailable for this institution.");
        }

        if (request.AcademicSessionId.HasValue && !await context.AcademicSessions.AnyAsync(
                session => session.Id == request.AcademicSessionId && session.InstitutionId == institutionId && session.IsActive && !session.IsSoftDeleted,
                cancellationToken))
        {
            throw new ValidationErrorException("The selected academic session is unavailable for this institution.");
        }
    }

    private Task<bool> EnsureCodeAvailableAsync(
        long institutionId,
        StudentDocumentRequirementRequestDTO request,
        long? exceptId,
        CancellationToken cancellationToken)
    {
        var code = NormalizeCode(request.Code);
        return EnsureAsync();

        async Task<bool> EnsureAsync()
        {
            var exists = await context.StudentDocumentRequirementMasters.AnyAsync(item =>
                item.InstitutionId == institutionId &&
                item.CampusId == request.CampusId &&
                item.AcademicSessionId == request.AcademicSessionId &&
                item.Code == code &&
                !item.IsSoftDeleted &&
                (!exceptId.HasValue || item.Id != exceptId.Value), cancellationToken);
            if (exists)
            {
                throw new ConflictException("A document requirement with this code already exists in the selected scope.");
            }

            return true;
        }
    }

    private static void Apply(StudentDocumentRequirementMaster entity, StudentDocumentRequirementRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationErrorException("A document requirement name is required.");
        }

        entity.Code = NormalizeCode(request.Code);
        entity.Name = request.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        entity.CampusId = request.CampusId;
        entity.AcademicSessionId = request.AcademicSessionId;
        entity.AppliesTo = (short)request.AppliesTo;
        entity.IsRequired = request.IsRequired;
        entity.AllowedMimeTypesJson = JsonSerializer.Serialize(request.AllowedMimeTypes
            .Select(value => value.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.Ordinal));
        entity.MaxFileSizeBytes = request.MaxFileSizeBytes;
        entity.RequiresExpiryDate = request.RequiresExpiryDate;
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsActive = request.IsActive;
    }

    private static string NormalizeCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationErrorException("A document requirement code is required.");
        }

        return value.Trim().ToUpperInvariant();
    }

    private static StudentDocumentRequirementResponseDTO Map(StudentDocumentRequirementMaster entity)
    {
        return new StudentDocumentRequirementResponseDTO
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Description = entity.Description,
            CampusId = entity.CampusId,
            AcademicSessionId = entity.AcademicSessionId,
            AppliesTo = (StudentDocumentPersonScope)entity.AppliesTo,
            IsRequired = entity.IsRequired,
            AllowedMimeTypes = JsonSerializer.Deserialize<List<string>>(entity.AllowedMimeTypesJson) ?? [],
            MaxFileSizeBytes = entity.MaxFileSizeBytes,
            RequiresExpiryDate = entity.RequiresExpiryDate,
            DisplayOrder = entity.DisplayOrder,
            IsActive = entity.IsActive
        };
    }
}
