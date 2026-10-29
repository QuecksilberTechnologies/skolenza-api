using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Student;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Repositories;

public sealed class HostCountryCurrencyMasterRepository(SkolenzaDbContext context)
    : IHostCountryCurrencyMasterRepository
{
    public async Task<PagedResponseDTO<CountryCurrencyMasterResponseDTO>> GetPageAsync(
        CountryCurrencyMasterFilterDTO filter,
        CancellationToken cancellationToken)
    {
        var query = from mapping in context.CountryCurrencyMasters.AsNoTracking()
                    join country in context.Countries.AsNoTracking() on mapping.CountryId equals country.Id
                    where !filter.CountryId.HasValue || mapping.CountryId == filter.CountryId.Value
                    select new { Mapping = mapping, CountryName = country.CountryName };

        if (filter.ActiveOnly)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            query = query.Where(item => item.Mapping.IsActive &&
                item.Mapping.EffectiveFrom <= today &&
                (!item.Mapping.EffectiveTo.HasValue || item.Mapping.EffectiveTo.Value >= today));
        }

        var page = await query
            .OrderBy(item => item.CountryName)
            .ThenByDescending(item => item.Mapping.IsPrimary)
            .ThenBy(item => item.Mapping.CurrencyCode)
            .ThenBy(item => item.Mapping.Id)
            .ToPageAsync(filter.PageNumber, filter.PageSize, cancellationToken);

        var response = page.Data.Select(item => Map(item.Mapping, item.CountryName)).ToList();
        return new PagedResponseDTO<CountryCurrencyMasterResponseDTO>(
            response, page.TotalCount, page.PageNumber, page.PageSize)
        {
            TotalPages = page.TotalPages
        };
    }

    public async Task<CountryCurrencyMasterResponseDTO> SaveAsync(
        long? id,
        CountryCurrencyMasterRequestDTO request,
        long hostUserId,
        CancellationToken cancellationToken)
    {
        var code = request.CurrencyCode.Trim().ToUpperInvariant();
        var name = request.CurrencyName.Trim();
        var symbol = request.CurrencySymbol.Trim();
        if (code.Length != 3 || name.Length == 0 || symbol.Length == 0 || request.DecimalPlaces is not 0 and not 2 and not 3 ||
            request.EffectiveFrom == default ||
            (request.EffectiveTo.HasValue && request.EffectiveTo.Value < request.EffectiveFrom))
        {
            throw new ValidationErrorException("Country, ISO currency code, currency name, symbol, and a valid effective date range are required.");
        }

        if (!await context.Countries.AsNoTracking().AnyAsync(
                item => item.Id == request.CountryId && item.IsActive == true,
                cancellationToken))
        {
            throw new ValidationErrorException("The selected country is not active.");
        }

        var duplicate = await context.CountryCurrencyMasters.AnyAsync(item =>
            item.CountryId == request.CountryId &&
            item.CurrencyCode == code &&
            item.EffectiveFrom == request.EffectiveFrom &&
            (!id.HasValue || item.Id != id.Value), cancellationToken);
        if (duplicate)
        {
            throw new ConflictException("This currency mapping already exists for the country and effective date.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var entity = id.HasValue
            ? await context.CountryCurrencyMasters.SingleOrDefaultAsync(
                item => item.Id == id.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Country currency mapping was not found.")
            : new CountryCurrencyMaster
            {
                CountryId = request.CountryId,
                AddedByHostUserId = hostUserId,
                AddedDateTime = DateTime.UtcNow
            };

        if (request.IsPrimary && request.IsActive)
        {
            var previousPrimary = await context.CountryCurrencyMasters
                .Where(item => item.CountryId == request.CountryId &&
                    item.IsPrimary && item.IsActive &&
                    item.EffectiveTo == null &&
                    (!id.HasValue || item.Id != id.Value))
                .ToListAsync(cancellationToken);
            if (previousPrimary.Any(item => request.EffectiveFrom <= item.EffectiveFrom))
            {
                throw new ValidationErrorException("A replacement primary currency must start after the current primary currency's effective-from date.");
            }

            foreach (var previous in previousPrimary)
            {
                previous.IsPrimary = false;
                previous.IsActive = false;
                previous.EffectiveTo = request.EffectiveFrom.AddDays(-1);
                previous.UpdatedByHostUserId = hostUserId;
                previous.UpdatedDateTime = DateTime.UtcNow;
            }
        }

        entity.CountryId = request.CountryId;
        entity.CurrencyCode = code;
        entity.CurrencyName = name;
        entity.CurrencySymbol = symbol;
        entity.DecimalPlaces = request.DecimalPlaces;
        entity.IsPrimary = request.IsPrimary;
        entity.IsActive = request.IsActive;
        entity.EffectiveFrom = request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo;
        if (id.HasValue)
        {
            entity.UpdatedByHostUserId = hostUserId;
            entity.UpdatedDateTime = DateTime.UtcNow;
        }
        else
        {
            context.CountryCurrencyMasters.Add(entity);
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        var countryName = await context.Countries.AsNoTracking()
            .Where(item => item.Id == entity.CountryId)
            .Select(item => item.CountryName)
            .SingleAsync(cancellationToken);
        return Map(entity, countryName);
    }

    public async Task DeactivateAsync(long id, long hostUserId, CancellationToken cancellationToken)
    {
        var entity = await context.CountryCurrencyMasters.SingleOrDefaultAsync(
            item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Country currency mapping was not found.");
        entity.IsPrimary = false;
        entity.IsActive = false;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        entity.EffectiveTo ??= today < entity.EffectiveFrom ? entity.EffectiveFrom : today;
        entity.UpdatedByHostUserId = hostUserId;
        entity.UpdatedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    private static CountryCurrencyMasterResponseDTO Map(CountryCurrencyMaster entity, string countryName) => new()
    {
        Id = entity.Id,
        CountryId = entity.CountryId,
        CountryName = countryName,
        CurrencyCode = entity.CurrencyCode,
        CurrencyName = entity.CurrencyName,
        CurrencySymbol = entity.CurrencySymbol,
        DecimalPlaces = entity.DecimalPlaces,
        IsPrimary = entity.IsPrimary,
        IsActive = entity.IsActive,
        EffectiveFrom = entity.EffectiveFrom,
        EffectiveTo = entity.EffectiveTo
    };
}
