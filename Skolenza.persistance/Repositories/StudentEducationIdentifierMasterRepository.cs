using Microsoft.EntityFrameworkCore;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Student;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;

namespace skolenza.persistance.Repositories;

public sealed class StudentEducationIdentifierMasterRepository(SkolenzaDbContext context)
    : IStudentEducationIdentifierMasterRepository
{
    public async Task<PagedResponseDTO<StudentEducationIdentifierMasterResponseDTO>> GetPageAsync(
        long institutionId,
        StudentEducationIdentifierMasterFilterDTO filter,
        CancellationToken cancellationToken)
    {
        var query = context.StudentEducationIdentifierMasters.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && !item.IsSoftDeleted);

        if (filter.CountryId.HasValue)
        {
            query = query.Where(item => item.CountryId == filter.CountryId.Value);
        }

        if (filter.ActiveOnly)
        {
            query = query.Where(item => item.IsActive);
        }

        var page = await query
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .ThenBy(item => item.Id)
            .Select(item => new StudentEducationIdentifierMasterResponseDTO
            {
                Id = item.Id,
                CountryId = item.CountryId,
                CountryName = context.Countries.Where(country => country.Id == item.CountryId)
                    .Select(country => country.CountryName).FirstOrDefault() ?? string.Empty,
                IssuingAuthority = item.IssuingAuthority,
                Code = item.Code,
                Name = item.Name,
                Description = item.Description,
                DisplayOrder = item.DisplayOrder,
                IsActive = item.IsActive
            })
            .ToPageAsync(filter.PageNumber, filter.PageSize, cancellationToken);

        return new PagedResponseDTO<StudentEducationIdentifierMasterResponseDTO>(
            page.Data, page.TotalCount, page.PageNumber, page.PageSize)
        {
            TotalPages = page.TotalPages
        };
    }

    public async Task<StudentEducationIdentifierMasterResponseDTO> SaveAsync(
        long? id,
        long institutionId,
        long actorStaffMemberId,
        StudentEducationIdentifierMasterRequestDTO request,
        CancellationToken cancellationToken)
    {
        Validate(request);

        if (!await context.Countries.AnyAsync(country =>
                country.Id == request.CountryId && country.IsActive != false, cancellationToken))
        {
            throw new ValidationErrorException("The selected country is invalid or inactive.");
        }

        var code = request.Code.Trim().ToUpperInvariant();
        var duplicate = await context.StudentEducationIdentifierMasters.AnyAsync(item =>
            item.InstitutionId == institutionId && item.CountryId == request.CountryId &&
            item.Code.ToUpper() == code && !item.IsSoftDeleted && (!id.HasValue || item.Id != id.Value),
            cancellationToken);
        if (duplicate)
        {
            throw new ConflictException("An education identifier with this code already exists for this country.");
        }

        var entity = id.HasValue
            ? await context.StudentEducationIdentifierMasters.SingleOrDefaultAsync(item =>
                item.Id == id.Value && item.InstitutionId == institutionId && !item.IsSoftDeleted,
                cancellationToken) ?? throw new KeyNotFoundException("Student education identifier master was not found.")
            : new StudentEducationIdentifierMaster
            {
                InstitutionId = institutionId,
                AddedById = actorStaffMemberId,
                AddedDateTime = DateTime.UtcNow
            };

        entity.CountryId = request.CountryId;
        entity.IssuingAuthority = request.IssuingAuthority.Trim();
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsActive = request.IsActive;

        if (id.HasValue)
        {
            entity.UpdatedById = actorStaffMemberId;
            entity.UpdatedDateTime = DateTime.UtcNow;
        }
        else
        {
            context.StudentEducationIdentifierMasters.Add(entity);
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
        var entity = await context.StudentEducationIdentifierMasters.SingleOrDefaultAsync(item =>
            item.Id == id && item.InstitutionId == institutionId && !item.IsSoftDeleted,
            cancellationToken) ?? throw new KeyNotFoundException("Student education identifier master was not found.");

        entity.IsActive = false;
        entity.IsSoftDeleted = true;
        entity.SoftDeletedById = actorStaffMemberId;
        entity.DeletedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<StudentEducationIdentifierMasterResponseDTO> GetByIdAsync(
        long id,
        long institutionId,
        CancellationToken cancellationToken) =>
        await context.StudentEducationIdentifierMasters.AsNoTracking()
            .Where(item => item.Id == id && item.InstitutionId == institutionId && !item.IsSoftDeleted)
            .Select(item => new StudentEducationIdentifierMasterResponseDTO
            {
                Id = item.Id,
                CountryId = item.CountryId,
                CountryName = context.Countries.Where(country => country.Id == item.CountryId)
                    .Select(country => country.CountryName).FirstOrDefault() ?? string.Empty,
                IssuingAuthority = item.IssuingAuthority,
                Code = item.Code,
                Name = item.Name,
                Description = item.Description,
                DisplayOrder = item.DisplayOrder,
                IsActive = item.IsActive
            })
            .SingleAsync(cancellationToken);

    private static void Validate(StudentEducationIdentifierMasterRequestDTO request)
    {
        if (request.CountryId <= 0 || string.IsNullOrWhiteSpace(request.IssuingAuthority) ||
            string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationErrorException("CountryId, issuing authority, code and name are required.");
        }

        if (request.Code.Trim().Length > 60 || request.Name.Trim().Length > 150 ||
            request.IssuingAuthority.Trim().Length > 150 || request.Description?.Length > 500)
        {
            throw new ValidationErrorException("One or more education identifier fields exceed the supported length.");
        }
    }
}
