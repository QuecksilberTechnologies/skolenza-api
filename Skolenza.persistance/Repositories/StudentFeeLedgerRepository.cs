using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.Constants;
using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Student;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace skolenza.persistance.Repositories;

public sealed class StudentFeeLedgerRepository(
    SkolenzaDbContext context,
    IStudentPortalAuthRepository? portalAuth = null) : IStudentFeeLedgerRepository
{
    private sealed record PortalCheckoutScope(long AccountId, long StudentId, long AdmissionId);
    public async Task<PagedResponseDTO<StudentPortalFeeAdmissionResponseDTO>> GetPortalAdmissionsAsync(
        long institutionId,
        long studentId,
        StudentPortalFeeFilterDTO filter,
        CancellationToken cancellationToken)
    {
        var query = from admission in context.StudentAdmissions.AsNoTracking()
                    join quote in context.StudentAdmissionFeeQuotes.AsNoTracking()
                        on admission.Id equals quote.StudentAdmissionId
                    join session in context.AcademicSessions.AsNoTracking()
                        on admission.AcademicSessionId equals session.Id
                    join campus in context.Campuss.AsNoTracking()
                        on admission.CampusId equals campus.Id
                    join academicGroup in context.AcademicGroups.AsNoTracking()
                        on admission.AcademicGroupId equals academicGroup.Id
                    where admission.InstitutionId == institutionId && quote.InstitutionId == institutionId &&
                        session.InstitutionId == institutionId && campus.InstitutionId == institutionId &&
                        academicGroup.InstitutionId == institutionId &&
                        admission.StudentId == studentId && admission.Status == (short)StudentAdmissionStatus.Approved
                    orderby admission.AcademicSessionId descending, admission.Id descending
                    select new StudentPortalFeeAdmissionResponseDTO
                    {
                        AdmissionId = admission.Id,
                        AcademicSessionId = admission.AcademicSessionId,
                        AcademicSessionName = session.Name,
                        CampusId = admission.CampusId,
                        CampusName = campus.LocationName,
                        AcademicGroupId = admission.AcademicGroupId,
                        AcademicGroupName = academicGroup.Name,
                        QuoteId = quote.Id,
                        CurrencyCode = quote.CurrencyCode,
                        NetAnnualAmount = quote.NetAnnualAmount
                    };

        return await query.ToPageAsync(filter.PageNumber, filter.PageSize, cancellationToken);
    }

    public Task<bool> AdmissionBelongsToStudentAsync(
        long institutionId,
        long studentId,
        long admissionId,
        CancellationToken cancellationToken) => context.StudentAdmissions.AsNoTracking().AnyAsync(
            item => item.Id == admissionId && item.InstitutionId == institutionId &&
                item.StudentId == studentId && item.Status == (short)StudentAdmissionStatus.Approved,
            cancellationToken);

    public Task<bool> InstallmentBelongsToAdmissionAsync(
        long institutionId,
        long admissionId,
        long installmentId,
        CancellationToken cancellationToken)
    {
        return (from installment in context.StudentFeeInstallments.AsNoTracking()
                join quote in context.StudentAdmissionFeeQuotes.AsNoTracking()
                    on installment.StudentAdmissionFeeQuoteId equals quote.Id
                where installment.InstitutionId == institutionId && quote.InstitutionId == institutionId &&
                    installment.Id == installmentId && quote.StudentAdmissionId == admissionId
                select installment.Id).AnyAsync(cancellationToken);
    }

    public async Task<PagedResponseDTO<StudentFeeStructureResponseDTO>> GetStructuresAsync(
        long institutionId,
        StudentFeeStructureFilterDTO filter,
        CancellationToken cancellationToken)
    {
        var query = from structure in context.StudentFeeStructures.AsNoTracking()
                    join campus in context.Campuss.AsNoTracking() on structure.CampusId equals campus.Id
                    join session in context.AcademicSessions.AsNoTracking() on structure.AcademicSessionId equals session.Id
                    join academicGroup in context.AcademicGroups.AsNoTracking() on structure.AcademicGroupId equals academicGroup.Id
                    where structure.InstitutionId == institutionId && !structure.IsSoftDeleted
                    select new { Structure = structure, CampusName = campus.LocationName, SessionName = session.Name, GroupName = academicGroup.Name };

        if (filter.ActiveOnly)
        {
            query = query.Where(item => item.Structure.IsActive);
        }

        if (filter.CampusId.HasValue)
        {
            query = query.Where(item => item.Structure.CampusId == filter.CampusId.Value);
        }

        if (filter.AcademicSessionId.HasValue)
        {
            query = query.Where(item => item.Structure.AcademicSessionId == filter.AcademicSessionId.Value);
        }

        if (filter.AcademicGroupId.HasValue)
        {
            query = query.Where(item => item.Structure.AcademicGroupId == filter.AcademicGroupId.Value);
        }

        var page = await query
            .OrderBy(item => item.CampusName)
            .ThenBy(item => item.SessionName)
            .ThenBy(item => item.GroupName)
            .ThenBy(item => item.Structure.Id)
            .ToPageAsync(filter.PageNumber, filter.PageSize, cancellationToken);
        var structureIds = page.Data.Select(item => item.Structure.Id).ToArray();
        var items = await (from item in context.StudentFeeStructureItems.AsNoTracking()
                           join master in context.StudentFeeComponentMasters.AsNoTracking()
                               on item.FeeComponentMasterId equals master.Id
                           where item.InstitutionId == institutionId && structureIds.Contains(item.StudentFeeStructureId) && item.IsActive
                           select new { item.StudentFeeStructureId, Value = new StudentFeeStructureItemResponseDTO
                           {
                               FeeComponentMasterId = item.FeeComponentMasterId,
                               ComponentCode = item.ComponentCode,
                               ComponentName = item.ComponentName,
                               ChargeNature = item.ChargeNature,
                               Amount = item.Amount
                           } }).ToListAsync(cancellationToken);
        var frequencyRows = await (from item in context.StudentFeeStructureFrequencies.AsNoTracking()
                                   join master in context.StudentFeeFrequencyMasters.AsNoTracking()
                                       on item.FeeFrequencyMasterId equals master.Id
                                   where item.InstitutionId == institutionId && structureIds.Contains(item.StudentFeeStructureId) && item.IsActive
                                   select new { item.Id, item.StudentFeeStructureId, item.FeeFrequencyMasterId, master.Code, master.Name, master.InstallmentsPerYear })
            .ToListAsync(cancellationToken);
        var frequencyIds = frequencyRows.Select(item => item.Id).ToArray();
        var dueDates = await context.StudentFeeStructureInstallmentDueDates.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && frequencyIds.Contains(item.StudentFeeStructureFrequencyId))
            .OrderBy(item => item.InstallmentNumber)
            .ToListAsync(cancellationToken);
        var dueDateByFrequency = dueDates.GroupBy(item => item.StudentFeeStructureFrequencyId)
            .ToDictionary(group => group.Key, group => group.Select(item => new StudentFeeStructureDueDateResponseDTO
            {
                InstallmentNumber = item.InstallmentNumber,
                DueDate = item.DueDate
            }).ToList());
        var frequencyByStructure = frequencyRows.GroupBy(item => item.StudentFeeStructureId)
            .ToDictionary(group => group.Key, group => group.Select(item => new StudentFeeStructureFrequencyResponseDTO
            {
                Id = item.Id,
                FeeFrequencyMasterId = item.FeeFrequencyMasterId,
                FrequencyCode = item.Code,
                FrequencyName = item.Name,
                InstallmentsPerYear = item.InstallmentsPerYear,
                Installments = dueDateByFrequency.GetValueOrDefault(item.Id) ?? []
            }).ToList());
        var itemsByStructure = items.GroupBy(item => item.StudentFeeStructureId)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Value).ToList());
        var response = page.Data.Select(row => MapStructure(
            row.Structure, row.CampusName, row.SessionName, row.GroupName,
            itemsByStructure.GetValueOrDefault(row.Structure.Id) ?? [],
            frequencyByStructure.GetValueOrDefault(row.Structure.Id) ?? [])).ToList();
        return new PagedResponseDTO<StudentFeeStructureResponseDTO>(response, page.TotalCount, page.PageNumber, page.PageSize)
        {
            TotalPages = page.TotalPages
        };
    }

    public async Task<StudentFeeStructureResponseDTO> SaveStructureAsync(
        long? id,
        long institutionId,
        long actorId,
        StudentFeeStructureRequestDTO request,
        CancellationToken cancellationToken)
    {
        var campus = await context.Campuss.AsNoTracking().SingleOrDefaultAsync(item =>
            item.Id == request.CampusId && item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted,
            cancellationToken) ?? throw new ValidationErrorException("The selected campus is not active in this institution.");
        var session = await context.AcademicSessions.AsNoTracking().SingleOrDefaultAsync(item =>
            item.Id == request.AcademicSessionId && item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted,
            cancellationToken) ?? throw new ValidationErrorException("The selected academic session is not active in this institution.");
        var group = await context.AcademicGroups.AsNoTracking().SingleOrDefaultAsync(item =>
            item.Id == request.AcademicGroupId && item.InstitutionId == institutionId &&
            item.AcademicSessionId == session.Id && item.IsActive && !item.IsSoftDeleted,
            cancellationToken) ?? throw new ValidationErrorException("The selected group must be active and belong to the selected academic session.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currency = await context.CountryCurrencyMasters.AsNoTracking().SingleOrDefaultAsync(item =>
            item.CountryId == campus.CountryId && item.IsPrimary && item.IsActive && item.EffectiveFrom <= today &&
            (!item.EffectiveTo.HasValue || item.EffectiveTo.Value >= today), cancellationToken)
            ?? throw new ValidationErrorException("No active primary currency is configured for the campus country.");

        if (request.Items.Count == 0 || request.Items.Select(item => item.FeeComponentMasterId).Distinct().Count() != request.Items.Count ||
            request.Frequencies.Count == 0 || request.Frequencies.Select(item => item.FeeFrequencyMasterId).Distinct().Count() != request.Frequencies.Count)
        {
            throw new ValidationErrorException("Fee structure requires unique fee components and at least one allowed payment frequency.");
        }

        var componentIds = request.Items.Select(item => item.FeeComponentMasterId).ToArray();
        var components = await context.StudentFeeComponentMasters.AsNoTracking()
            .Where(item => componentIds.Contains(item.Id) && item.IsActive && !item.IsSoftDeleted &&
                (!item.InstitutionId.HasValue || item.InstitutionId == institutionId))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        if (components.Count != componentIds.Length || components.Values.Any(item =>
                item.ChargeNature != (short)StudentFeeComponentChargeNature.RecurringAnnual &&
                item.ChargeNature != (short)StudentFeeComponentChargeNature.OneTime))
        {
            throw new ValidationErrorException("Every fee component must be active, institution-accessible and have a supported charge nature.");
        }

        var frequencyIds = request.Frequencies.Select(item => item.FeeFrequencyMasterId).ToArray();
        var frequencies = await context.StudentFeeFrequencyMasters.AsNoTracking()
            .Where(item => frequencyIds.Contains(item.Id) && item.IsActive && !item.IsSoftDeleted &&
                (!item.InstitutionId.HasValue || item.InstitutionId == institutionId))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        if (frequencies.Count != frequencyIds.Length)
        {
            throw new ValidationErrorException("Every payment frequency must be active and institution-accessible.");
        }

        foreach (var frequencyRequest in request.Frequencies)
        {
            var count = frequencies[frequencyRequest.FeeFrequencyMasterId].InstallmentsPerYear;
            var dates = frequencyRequest.Installments.OrderBy(item => item.InstallmentNumber).ToArray();
            if (count <= 0 || dates.Length != count || dates.Select(item => item.InstallmentNumber).Distinct().Count() != count ||
                dates.Where((item, index) => item.InstallmentNumber != index + 1 || item.DueDate == default ||
                    (index > 0 && item.DueDate < dates[index - 1].DueDate)).Any())
            {
                throw new ValidationErrorException($"Frequency {frequencies[frequencyRequest.FeeFrequencyMasterId].Name} requires {count} ordered, explicit installment due dates.");
            }
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        StudentFeeStructure entity;
        if (id.HasValue)
        {
            entity = await context.StudentFeeStructures.SingleOrDefaultAsync(item =>
                item.Id == id.Value && item.InstitutionId == institutionId && !item.IsSoftDeleted, cancellationToken)
                ?? throw new NotFoundException("Fee structure was not found.");
            if (await context.StudentAdmissionFeeQuotes.AnyAsync(item =>
                    item.InstitutionId == institutionId && item.StudentFeeStructureId == entity.Id, cancellationToken))
            {
                throw new ConflictException("This fee structure has immutable admission quotes and can no longer be edited. Create a new academic session structure instead.");
            }

            var oldFrequencyIds = await context.StudentFeeStructureFrequencies
                .Where(item => item.InstitutionId == institutionId && item.StudentFeeStructureId == entity.Id)
                .Select(item => item.Id).ToArrayAsync(cancellationToken);
            var oldDueDates = context.StudentFeeStructureInstallmentDueDates
                .Where(item => item.InstitutionId == institutionId && oldFrequencyIds.Contains(item.StudentFeeStructureFrequencyId));
            var oldFrequencies = context.StudentFeeStructureFrequencies
                .Where(item => item.InstitutionId == institutionId && item.StudentFeeStructureId == entity.Id);
            var oldItems = context.StudentFeeStructureItems
                .Where(item => item.InstitutionId == institutionId && item.StudentFeeStructureId == entity.Id);
            context.StudentFeeStructureInstallmentDueDates.RemoveRange(oldDueDates);
            context.StudentFeeStructureFrequencies.RemoveRange(oldFrequencies);
            context.StudentFeeStructureItems.RemoveRange(oldItems);
            entity.UpdatedById = actorId;
            entity.UpdatedDateTime = DateTime.UtcNow;
        }
        else
        {
            entity = new StudentFeeStructure
            {
                InstitutionId = institutionId,
                AddedById = actorId,
                AddedDateTime = DateTime.UtcNow
            };
            context.StudentFeeStructures.Add(entity);
        }

        entity.CampusId = campus.Id;
        entity.AcademicSessionId = session.Id;
        entity.AcademicGroupId = group.Id;
        entity.CountryCurrencyMasterId = currency.Id;
        entity.CurrencyCode = currency.CurrencyCode;
        entity.CurrencyDecimalPlaces = currency.DecimalPlaces;
        entity.LateFeePerInstallment = StudentFeeQuoteCalculator.RoundCurrency(request.LateFeePerInstallment, currency.DecimalPlaces);
        entity.GracePeriodDays = request.GracePeriodDays;
        entity.LateFeeCap = request.LateFeeCap.HasValue ? StudentFeeQuoteCalculator.RoundCurrency(request.LateFeeCap.Value, currency.DecimalPlaces) : null;
        entity.IsActive = request.IsActive;
        await context.SaveChangesAsync(cancellationToken);

        context.StudentFeeStructureItems.AddRange(request.Items.Select(item =>
        {
            var master = components[item.FeeComponentMasterId];
            return new StudentFeeStructureItem
            {
                InstitutionId = institutionId,
                StudentFeeStructureId = entity.Id,
                FeeComponentMasterId = master.Id,
                ComponentCode = master.Code,
                ComponentName = master.Name,
                ChargeNature = master.ChargeNature,
                Amount = StudentFeeQuoteCalculator.RoundCurrency(item.Amount, currency.DecimalPlaces)
            };
        }));

        foreach (var frequencyRequest in request.Frequencies)
        {
            var structureFrequency = new StudentFeeStructureFrequency
            {
                InstitutionId = institutionId,
                StudentFeeStructureId = entity.Id,
                FeeFrequencyMasterId = frequencyRequest.FeeFrequencyMasterId
            };
            context.StudentFeeStructureFrequencies.Add(structureFrequency);
            await context.SaveChangesAsync(cancellationToken);
            context.StudentFeeStructureInstallmentDueDates.AddRange(frequencyRequest.Installments.Select(item => new StudentFeeStructureInstallmentDueDate
            {
                InstitutionId = institutionId,
                StudentFeeStructureFrequencyId = structureFrequency.Id,
                InstallmentNumber = item.InstallmentNumber,
                DueDate = item.DueDate
            }));
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        var page = await GetStructuresAsync(institutionId, new StudentFeeStructureFilterDTO
        {
            CampusId = entity.CampusId,
            AcademicSessionId = entity.AcademicSessionId,
            AcademicGroupId = entity.AcademicGroupId,
            ActiveOnly = false,
            PageNumber = 1,
            PageSize = 10
        }, cancellationToken);
        return page.Data.Single(item => item.Id == entity.Id);
    }

    public async Task DeactivateStructureAsync(long id, long institutionId, long actorId, CancellationToken cancellationToken)
    {
        var entity = await context.StudentFeeStructures.SingleOrDefaultAsync(item =>
            item.Id == id && item.InstitutionId == institutionId && !item.IsSoftDeleted, cancellationToken)
            ?? throw new NotFoundException("Fee structure was not found.");
        if (await context.StudentAdmissionFeeQuotes.AnyAsync(item =>
                item.InstitutionId == institutionId && item.StudentFeeStructureId == id, cancellationToken))
        {
            throw new ConflictException("This fee structure is referenced by an immutable admission quote and cannot be deactivated.");
        }

        entity.IsActive = false;
        entity.UpdatedById = actorId;
        entity.UpdatedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<StudentAdmissionFeeQuoteResponseDTO> CreateQuoteAsync(
        long admissionId,
        long institutionId,
        long actorId,
        StudentAdmissionFeeQuoteRequestDTO request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var admission = await context.StudentAdmissions
            .FromSqlInterpolated($"SELECT * FROM skolenza.\"StudentAdmission\" WHERE \"Id\" = {admissionId} AND \"InstitutionId\" = {institutionId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (admission is null || admission.StudentId.HasValue ||
            (admission.Status != (short)StudentAdmissionStatus.Draft && admission.Status != (short)StudentAdmissionStatus.Submitted &&
             admission.Status != (short)StudentAdmissionStatus.UnderReview && admission.Status != (short)StudentAdmissionStatus.ReturnedForCorrection))
        {
            throw new ValidationErrorException("A fee quote can only be created for an active, unapproved admission.");
        }
        var structure = await context.StudentFeeStructures.AsNoTracking().SingleOrDefaultAsync(item =>
            item.Id == request.StudentFeeStructureId && item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted &&
            item.CampusId == admission.CampusId && item.AcademicSessionId == admission.AcademicSessionId &&
            item.AcademicGroupId == admission.AcademicGroupId, cancellationToken)
            ?? throw new ValidationErrorException("The fee structure must match the admission campus, academic session and group.");
        if (await context.StudentAdmissionFeeQuotes.AnyAsync(item =>
                item.InstitutionId == institutionId && item.StudentAdmissionId == admissionId, cancellationToken))
        {
            throw new ConflictException("An immutable fee quote already exists for this admission.");
        }

        var frequency = await (from allowed in context.StudentFeeStructureFrequencies.AsNoTracking()
                               join master in context.StudentFeeFrequencyMasters.AsNoTracking()
                                   on allowed.FeeFrequencyMasterId equals master.Id
                               where allowed.Id == request.StudentFeeStructureFrequencyId && allowed.InstitutionId == institutionId &&
                                     allowed.StudentFeeStructureId == structure.Id && allowed.IsActive && master.IsActive && !master.IsSoftDeleted
                               select new { Allowed = allowed, Master = master }).SingleOrDefaultAsync(cancellationToken)
            ?? throw new ValidationErrorException("The selected payment frequency is not enabled for this fee structure.");
        var components = await context.StudentFeeStructureItems.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && item.StudentFeeStructureId == structure.Id && item.IsActive)
            .OrderBy(item => item.Id)
            .Select(item => new StudentFeeStructureItemResponseDTO
            {
                FeeComponentMasterId = item.FeeComponentMasterId,
                ComponentCode = item.ComponentCode,
                ComponentName = item.ComponentName,
                ChargeNature = item.ChargeNature,
                Amount = item.Amount
            }).ToListAsync(cancellationToken);
        var dueDates = await context.StudentFeeStructureInstallmentDueDates.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && item.StudentFeeStructureFrequencyId == frequency.Allowed.Id)
            .OrderBy(item => item.InstallmentNumber)
            .Select(item => new StudentFeeStructureDueDateResponseDTO
            {
                InstallmentNumber = item.InstallmentNumber,
                DueDate = item.DueDate
            }).ToListAsync(cancellationToken);

        var discountPercent = 0m;
        long? schemeId = null;
        if (request.StudentBenefitSchemeMasterId.HasValue)
        {
            var campus = await context.Campuss.AsNoTracking().SingleAsync(item => item.Id == admission.CampusId, cancellationToken);
            var scheme = await context.StudentBenefitSchemeMasters.AsNoTracking().SingleOrDefaultAsync(item =>
                item.Id == request.StudentBenefitSchemeMasterId.Value && item.IsActive && !item.IsSoftDeleted &&
                (!item.InstitutionId.HasValue || item.InstitutionId == institutionId) &&
                (!item.CountryId.HasValue || item.CountryId == campus.CountryId) &&
                (!item.StateId.HasValue || item.StateId == campus.StateId) &&
                (!item.CampusId.HasValue || item.CampusId == campus.Id) &&
                (!item.AcademicSessionId.HasValue || item.AcademicSessionId == admission.AcademicSessionId), cancellationToken)
                ?? throw new ValidationErrorException("The selected benefit scheme is not active or does not apply to this admission location and session.");
            if (request.DiscountPercent < scheme.MinimumDiscountPercent || request.DiscountPercent > scheme.MaximumDiscountPercent)
            {
                throw new ValidationErrorException("The discount percentage must be within the selected scheme's configured minimum and maximum.");
            }

            if (scheme.EvidenceRequirementId.HasValue && !await context.StudentAdmissionDocuments.AnyAsync(item =>
                    item.InstitutionId == institutionId && item.StudentAdmissionId == admissionId &&
                    item.RequirementId == scheme.EvidenceRequirementId.Value && item.Status == (short)StudentDocumentReviewStatus.Approved,
                    cancellationToken))
            {
                throw new ValidationErrorException("The benefit scheme requires an approved admission document before it can be quoted.");
            }

            schemeId = scheme.Id;
            discountPercent = request.DiscountPercent;
        }
        else if (request.DiscountPercent != 0m)
        {
            throw new ValidationErrorException("A discount cannot be applied without an eligible benefit scheme.");
        }

        var calculation = StudentFeeQuoteCalculator.Calculate(
            admissionId, structure.Id, frequency.Allowed.Id, structure.CurrencyCode,
            frequency.Master.InstallmentsPerYear, discountPercent, components, dueDates,
            structure.CurrencyDecimalPlaces);
        var quote = new StudentAdmissionFeeQuote
        {
            InstitutionId = institutionId,
            StudentAdmissionId = admissionId,
            StudentFeeStructureId = structure.Id,
            StudentFeeStructureFrequencyId = frequency.Allowed.Id,
            StudentBenefitSchemeMasterId = schemeId,
            CurrencyCode = calculation.CurrencyCode,
            CurrencyDecimalPlaces = calculation.CurrencyDecimalPlaces,
            DiscountPercent = calculation.DiscountPercent,
            GrossRecurringAmount = calculation.GrossRecurringAmount,
            DiscountAmount = calculation.DiscountAmount,
            OneTimeAmount = calculation.OneTimeAmount,
            NetAnnualAmount = calculation.NetAnnualAmount,
            InstallmentsPerYear = calculation.InstallmentsPerYear,
            CreatedById = actorId,
            CreatedDateTime = DateTime.UtcNow
        };
        context.StudentAdmissionFeeQuotes.Add(quote);
        await context.SaveChangesAsync(cancellationToken);
        context.StudentAdmissionFeeQuoteLines.AddRange(calculation.Lines.Select(item => new StudentAdmissionFeeQuoteLine
        {
            InstitutionId = institutionId,
            StudentAdmissionFeeQuoteId = quote.Id,
            ComponentCode = item.ComponentCode,
            ComponentName = item.ComponentName,
            ChargeNature = item.ChargeNature,
            AnnualAmount = item.AnnualAmount,
            DiscountAmount = item.DiscountAmount,
            NetAmount = item.NetAmount
        }));
        context.StudentFeeInstallments.AddRange(calculation.Installments.Select(item => new StudentFeeInstallment
        {
            InstitutionId = institutionId,
            StudentAdmissionFeeQuoteId = quote.Id,
            InstallmentNumber = item.InstallmentNumber,
            DueDate = item.DueDate,
            RecurringAmount = item.RecurringAmount,
            OneTimeAmount = item.OneTimeAmount,
            Status = item.Status
        }));
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetQuoteAsync(quote.Id, institutionId, cancellationToken);
    }

    public async Task<PagedResponseDTO<StudentAdmissionFeeQuoteResponseDTO>> GetQuotesAsync(
        long institutionId,
        StudentFeeQuoteListRequestDTO filter,
        CancellationToken cancellationToken)
    {
        var page = await context.StudentAdmissionFeeQuotes.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId)
            .OrderByDescending(item => item.CreatedDateTime)
            .ThenByDescending(item => item.Id)
            .ToPageAsync(filter.PageNumber, filter.PageSize, cancellationToken);
        var quotes = new List<StudentAdmissionFeeQuoteResponseDTO>(page.Data.Count);
        foreach (var quote in page.Data)
        {
            quotes.Add(await GetQuoteAsync(quote.Id, institutionId, cancellationToken));
        }

        return new PagedResponseDTO<StudentAdmissionFeeQuoteResponseDTO>(quotes, page.TotalCount, page.PageNumber, page.PageSize)
        {
            TotalPages = page.TotalPages
        };
    }

    public async Task<StudentAdmissionFeeQuoteResponseDTO> GetAdmissionQuoteAsync(
        long admissionId,
        long institutionId,
        CancellationToken cancellationToken)
    {
        var quoteId = await context.StudentAdmissionFeeQuotes.AsNoTracking()
            .Where(item => item.StudentAdmissionId == admissionId && item.InstitutionId == institutionId)
            .Select(item => (long?)item.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Admission fee quote was not found.");

        return await GetQuoteAsync(quoteId, institutionId, cancellationToken);
    }

    public async Task<StudentAdmissionFeeBalanceResponseDTO> GetAdmissionBalanceAsync(
        long admissionId,
        long institutionId,
        CancellationToken cancellationToken)
    {
        var quote = await context.StudentAdmissionFeeQuotes.AsNoTracking()
            .SingleOrDefaultAsync(item => item.StudentAdmissionId == admissionId && item.InstitutionId == institutionId, cancellationToken)
            ?? throw new NotFoundException("Admission fee quote was not found.");
        var admission = await context.StudentAdmissions.AsNoTracking()
            .Where(item => item.Id == admissionId && item.InstitutionId == institutionId)
            .Select(item => new { item.Id, item.StudentId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Student admission was not found.");
        var structure = await context.StudentFeeStructures.AsNoTracking()
            .Where(item => item.Id == quote.StudentFeeStructureId && item.InstitutionId == institutionId)
            .Select(item => new { item.LateFeePerInstallment, item.GracePeriodDays, item.LateFeeCap })
            .SingleAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rows = await context.StudentFeeInstallments.AsNoTracking()
            .Where(item => item.StudentAdmissionFeeQuoteId == quote.Id && item.InstitutionId == institutionId)
            .OrderBy(item => item.InstallmentNumber)
            .ToListAsync(cancellationToken);
        var installments = rows.Select(item =>
        {
            var lateFee = item.LateFeeAmount;
            if (today > item.DueDate.AddDays(structure.GracePeriodDays) && lateFee == 0m && item.Status != (short)StudentFeeInstallmentStatus.Paid)
            {
                lateFee = structure.LateFeeCap.HasValue
                    ? Math.Min(structure.LateFeePerInstallment, structure.LateFeeCap.Value)
                    : structure.LateFeePerInstallment;
            }

            return new StudentFeeInstallmentResponseDTO
            {
                Id = item.Id,
                QuoteId = quote.Id,
                InstallmentNumber = item.InstallmentNumber,
                DueDate = item.DueDate,
                RecurringAmount = item.RecurringAmount,
                OneTimeAmount = item.OneTimeAmount,
                LateFeeAmount = lateFee,
                PaidAmount = item.PaidAmount,
                OutstandingAmount = StudentFeeQuoteCalculator.RoundCurrency(
                    item.RecurringAmount + item.OneTimeAmount + lateFee - item.PaidAmount,
                    quote.CurrencyDecimalPlaces),
                Status = today > item.DueDate.AddDays(structure.GracePeriodDays) && item.Status != (short)StudentFeeInstallmentStatus.Paid
                    ? (short)StudentFeeInstallmentStatus.Overdue
                    : item.Status
            };
        }).ToList();
        var totalPaid = StudentFeeQuoteCalculator.RoundCurrency(installments.Sum(item => item.PaidAmount), quote.CurrencyDecimalPlaces);
        var totalLateFees = StudentFeeQuoteCalculator.RoundCurrency(installments.Sum(item => item.LateFeeAmount), quote.CurrencyDecimalPlaces);
        var outstanding = StudentFeeQuoteCalculator.RoundCurrency(installments.Sum(item => item.OutstandingAmount), quote.CurrencyDecimalPlaces);
        var overdue = StudentFeeQuoteCalculator.RoundCurrency(installments
            .Where(item => item.Status == (short)StudentFeeInstallmentStatus.Overdue ||
                           item.Status == (short)StudentFeeInstallmentStatus.PartiallyPaid && item.DueDate.AddDays(structure.GracePeriodDays) < today)
            .Sum(item => item.OutstandingAmount), quote.CurrencyDecimalPlaces);

        return new StudentAdmissionFeeBalanceResponseDTO
        {
            StudentAdmissionId = admission.Id,
            StudentId = admission.StudentId,
            QuoteId = quote.Id,
            CurrencyCode = quote.CurrencyCode,
            CurrencyDecimalPlaces = quote.CurrencyDecimalPlaces,
            NetAnnualAmount = quote.NetAnnualAmount,
            TotalLateFeeAmount = totalLateFees,
            TotalPaidAmount = totalPaid,
            TotalOutstandingAmount = outstanding,
            OverdueAmount = overdue,
            Installments = installments
        };
    }

    public async Task<PagedResponseDTO<StudentFeeReceiptResponseDTO>> GetReceiptsAsync(
        long institutionId,
        StudentFeeReceiptListRequestDTO filter,
        CancellationToken cancellationToken)
    {
        var query = from receipt in context.StudentFeeReceipts.AsNoTracking()
                    join quote in context.StudentAdmissionFeeQuotes.AsNoTracking()
                        on receipt.StudentAdmissionFeeQuoteId equals quote.Id
                    join payment in context.StudentFeePaymentTransactions.AsNoTracking()
                        on receipt.StudentFeePaymentTransactionId equals payment.Id
                    where receipt.InstitutionId == institutionId && quote.InstitutionId == institutionId && payment.InstitutionId == institutionId
                    select new
                    {
                        receipt.Id,
                        receipt.ReceiptNumber,
                        StudentAdmissionId = quote.StudentAdmissionId,
                        receipt.StudentFeeInstallmentId,
                        PaymentTransactionId = payment.Id,
                        receipt.Amount,
                        receipt.CurrencyCode,
                        receipt.IssuedDateTime,
                        PaymentMethod = payment.Method,
                        payment.RecordedReference
                    };
        if (filter.StudentAdmissionId.HasValue)
        {
            query = query.Where(item => item.StudentAdmissionId == filter.StudentAdmissionId.Value);
        }

        if (filter.StudentFeeInstallmentId.HasValue)
        {
            query = query.Where(item => item.StudentFeeInstallmentId == filter.StudentFeeInstallmentId.Value);
        }

        var page = await query.OrderByDescending(item => item.IssuedDateTime)
            .ThenByDescending(item => item.Id)
            .ToPageAsync(filter.PageNumber, filter.PageSize, cancellationToken);
        var receipts = page.Data.Select(item => new StudentFeeReceiptResponseDTO
        {
            Id = item.Id,
            ReceiptNumber = item.ReceiptNumber,
            StudentAdmissionId = item.StudentAdmissionId,
            StudentFeeInstallmentId = item.StudentFeeInstallmentId,
            PaymentTransactionId = item.PaymentTransactionId,
            Amount = item.Amount,
            CurrencyCode = item.CurrencyCode,
            IssuedAt = new DateTimeOffset(DateTime.SpecifyKind(item.IssuedDateTime, DateTimeKind.Utc)),
            PaymentMethod = item.PaymentMethod,
            RecordedReference = item.RecordedReference
        }).ToList();
        return new PagedResponseDTO<StudentFeeReceiptResponseDTO>(receipts, page.TotalCount, page.PageNumber, page.PageSize)
        {
            TotalPages = page.TotalPages
        };
    }

    public async Task<StudentFeePaymentResponseDTO> RecordOfflinePaymentAsync(
        long installmentId,
        long institutionId,
        long actorId,
        StudentOfflinePaymentRequestDTO request,
        CancellationToken cancellationToken)
    {
        return await RetrySerializationFailureAsync(() => RecordOfflinePaymentCoreAsync(
            installmentId, institutionId, actorId, request, cancellationToken));
    }

    private async Task<StudentFeePaymentResponseDTO> RecordOfflinePaymentCoreAsync(
        long installmentId,
        long institutionId,
        long actorId,
        StudentOfflinePaymentRequestDTO request,
        CancellationToken cancellationToken)
    {
        if (request.PaidAt == default || request.PaidAt > DateTimeOffset.UtcNow.AddMinutes(5) || string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            throw new ValidationErrorException("A valid payment date and idempotency key are required.");
        }

        var existing = await context.StudentFeePaymentTransactions.AsNoTracking().SingleOrDefaultAsync(item =>
            item.InstitutionId == institutionId && item.IdempotencyKey == request.IdempotencyKey.Trim(), cancellationToken);
        if (existing is not null)
        {
            var decimalPlaces = await GetPaymentDecimalPlacesAsync(existing.Id, institutionId, cancellationToken);
            if (!MatchesOfflinePayment(existing, installmentId, request, decimalPlaces))
            {
                throw new ConflictException("The idempotency key was already used for a different fee payment.");
            }

            return await GetPaymentAsync(existing.Id, institutionId, cancellationToken);
        }

        await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var idempotencyLockKey = $"{institutionId}:{request.IdempotencyKey.Trim()}";
        await context.Database.SqlQuery<bool>(
                $"SELECT pg_advisory_xact_lock(hashtextextended({idempotencyLockKey}, 0)) IS NULL AS \"Value\"")
            .SingleAsync(cancellationToken);
        existing = await context.StudentFeePaymentTransactions.AsNoTracking().SingleOrDefaultAsync(item =>
            item.InstitutionId == institutionId && item.IdempotencyKey == request.IdempotencyKey.Trim(), cancellationToken);
        if (existing is not null)
        {
            var decimalPlaces = await GetPaymentDecimalPlacesAsync(existing.Id, institutionId, cancellationToken);
            if (!MatchesOfflinePayment(existing, installmentId, request, decimalPlaces))
            {
                throw new ConflictException("The idempotency key was already used for a different fee payment.");
            }

            await transaction.CommitAsync(cancellationToken);
            return await GetPaymentAsync(existing.Id, institutionId, cancellationToken);
        }

        var installment = await context.StudentFeeInstallments.FromSqlInterpolated($"SELECT * FROM skolenza.\"StudentFeeInstallment\" WHERE \"Id\" = {installmentId} AND \"InstitutionId\" = {institutionId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Student fee installment was not found.");
        await context.Entry(installment).ReloadAsync(cancellationToken);
        var quote = await context.StudentAdmissionFeeQuotes.AsNoTracking().SingleAsync(item =>
            item.Id == installment.StudentAdmissionFeeQuoteId && item.InstitutionId == institutionId, cancellationToken);
        var structure = await context.StudentFeeStructures.AsNoTracking().SingleAsync(item =>
            item.Id == quote.StudentFeeStructureId && item.InstitutionId == institutionId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (today > installment.DueDate.AddDays(structure.GracePeriodDays))
        {
            var lateFee = structure.LateFeeCap.HasValue
                ? Math.Min(structure.LateFeePerInstallment, structure.LateFeeCap.Value)
                : structure.LateFeePerInstallment;
            installment.LateFeeAmount = StudentFeeQuoteCalculator.RoundCurrency(lateFee, quote.CurrencyDecimalPlaces);
            if (installment.Status != (short)StudentFeeInstallmentStatus.Paid)
            {
                installment.Status = (short)StudentFeeInstallmentStatus.Overdue;
            }
        }

        var outstanding = StudentFeeQuoteCalculator.RoundCurrency(
            installment.RecurringAmount + installment.OneTimeAmount + installment.LateFeeAmount - installment.PaidAmount,
            quote.CurrencyDecimalPlaces);
        var amount = StudentFeeQuoteCalculator.RoundCurrency(request.Amount, quote.CurrencyDecimalPlaces);
        if (amount <= 0m || amount > outstanding)
        {
            throw new ValidationErrorException("Payment amount must be positive and cannot exceed the current installment balance.");
        }

        var payment = new StudentFeePaymentTransaction
        {
            InstitutionId = institutionId,
            StudentFeeInstallmentId = installment.Id,
            IdempotencyKey = request.IdempotencyKey.Trim(),
            Method = (short)StudentFeePaymentMethod.Offline,
            Status = (short)StudentFeePaymentStatus.Succeeded,
            Amount = amount,
            CurrencyCode = quote.CurrencyCode,
            RecordedReference = string.IsNullOrWhiteSpace(request.RecordedReference) ? null : request.RecordedReference.Trim(),
            RecordedById = actorId,
            PaidDateTime = DateTime.SpecifyKind(request.PaidAt.UtcDateTime, DateTimeKind.Unspecified),
            AddedDateTime = DateTime.UtcNow
        };
        context.StudentFeePaymentTransactions.Add(payment);
        installment.PaidAmount = StudentFeeQuoteCalculator.RoundCurrency(installment.PaidAmount + amount, quote.CurrencyDecimalPlaces);
        installment.Status = installment.PaidAmount >= installment.RecurringAmount + installment.OneTimeAmount + installment.LateFeeAmount
            ? (short)StudentFeeInstallmentStatus.Paid
            : (short)StudentFeeInstallmentStatus.PartiallyPaid;
        await context.SaveChangesAsync(cancellationToken);
        var receipt = new StudentFeeReceipt
        {
            InstitutionId = institutionId,
            StudentFeePaymentTransactionId = payment.Id,
            StudentAdmissionFeeQuoteId = quote.Id,
            StudentFeeInstallmentId = installment.Id,
            ReceiptNumber = $"RCT-{institutionId}-{DateTime.UtcNow:yyyyMMdd}-{payment.Id:D8}",
            Amount = amount,
            CurrencyCode = quote.CurrencyCode,
            IssuedDateTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
        };
        context.StudentFeeReceipts.Add(receipt);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return MapPayment(payment, receipt.ReceiptNumber);
    }

    public async Task<StudentCashfreeCheckoutDataDTO> PrepareCashfreeCheckoutAsync(
        long installmentId,
        long institutionId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        return await RetrySerializationFailureAsync(() => PrepareCashfreeCheckoutCoreAsync(
            installmentId, institutionId, idempotencyKey, null, cancellationToken));
    }

    public async Task<StudentCashfreeCheckoutDataDTO> PreparePortalCashfreeCheckoutAsync(
        long institutionId,
        long accountId,
        long studentId,
        long admissionId,
        long installmentId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        return await RetrySerializationFailureAsync(() => PrepareCashfreeCheckoutCoreAsync(
            installmentId, institutionId, idempotencyKey,
            new PortalCheckoutScope(accountId, studentId, admissionId), cancellationToken));
    }

    private async Task<StudentCashfreeCheckoutDataDTO> PrepareCashfreeCheckoutCoreAsync(
        long installmentId,
        long institutionId,
        string idempotencyKey,
        PortalCheckoutScope? portalScope,
        CancellationToken cancellationToken)
    {
        var normalizedKey = idempotencyKey.Trim();
        if (normalizedKey.Length == 0)
        {
            throw new ValidationErrorException("An idempotency key is required.");
        }

        StudentFeePaymentAttempt? existing = null;
        if (portalScope is null)
        {
            existing = await context.StudentFeePaymentAttempts.AsNoTracking().SingleOrDefaultAsync(item =>
                item.InstitutionId == institutionId && item.IdempotencyKey == normalizedKey, cancellationToken);
            if (existing is not null)
            {
                if (existing.StudentFeeInstallmentId != installmentId || existing.GatewayCode != StudentConstants.CashfreeGatewayCode)
                {
                    throw new ConflictException("The idempotency key was already used for a different fee installment.");
                }

                return await GetCashfreeCheckoutDataAsync(existing.Id, institutionId, cancellationToken);
            }
        }

        await using var transaction = await context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);
        if (portalScope is not null)
        {
            await LockAndAuthorizePortalCheckoutAsync(
                institutionId, installmentId, portalScope, cancellationToken);
        }
        var lockKey = $"{institutionId}:{normalizedKey}";
        await context.Database.SqlQuery<bool>($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0)) IS NULL AS \"Value\"")
            .SingleAsync(cancellationToken);
        existing = await context.StudentFeePaymentAttempts.AsNoTracking().SingleOrDefaultAsync(item =>
            item.InstitutionId == institutionId && item.IdempotencyKey == normalizedKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.StudentFeeInstallmentId != installmentId || existing.GatewayCode != StudentConstants.CashfreeGatewayCode)
            {
                throw new ConflictException("The idempotency key was already used for a different fee installment.");
            }

            await transaction.CommitAsync(cancellationToken);
            return await GetCashfreeCheckoutDataAsync(existing.Id, institutionId, cancellationToken);
        }

        var installment = await context.StudentFeeInstallments.FromSqlInterpolated(
                $"SELECT * FROM skolenza.\"StudentFeeInstallment\" WHERE \"Id\" = {installmentId} AND \"InstitutionId\" = {institutionId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Student fee installment was not found.");
        await context.Entry(installment).ReloadAsync(cancellationToken);
        var quote = await context.StudentAdmissionFeeQuotes.AsNoTracking().SingleAsync(item =>
            item.Id == installment.StudentAdmissionFeeQuoteId && item.InstitutionId == institutionId, cancellationToken);
        if (quote.CurrencyDecimalPlaces == 3)
        {
            throw new ValidationErrorException("Cashfree checkout is not available for 3-decimal currencies. Record this payment offline.");
        }

        var structure = await context.StudentFeeStructures.AsNoTracking().SingleAsync(item =>
            item.Id == quote.StudentFeeStructureId && item.InstitutionId == institutionId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (today > installment.DueDate.AddDays(structure.GracePeriodDays) &&
            installment.Status != (short)StudentFeeInstallmentStatus.Paid)
        {
            installment.LateFeeAmount = StudentFeeQuoteCalculator.RoundCurrency(
                structure.LateFeeCap.HasValue
                    ? Math.Min(structure.LateFeePerInstallment, structure.LateFeeCap.Value)
                    : structure.LateFeePerInstallment,
                quote.CurrencyDecimalPlaces);
            installment.Status = (short)StudentFeeInstallmentStatus.Overdue;
        }

        var amount = StudentFeeQuoteCalculator.RoundCurrency(
            installment.RecurringAmount + installment.OneTimeAmount + installment.LateFeeAmount - installment.PaidAmount,
            quote.CurrencyDecimalPlaces);
        if (amount < 1m)
        {
            throw new ValidationErrorException("Cashfree requires a minimum order amount of 1.00. The current installment balance is not eligible for online checkout.");
        }

        var admissionId = quote.StudentAdmissionId;
        var primaryGuardian = await (from link in context.StudentAdmissionGuardians.AsNoTracking()
                                     join guardian in context.Guardians.AsNoTracking() on link.GuardianId equals guardian.Id
                                     where link.InstitutionId == institutionId && link.StudentAdmissionId == admissionId &&
                                           link.IsActive && link.IsPrimary && guardian.InstitutionId == institutionId &&
                                           guardian.IsActive && !guardian.IsSoftDeleted
                                     select new { Guardian = guardian, link.EffectiveTo })
            .SingleOrDefaultAsync(item => !item.EffectiveTo.HasValue || item.EffectiveTo.Value >= today, cancellationToken)
            ?? throw new ConflictException("Cashfree checkout requires one active primary Guardian linked to the admission.");
        if (string.IsNullOrWhiteSpace(primaryGuardian.Guardian.PhoneNumber))
        {
            throw new ValidationErrorException("Cashfree checkout requires a phone number on the primary Guardian profile.");
        }

        var attempt = new StudentFeePaymentAttempt
        {
            InstitutionId = institutionId,
            StudentFeeInstallmentId = installment.Id,
            IdempotencyKey = normalizedKey,
            GatewayCode = StudentConstants.CashfreeGatewayCode,
            GatewayOrderId = $"S{Guid.NewGuid():N}",
            Amount = amount,
            CurrencyCode = quote.CurrencyCode,
            Status = (short)StudentFeePaymentAttemptStatus.Created,
            CreatedDateTime = DateTime.UtcNow
        };
        context.StudentFeePaymentAttempts.Add(attempt);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new StudentCashfreeCheckoutDataDTO
        {
            PaymentAttemptId = attempt.Id,
            InstitutionId = institutionId,
            StudentFeeInstallmentId = installment.Id,
            GatewayOrderId = attempt.GatewayOrderId,
            CurrencyCode = attempt.CurrencyCode,
            CurrencyDecimalPlaces = quote.CurrencyDecimalPlaces,
            Amount = attempt.Amount,
            CustomerId = $"G{institutionId}-{primaryGuardian.Guardian.Id}",
            CustomerPhone = primaryGuardian.Guardian.PhoneNumber.Trim(),
            CustomerEmail = primaryGuardian.Guardian.Email,
            CustomerName = string.Join(' ', new[]
            {
                primaryGuardian.Guardian.FirstName,
                primaryGuardian.Guardian.MiddleName,
                primaryGuardian.Guardian.LastName
            }.Where(value => !string.IsNullOrWhiteSpace(value)))
        };
    }

    private async Task LockAndAuthorizePortalCheckoutAsync(
        long institutionId,
        long installmentId,
        PortalCheckoutScope scope,
        CancellationToken cancellationToken)
    {
        if (portalAuth is null)
        {
            throw new InvalidOperationException("Portal authorization is not configured for student fee checkout.");
        }

        // Student is locked first, matching Guardian-link updates and student exit.
        await context.Database.SqlQuery<long>($"SELECT \"Id\" AS \"Value\" FROM skolenza.\"Student\" WHERE \"InstitutionId\" = {institutionId} AND \"Id\" = {scope.StudentId} FOR UPDATE")
            .ToListAsync(cancellationToken);
        await context.Database.SqlQuery<long>($"SELECT \"Id\" AS \"Value\" FROM skolenza.\"StudentPortalAccount\" WHERE \"InstitutionId\" = {institutionId} AND \"Id\" = {scope.AccountId} FOR SHARE")
            .ToListAsync(cancellationToken);
        await context.Database.SqlQuery<long>($"SELECT sg.\"Id\" AS \"Value\" FROM skolenza.\"StudentGuardian\" sg JOIN skolenza.\"StudentPortalAccountGuardian\" ag ON ag.\"GuardianId\" = sg.\"GuardianId\" AND ag.\"InstitutionId\" = sg.\"InstitutionId\" JOIN skolenza.\"Guardian\" g ON g.\"Id\" = sg.\"GuardianId\" AND g.\"InstitutionId\" = sg.\"InstitutionId\" WHERE sg.\"InstitutionId\" = {institutionId} AND sg.\"StudentId\" = {scope.StudentId} AND ag.\"StudentPortalAccountId\" = {scope.AccountId} ORDER BY sg.\"Id\" FOR SHARE OF sg, ag, g")
            .ToListAsync(cancellationToken);
        await context.Database.SqlQuery<long>($"SELECT \"Id\" AS \"Value\" FROM skolenza.\"StudentAdmission\" WHERE \"InstitutionId\" = {institutionId} AND \"Id\" = {scope.AdmissionId} FOR SHARE")
            .ToListAsync(cancellationToken);

        var profile = await portalAuth.GetProfileAsync(institutionId, scope.AccountId, cancellationToken);
        var hasGuardianAccess = profile?.Personas.Any(persona =>
            persona.Type == StudentPortalPersonaType.Guardian &&
            persona.LinkedStudents.Any(student => student.StudentId == scope.StudentId)) == true;
        if (!hasGuardianAccess ||
            !await AdmissionBelongsToStudentAsync(institutionId, scope.StudentId, scope.AdmissionId, cancellationToken) ||
            !await InstallmentBelongsToAdmissionAsync(institutionId, scope.AdmissionId, installmentId, cancellationToken))
        {
            throw new NotFoundException("Student fee information was not found.");
        }
    }

    public async Task<StudentCashfreeCheckoutResponseDTO> SaveCashfreeOrderAsync(
        long attemptId,
        long institutionId,
        string paymentSessionId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        var attempt = await context.StudentFeePaymentAttempts.SingleOrDefaultAsync(item =>
            item.Id == attemptId && item.InstitutionId == institutionId &&
            item.GatewayCode == StudentConstants.CashfreeGatewayCode, cancellationToken)
            ?? throw new NotFoundException("Cashfree payment attempt was not found.");
        if (!string.IsNullOrWhiteSpace(attempt.GatewayPaymentSessionId) &&
            !string.Equals(attempt.GatewayPaymentSessionId, paymentSessionId, StringComparison.Ordinal))
        {
            throw new ConflictException("Cashfree returned a different payment session for this idempotent order.");
        }

        attempt.GatewayPaymentSessionId = paymentSessionId;
        attempt.ExpiresDateTime = DateTime.SpecifyKind(expiresAt.UtcDateTime, DateTimeKind.Unspecified);
        if (attempt.Status == (short)StudentFeePaymentAttemptStatus.Created)
        {
            attempt.Status = (short)StudentFeePaymentAttemptStatus.Pending;
        }

        await context.SaveChangesAsync(cancellationToken);
        return new StudentCashfreeCheckoutResponseDTO
        {
            PaymentAttemptId = attempt.Id,
            GatewayOrderId = attempt.GatewayOrderId,
            PaymentSessionId = attempt.GatewayPaymentSessionId,
            CurrencyCode = attempt.CurrencyCode,
            Amount = attempt.Amount,
            ExpiresAt = expiresAt
        };
    }

    public Task<StudentCashfreeCheckoutDataDTO> GetCashfreeAttemptAsync(
        long attemptId,
        long institutionId,
        CancellationToken cancellationToken) =>
        GetCashfreeAttemptDataAsync(attemptId, institutionId, cancellationToken);

    public async Task<StudentCashfreeCheckoutDataDTO> GetCashfreeAttemptByOrderAsync(
        string gatewayOrderId,
        CancellationToken cancellationToken)
    {
        var attempt = await context.StudentFeePaymentAttempts.AsNoTracking().SingleOrDefaultAsync(item =>
            item.GatewayOrderId == gatewayOrderId && item.GatewayCode == StudentConstants.CashfreeGatewayCode,
            cancellationToken) ?? throw new NotFoundException("Cashfree payment order was not found.");
        return await GetCashfreeAttemptDataAsync(attempt.Id, attempt.InstitutionId, cancellationToken);
    }

    public async Task<StudentCashfreePaymentStatusResponseDTO> RecordCashfreePaymentAsync(
        long attemptId,
        StudentCashfreePaymentDetailsDTO payment,
        string? webhookEventId,
        string? webhookEventType,
        string? webhookPayloadSha256,
        CancellationToken cancellationToken)
    {
        return await RetrySerializationFailureAsync(() => RecordCashfreePaymentCoreAsync(
            attemptId, payment, webhookEventId, webhookEventType, webhookPayloadSha256, cancellationToken));
    }

    private async Task<StudentCashfreePaymentStatusResponseDTO> RecordCashfreePaymentCoreAsync(
        long attemptId,
        StudentCashfreePaymentDetailsDTO payment,
        string? webhookEventId,
        string? webhookEventType,
        string? webhookPayloadSha256,
        CancellationToken cancellationToken)
    {
        var attempt = await context.StudentFeePaymentAttempts.AsNoTracking().SingleOrDefaultAsync(item =>
            item.Id == attemptId && item.GatewayCode == StudentConstants.CashfreeGatewayCode, cancellationToken)
            ?? throw new NotFoundException("Cashfree payment attempt was not found.");
        var attemptStatus = CashfreeAttemptStatus(payment.Status);
        await using var transaction = await context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);

        StudentFeePaymentWebhookEvent? webhookEvent = null;
        if (!string.IsNullOrWhiteSpace(webhookEventId))
        {
            var alreadySeen = await context.StudentFeePaymentWebhookEvents.AnyAsync(item =>
                item.EventId == webhookEventId, cancellationToken);
            if (alreadySeen)
            {
                await transaction.CommitAsync(cancellationToken);
                return await GetCashfreePaymentStatusAsync(attemptId, attempt.InstitutionId, cancellationToken);
            }

            webhookEvent = new StudentFeePaymentWebhookEvent
            {
                StudentFeePaymentAttemptId = attemptId,
                EventId = webhookEventId,
                EventType = string.IsNullOrWhiteSpace(webhookEventType) ? "UNKNOWN" : webhookEventType[..Math.Min(100, webhookEventType.Length)],
                PayloadSha256 = webhookPayloadSha256 ?? string.Empty,
                ProcessingStatus = (short)StudentFeeWebhookProcessingStatus.Pending,
                ReceivedDateTime = DateTime.UtcNow
            };
            context.StudentFeePaymentWebhookEvents.Add(webhookEvent);
        }

        var currentAttempt = await context.StudentFeePaymentAttempts.FromSqlInterpolated(
                $"SELECT * FROM skolenza.\"StudentFeePaymentAttempt\" WHERE \"Id\" = {attemptId} FOR UPDATE")
            .SingleAsync(cancellationToken);
        if (!string.Equals(payment.OrderId, currentAttempt.GatewayOrderId, StringComparison.Ordinal) ||
            !string.Equals(payment.CurrencyCode, currentAttempt.CurrencyCode, StringComparison.OrdinalIgnoreCase) ||
            payment.Amount != currentAttempt.Amount)
        {
            throw new ConflictException("Cashfree payment order, amount, or currency does not match the stored fee attempt.");
        }

        var existingTransaction = await context.StudentFeePaymentTransactions.AsNoTracking().SingleOrDefaultAsync(item =>
            item.StudentFeePaymentAttemptId == attemptId && item.GatewayPaymentId == payment.PaymentId, cancellationToken);
        if (string.Equals(payment.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase) && existingTransaction is null)
        {
            var installment = await context.StudentFeeInstallments.FromSqlInterpolated(
                    $"SELECT * FROM skolenza.\"StudentFeeInstallment\" WHERE \"Id\" = {currentAttempt.StudentFeeInstallmentId} AND \"InstitutionId\" = {currentAttempt.InstitutionId} FOR UPDATE")
                .SingleAsync(cancellationToken);
            var quote = await context.StudentAdmissionFeeQuotes.AsNoTracking().SingleAsync(item =>
                item.Id == installment.StudentAdmissionFeeQuoteId && item.InstitutionId == currentAttempt.InstitutionId,
                cancellationToken);
            var outstanding = StudentFeeQuoteCalculator.RoundCurrency(
                installment.RecurringAmount + installment.OneTimeAmount + installment.LateFeeAmount - installment.PaidAmount,
                quote.CurrencyDecimalPlaces);
            if (payment.Amount > outstanding)
            {
                throw new ConflictException("Cashfree confirmed more than the remaining installment balance. Payment has not been posted; reconciliation is required.");
            }

            var paymentTransaction = new StudentFeePaymentTransaction
            {
                InstitutionId = currentAttempt.InstitutionId,
                StudentFeeInstallmentId = installment.Id,
                StudentFeePaymentAttemptId = currentAttempt.Id,
                GatewayPaymentId = payment.PaymentId,
                Method = (short)StudentFeePaymentMethod.Cashfree,
                Status = (short)StudentFeePaymentStatus.Succeeded,
                Amount = payment.Amount,
                CurrencyCode = quote.CurrencyCode,
                BankReference = payment.BankReference,
                PaidDateTime = DateTime.SpecifyKind(payment.PaidAt?.UtcDateTime ?? DateTime.UtcNow, DateTimeKind.Unspecified),
                AddedDateTime = DateTime.UtcNow
            };
            context.StudentFeePaymentTransactions.Add(paymentTransaction);
            installment.PaidAmount = StudentFeeQuoteCalculator.RoundCurrency(
                installment.PaidAmount + payment.Amount, quote.CurrencyDecimalPlaces);
            installment.Status = installment.PaidAmount >= installment.RecurringAmount + installment.OneTimeAmount + installment.LateFeeAmount
                ? (short)StudentFeeInstallmentStatus.Paid
                : (short)StudentFeeInstallmentStatus.PartiallyPaid;
            currentAttempt.Status = (short)StudentFeePaymentAttemptStatus.Succeeded;
            await context.SaveChangesAsync(cancellationToken);
            context.StudentFeeReceipts.Add(new StudentFeeReceipt
            {
                InstitutionId = currentAttempt.InstitutionId,
                StudentFeePaymentTransactionId = paymentTransaction.Id,
                StudentAdmissionFeeQuoteId = quote.Id,
                StudentFeeInstallmentId = installment.Id,
                ReceiptNumber = $"RCT-{currentAttempt.InstitutionId}-{DateTime.UtcNow:yyyyMMdd}-{paymentTransaction.Id:D8}",
                Amount = payment.Amount,
                CurrencyCode = quote.CurrencyCode,
                IssuedDateTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
            });
        }
        else if (existingTransaction is null)
        {
            currentAttempt.Status = (short)attemptStatus;
        }

        if (webhookEvent is not null)
        {
            webhookEvent.ProcessingStatus = (short)StudentFeeWebhookProcessingStatus.Processed;
            webhookEvent.ProcessedDateTime = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetCashfreePaymentStatusAsync(attemptId, currentAttempt.InstitutionId, cancellationToken);
    }

    private async Task<StudentCashfreeCheckoutDataDTO> GetCashfreeCheckoutDataAsync(
        long attemptId,
        long institutionId,
        CancellationToken cancellationToken)
    {
        var row = await (from attempt in context.StudentFeePaymentAttempts.AsNoTracking()
                         join installment in context.StudentFeeInstallments.AsNoTracking() on attempt.StudentFeeInstallmentId equals installment.Id
                         join quote in context.StudentAdmissionFeeQuotes.AsNoTracking() on installment.StudentAdmissionFeeQuoteId equals quote.Id
                         join admission in context.StudentAdmissions.AsNoTracking() on quote.StudentAdmissionId equals admission.Id
                         join link in context.StudentAdmissionGuardians.AsNoTracking() on admission.Id equals link.StudentAdmissionId
                         join guardian in context.Guardians.AsNoTracking() on link.GuardianId equals guardian.Id
                         where attempt.Id == attemptId && attempt.InstitutionId == institutionId && quote.InstitutionId == institutionId &&
                               link.InstitutionId == institutionId && guardian.InstitutionId == institutionId && link.IsActive && link.IsPrimary &&
                               guardian.IsActive && !guardian.IsSoftDeleted
                         select new { attempt, quote, Guardian = guardian, link.EffectiveTo })
            .SingleOrDefaultAsync(item => !item.EffectiveTo.HasValue || item.EffectiveTo.Value >= DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken)
            ?? throw new NotFoundException("Cashfree fee attempt or its primary Guardian could not be found.");
        return new StudentCashfreeCheckoutDataDTO
        {
            PaymentAttemptId = row.attempt.Id,
            InstitutionId = institutionId,
            StudentFeeInstallmentId = row.attempt.StudentFeeInstallmentId,
            GatewayOrderId = row.attempt.GatewayOrderId,
            PaymentSessionId = row.attempt.GatewayPaymentSessionId,
            ExpiresAt = row.attempt.ExpiresDateTime.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(row.attempt.ExpiresDateTime.Value, DateTimeKind.Utc))
                : null,
            CurrencyCode = row.attempt.CurrencyCode,
            CurrencyDecimalPlaces = row.quote.CurrencyDecimalPlaces,
            Amount = row.attempt.Amount,
            CustomerId = $"G{institutionId}-{row.Guardian.Id}",
            CustomerPhone = row.Guardian.PhoneNumber ?? string.Empty,
            CustomerEmail = row.Guardian.Email,
            CustomerName = string.Join(' ', new[] { row.Guardian.FirstName, row.Guardian.MiddleName, row.Guardian.LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value)))
        };
    }

    private async Task<StudentCashfreeCheckoutDataDTO> GetCashfreeAttemptDataAsync(
        long attemptId,
        long institutionId,
        CancellationToken cancellationToken)
    {
        var row = await (from attempt in context.StudentFeePaymentAttempts.AsNoTracking()
                         join installment in context.StudentFeeInstallments.AsNoTracking() on attempt.StudentFeeInstallmentId equals installment.Id
                         join quote in context.StudentAdmissionFeeQuotes.AsNoTracking() on installment.StudentAdmissionFeeQuoteId equals quote.Id
                         where attempt.Id == attemptId && attempt.InstitutionId == institutionId &&
                               attempt.GatewayCode == StudentConstants.CashfreeGatewayCode && quote.InstitutionId == institutionId
                         select new { attempt, quote })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Cashfree payment attempt was not found.");

        return new StudentCashfreeCheckoutDataDTO
        {
            PaymentAttemptId = row.attempt.Id,
            InstitutionId = institutionId,
            StudentFeeInstallmentId = row.attempt.StudentFeeInstallmentId,
            GatewayOrderId = row.attempt.GatewayOrderId,
            PaymentSessionId = row.attempt.GatewayPaymentSessionId,
            ExpiresAt = row.attempt.ExpiresDateTime.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(row.attempt.ExpiresDateTime.Value, DateTimeKind.Utc))
                : null,
            CurrencyCode = row.attempt.CurrencyCode,
            CurrencyDecimalPlaces = row.quote.CurrencyDecimalPlaces,
            Amount = row.attempt.Amount
        };
    }

    private async Task<StudentCashfreePaymentStatusResponseDTO> GetCashfreePaymentStatusAsync(
        long attemptId,
        long institutionId,
        CancellationToken cancellationToken)
    {
        var attempt = await context.StudentFeePaymentAttempts.AsNoTracking().SingleAsync(item =>
            item.Id == attemptId && item.InstitutionId == institutionId, cancellationToken);
        var payment = await context.StudentFeePaymentTransactions.AsNoTracking().SingleOrDefaultAsync(item =>
            item.StudentFeePaymentAttemptId == attemptId && item.Status == (short)StudentFeePaymentStatus.Succeeded,
            cancellationToken);
        var receipt = payment is null ? null : await context.StudentFeeReceipts.AsNoTracking()
            .Where(item => item.StudentFeePaymentTransactionId == payment.Id)
            .Select(item => item.ReceiptNumber)
            .SingleOrDefaultAsync(cancellationToken);
        return new StudentCashfreePaymentStatusResponseDTO
        {
            PaymentAttemptId = attempt.Id,
            GatewayOrderId = attempt.GatewayOrderId,
            AttemptStatus = attempt.Status,
            PaymentTransactionId = payment?.Id,
            PaymentStatus = payment?.Status,
            GatewayPaymentId = payment?.GatewayPaymentId,
            CurrencyCode = attempt.CurrencyCode,
            Amount = attempt.Amount,
            ReceiptNumber = receipt,
            PaidAt = payment?.PaidDateTime.HasValue == true
                ? new DateTimeOffset(DateTime.SpecifyKind(payment.PaidDateTime.Value, DateTimeKind.Utc))
                : null
        };
    }

    private static StudentFeePaymentAttemptStatus CashfreeAttemptStatus(string status) => status.ToUpperInvariant() switch
    {
        "SUCCESS" => StudentFeePaymentAttemptStatus.Succeeded,
        "FAILED" or "USER_DROPPED" or "VOID" or "CANCELLED" => StudentFeePaymentAttemptStatus.Failed,
        _ => StudentFeePaymentAttemptStatus.Pending
    };

    private async Task<StudentAdmissionFeeQuoteResponseDTO> GetQuoteAsync(long id, long institutionId, CancellationToken cancellationToken)
    {
        var quote = await context.StudentAdmissionFeeQuotes.AsNoTracking().SingleOrDefaultAsync(item =>
            item.Id == id && item.InstitutionId == institutionId, cancellationToken)
            ?? throw new NotFoundException("Admission fee quote was not found.");
        var lines = await context.StudentAdmissionFeeQuoteLines.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && item.StudentAdmissionFeeQuoteId == id)
            .OrderBy(item => item.Id)
            .Select(item => new StudentAdmissionFeeQuoteLineResponseDTO
            {
                ComponentCode = item.ComponentCode,
                ComponentName = item.ComponentName,
                ChargeNature = item.ChargeNature,
                AnnualAmount = item.AnnualAmount,
                DiscountAmount = item.DiscountAmount,
                NetAmount = item.NetAmount
            }).ToListAsync(cancellationToken);
        var installments = await context.StudentFeeInstallments.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && item.StudentAdmissionFeeQuoteId == id)
            .OrderBy(item => item.InstallmentNumber)
            .Select(item => new StudentFeeInstallmentResponseDTO
            {
                Id = item.Id,
                QuoteId = id,
                InstallmentNumber = item.InstallmentNumber,
                DueDate = item.DueDate,
                RecurringAmount = item.RecurringAmount,
                OneTimeAmount = item.OneTimeAmount,
                LateFeeAmount = item.LateFeeAmount,
                PaidAmount = item.PaidAmount,
                OutstandingAmount = item.RecurringAmount + item.OneTimeAmount + item.LateFeeAmount - item.PaidAmount,
                Status = item.Status
            }).ToListAsync(cancellationToken);
        return new StudentAdmissionFeeQuoteResponseDTO
        {
            Id = quote.Id,
            StudentAdmissionId = quote.StudentAdmissionId,
            StudentFeeStructureId = quote.StudentFeeStructureId,
            StudentFeeStructureFrequencyId = quote.StudentFeeStructureFrequencyId,
            CurrencyCode = quote.CurrencyCode,
            CurrencyDecimalPlaces = quote.CurrencyDecimalPlaces,
            DiscountPercent = quote.DiscountPercent,
            GrossRecurringAmount = quote.GrossRecurringAmount,
            DiscountAmount = quote.DiscountAmount,
            OneTimeAmount = quote.OneTimeAmount,
            NetAnnualAmount = quote.NetAnnualAmount,
            InstallmentsPerYear = quote.InstallmentsPerYear,
            Lines = lines,
            Installments = installments
        };
    }

    private async Task<StudentFeePaymentResponseDTO> GetPaymentAsync(long id, long institutionId, CancellationToken cancellationToken)
    {
        var payment = await context.StudentFeePaymentTransactions.AsNoTracking().SingleAsync(item =>
            item.Id == id && item.InstitutionId == institutionId, cancellationToken);
        var receipt = await context.StudentFeeReceipts.AsNoTracking()
            .Where(item => item.StudentFeePaymentTransactionId == payment.Id && item.InstitutionId == institutionId)
            .Select(item => item.ReceiptNumber)
            .SingleOrDefaultAsync(cancellationToken);
        return MapPayment(payment, receipt);
    }

    private static StudentFeeStructureResponseDTO MapStructure(
        StudentFeeStructure entity,
        string campusName,
        string sessionName,
        string groupName,
        List<StudentFeeStructureItemResponseDTO> items,
        List<StudentFeeStructureFrequencyResponseDTO> frequencies) => new()
    {
        Id = entity.Id,
        CampusId = entity.CampusId,
        CampusName = campusName,
        AcademicSessionId = entity.AcademicSessionId,
        AcademicSessionName = sessionName,
        AcademicGroupId = entity.AcademicGroupId,
        AcademicGroupName = groupName,
        CurrencyCode = entity.CurrencyCode,
        CurrencyDecimalPlaces = entity.CurrencyDecimalPlaces,
        LateFeePerInstallment = entity.LateFeePerInstallment,
        GracePeriodDays = entity.GracePeriodDays,
        LateFeeCap = entity.LateFeeCap,
        IsActive = entity.IsActive,
        Items = items,
        Frequencies = frequencies
    };

    private static StudentFeePaymentResponseDTO MapPayment(StudentFeePaymentTransaction payment, string? receiptNumber) => new()
    {
        Id = payment.Id,
        StudentFeeInstallmentId = payment.StudentFeeInstallmentId,
        Method = payment.Method,
        Status = payment.Status,
        Amount = payment.Amount,
        CurrencyCode = payment.CurrencyCode,
        GatewayPaymentId = payment.GatewayPaymentId,
        ReceiptNumber = receiptNumber,
        PaidAt = payment.PaidDateTime.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(payment.PaidDateTime.Value, DateTimeKind.Utc)) : null
    };

    private async Task<T> RetrySerializationFailureAsync<T>(Func<Task<T>> operation)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception exception) when (IsSerializationFailure(exception))
            {
                context.ChangeTracker.Clear();
                if (attempt >= StudentConstants.FeeTransactionMaximumAttempts)
                {
                    throw new ConflictException(StudentConstants.FeeTransactionConcurrencyMessage);
                }
            }
        }
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

    private async Task<int> GetPaymentDecimalPlacesAsync(
        long paymentId,
        long institutionId,
        CancellationToken cancellationToken)
    {
        return await (from payment in context.StudentFeePaymentTransactions.AsNoTracking()
                      join installment in context.StudentFeeInstallments.AsNoTracking()
                          on payment.StudentFeeInstallmentId equals installment.Id
                      join quote in context.StudentAdmissionFeeQuotes.AsNoTracking()
                          on installment.StudentAdmissionFeeQuoteId equals quote.Id
                      where payment.Id == paymentId && payment.InstitutionId == institutionId &&
                            quote.InstitutionId == institutionId
                      select (int)quote.CurrencyDecimalPlaces)
            .SingleAsync(cancellationToken);
    }

    private static bool MatchesOfflinePayment(
        StudentFeePaymentTransaction payment,
        long installmentId,
        StudentOfflinePaymentRequestDTO request,
        int currencyDecimalPlaces) =>
        payment.StudentFeeInstallmentId == installmentId &&
        payment.Amount == StudentFeeQuoteCalculator.RoundCurrency(request.Amount, currencyDecimalPlaces) &&
        payment.RecordedReference == (string.IsNullOrWhiteSpace(request.RecordedReference) ? null : request.RecordedReference.Trim()) &&
        payment.PaidDateTime.HasValue &&
        payment.PaidDateTime.Value.Ticks / TimeSpan.TicksPerMicrosecond ==
            request.PaidAt.UtcDateTime.Ticks / TimeSpan.TicksPerMicrosecond;
}
