using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Student;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Repositories;

public sealed class StudentBenefitSchemeRepository(SkolenzaDbContext context)
    : IStudentBenefitSchemeRepository
{
    public async Task<PagedResponseDTO<StudentBenefitSchemeResponseDTO>> GetPageAsync(
        long institutionId,
        StudentBenefitSchemeFilterDTO filter,
        CancellationToken cancellationToken)
    {
        var query = context.StudentBenefitSchemeMasters.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && !item.IsSoftDeleted);

        if (filter.ActiveOnly)
        {
            query = query.Where(item => item.IsActive);
        }

        if (filter.CountryId.HasValue)
        {
            query = query.Where(item => item.CountryId == null || item.CountryId == filter.CountryId);
        }

        if (filter.StateId.HasValue)
        {
            query = query.Where(item => item.StateId == null || item.StateId == filter.StateId);
        }

        if (filter.CampusId.HasValue)
        {
            query = query.Where(item => item.CampusId == null || item.CampusId == filter.CampusId);
        }

        if (filter.AcademicSessionId.HasValue)
        {
            query = query.Where(item => item.AcademicSessionId == null || item.AcademicSessionId == filter.AcademicSessionId);
        }

        var page = await query
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .ThenBy(item => item.Id)
            .Select(item => new StudentBenefitSchemeResponseDTO
            {
                Id = item.Id,
                Code = item.Code,
                Name = item.Name,
                Description = item.Description,
                CountryId = item.CountryId,
                CountryName = item.CountryId == null
                    ? null
                    : context.Countries.Where(country => country.Id == item.CountryId).Select(country => country.CountryName).FirstOrDefault(),
                StateId = item.StateId,
                StateName = item.StateId == null
                    ? null
                    : context.States.Where(state => state.Id == item.StateId).Select(state => state.StateName).FirstOrDefault(),
                CampusId = item.CampusId,
                AcademicSessionId = item.AcademicSessionId,
                MinimumDiscountPercent = item.MinimumDiscountPercent,
                MaximumDiscountPercent = item.MaximumDiscountPercent,
                EligibilityDescription = item.EligibilityDescription,
                EvidenceRequirementId = item.EvidenceRequirementId,
                DisplayOrder = item.DisplayOrder,
                IsActive = item.IsActive
            })
            .ToPageAsync(filter.PageNumber, filter.PageSize, cancellationToken);

        return new PagedResponseDTO<StudentBenefitSchemeResponseDTO>(
            page.Data, page.TotalCount, page.PageNumber, page.PageSize)
        {
            TotalPages = page.TotalPages
        };
    }

    public async Task<StudentBenefitSchemeResponseDTO> SaveAsync(
        long? id,
        long institutionId,
        long actorStaffMemberId,
        StudentBenefitSchemeRequestDTO request,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        await ValidateScopeAsync(institutionId, request, cancellationToken);

        var code = request.Code.Trim().ToUpperInvariant();
        var duplicate = await context.StudentBenefitSchemeMasters.AnyAsync(item =>
            item.InstitutionId == institutionId && item.Code == code && !item.IsSoftDeleted &&
            (!id.HasValue || item.Id != id.Value), cancellationToken);
        if (duplicate)
        {
            throw new ConflictException("A benefit scheme with this code already exists for the institution.");
        }

        var entity = id.HasValue
            ? await context.StudentBenefitSchemeMasters.SingleOrDefaultAsync(item =>
                item.Id == id.Value && item.InstitutionId == institutionId && !item.IsSoftDeleted,
                cancellationToken) ?? throw new KeyNotFoundException("Student benefit scheme was not found.")
            : new StudentBenefitSchemeMaster
            {
                InstitutionId = institutionId,
                AddedById = actorStaffMemberId,
                AddedDateTime = DateTime.UtcNow
            };

        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Description = NormalizeOptional(request.Description);
        entity.CountryId = request.CountryId;
        entity.StateId = request.StateId;
        entity.CampusId = request.CampusId;
        entity.AcademicSessionId = request.AcademicSessionId;
        entity.MinimumDiscountPercent = request.MinimumDiscountPercent;
        entity.MaximumDiscountPercent = request.MaximumDiscountPercent;
        entity.EligibilityDescription = NormalizeOptional(request.EligibilityDescription);
        entity.EvidenceRequirementId = request.EvidenceRequirementId;
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsActive = request.IsActive;

        if (id.HasValue)
        {
            entity.UpdatedById = actorStaffMemberId;
            entity.UpdatedDateTime = DateTime.UtcNow;
        }
        else
        {
            context.StudentBenefitSchemeMasters.Add(entity);
        }

        await context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(entity.Id, institutionId, cancellationToken);
    }

    public async Task DeleteAsync(
        long id,
        long institutionId,
        long actorStaffMemberId,
        CancellationToken cancellationToken)
    {
        var entity = await context.StudentBenefitSchemeMasters.SingleOrDefaultAsync(item =>
            item.Id == id && item.InstitutionId == institutionId && !item.IsSoftDeleted,
            cancellationToken) ?? throw new KeyNotFoundException("Student benefit scheme was not found.");

        entity.IsActive = false;
        entity.IsSoftDeleted = true;
        entity.SoftDeletedById = actorStaffMemberId;
        entity.DeletedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateScopeAsync(
        long institutionId,
        StudentBenefitSchemeRequestDTO request,
        CancellationToken cancellationToken)
    {
        if (request.StateId.HasValue && !request.CountryId.HasValue)
        {
            throw new ValidationErrorException("CountryId is required when StateId is selected.");
        }

        if (request.CountryId.HasValue)
        {
            var countryExists = await context.Countries.AnyAsync(item =>
                item.Id == request.CountryId && item.IsActive != false, cancellationToken);
            if (!countryExists)
            {
                throw new ValidationErrorException("The selected country is invalid or inactive.");
            }
        }

        if (request.StateId.HasValue)
        {
            var stateExists = await context.States.AnyAsync(item =>
                item.Id == request.StateId && item.CountryId == request.CountryId && item.IsActive != false,
                cancellationToken);
            if (!stateExists)
            {
                throw new ValidationErrorException("The selected state does not belong to the selected country or is inactive.");
            }
        }

        if (request.CampusId.HasValue)
        {
            var campus = await context.Campuss.AsNoTracking().SingleOrDefaultAsync(item =>
                item.Id == request.CampusId && item.InstitutionId == institutionId &&
                item.IsActive && !item.IsSoftDeleted, cancellationToken);
            if (campus is null)
            {
                throw new ValidationErrorException("The selected campus is invalid or inactive for this institution.");
            }

            if ((request.CountryId.HasValue && campus.CountryId != request.CountryId.Value) ||
                (request.StateId.HasValue && campus.StateId != request.StateId.Value))
            {
                throw new ValidationErrorException("The selected campus does not match the scheme country/state scope.");
            }
        }

        if (request.AcademicSessionId.HasValue && !await context.AcademicSessions.AnyAsync(item =>
                item.Id == request.AcademicSessionId && item.InstitutionId == institutionId &&
                !item.IsSoftDeleted, cancellationToken))
        {
            throw new ValidationErrorException("The selected academic session is invalid for this institution.");
        }

        if (request.EvidenceRequirementId.HasValue && !await context.StudentDocumentRequirementMasters.AnyAsync(item =>
                item.Id == request.EvidenceRequirementId && item.InstitutionId == institutionId &&
                !item.IsSoftDeleted && item.IsActive, cancellationToken))
        {
            throw new ValidationErrorException("The selected evidence document requirement is invalid for this institution.");
        }
    }

    private async Task<StudentBenefitSchemeResponseDTO> GetByIdAsync(
        long id,
        long institutionId,
        CancellationToken cancellationToken) =>
        await context.StudentBenefitSchemeMasters.AsNoTracking()
            .Where(item => item.Id == id && item.InstitutionId == institutionId && !item.IsSoftDeleted)
            .Select(item => new StudentBenefitSchemeResponseDTO
            {
                Id = item.Id,
                Code = item.Code,
                Name = item.Name,
                Description = item.Description,
                CountryId = item.CountryId,
                CountryName = item.CountryId == null
                    ? null
                    : context.Countries.Where(country => country.Id == item.CountryId).Select(country => country.CountryName).FirstOrDefault(),
                StateId = item.StateId,
                StateName = item.StateId == null
                    ? null
                    : context.States.Where(state => state.Id == item.StateId).Select(state => state.StateName).FirstOrDefault(),
                CampusId = item.CampusId,
                AcademicSessionId = item.AcademicSessionId,
                MinimumDiscountPercent = item.MinimumDiscountPercent,
                MaximumDiscountPercent = item.MaximumDiscountPercent,
                EligibilityDescription = item.EligibilityDescription,
                EvidenceRequirementId = item.EvidenceRequirementId,
                DisplayOrder = item.DisplayOrder,
                IsActive = item.IsActive
            })
            .SingleAsync(cancellationToken);

    private static void ValidateRequest(StudentBenefitSchemeRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationErrorException("Code and name are required.");
        }

        if (request.MinimumDiscountPercent < 0 || request.MaximumDiscountPercent > 100 ||
            request.MinimumDiscountPercent > request.MaximumDiscountPercent ||
            decimal.Round(request.MinimumDiscountPercent, 2) != request.MinimumDiscountPercent ||
            decimal.Round(request.MaximumDiscountPercent, 2) != request.MaximumDiscountPercent)
        {
            throw new ValidationErrorException("Discount bounds must be ordered percentages from 0 to 100 with at most two decimal places.");
        }

        if (request.EligibilityDescription?.Length > 1000)
        {
            throw new ValidationErrorException("EligibilityDescription cannot exceed 1000 characters.");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
