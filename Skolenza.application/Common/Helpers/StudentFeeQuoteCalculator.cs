using skolenza.application.Common.Enums;
using skolenza.application.DTOS.Student;
using skolenza.application.Exceptions;

namespace skolenza.application.Common.Helpers;

public static class StudentFeeQuoteCalculator
{
    public static decimal RoundCurrency(decimal amount, int decimalPlaces = 2) =>
        Math.Round(amount, decimalPlaces, MidpointRounding.AwayFromZero);

    public static StudentAdmissionFeeQuoteResponseDTO Calculate(
        long admissionId,
        long structureId,
        long frequencyId,
        string currencyCode,
        short installmentsPerYear,
        decimal discountPercent,
        IReadOnlyCollection<StudentFeeStructureItemResponseDTO> items,
        IReadOnlyCollection<StudentFeeStructureDueDateResponseDTO> dueDates,
        int currencyDecimalPlaces = 2)
    {
        if (currencyDecimalPlaces is not 0 and not 2 and not 3)
        {
            throw new ValidationErrorException("ISO currency precision must be zero, two, or three decimal places.");
        }

        decimal Round(decimal amount) => RoundCurrency(amount, currencyDecimalPlaces);
        if (installmentsPerYear <= 0 || dueDates.Count != installmentsPerYear ||
            dueDates.Select(item => item.InstallmentNumber).Distinct().Count() != installmentsPerYear ||
            dueDates.Any(item => item.InstallmentNumber < 1 || item.InstallmentNumber > installmentsPerYear))
        {
            throw new ValidationErrorException("The selected fee frequency must have one configured due date for every installment.");
        }

        var orderedDueDates = dueDates.OrderBy(item => item.InstallmentNumber).ToArray();
        if (orderedDueDates.Where((item, index) => index > 0 && item.DueDate < orderedDueDates[index - 1].DueDate).Any())
        {
            throw new ValidationErrorException("Installment due dates must be in chronological order.");
        }

        var recurring = Round(items
            .Where(item => item.ChargeNature == (short)StudentFeeComponentChargeNature.RecurringAnnual)
            .Sum(item => item.Amount));
        var oneTime = Round(items
            .Where(item => item.ChargeNature == (short)StudentFeeComponentChargeNature.OneTime)
            .Sum(item => item.Amount));
        var discount = Round(recurring * discountPercent / 100m);
        var netRecurring = Round(recurring - discount);
        var regularInstallmentAmount = Round(netRecurring / installmentsPerYear);
        var installments = new List<StudentFeeInstallmentResponseDTO>(installmentsPerYear);
        var allocatedRecurring = 0m;

        foreach (var dueDate in orderedDueDates)
        {
            var recurringAmount = dueDate.InstallmentNumber == installmentsPerYear
                ? Round(netRecurring - allocatedRecurring)
                : regularInstallmentAmount;
            allocatedRecurring = Round(allocatedRecurring + recurringAmount);
            installments.Add(new StudentFeeInstallmentResponseDTO
            {
                QuoteId = 0,
                InstallmentNumber = dueDate.InstallmentNumber,
                DueDate = dueDate.DueDate,
                RecurringAmount = recurringAmount,
                OneTimeAmount = dueDate.InstallmentNumber == 1 ? oneTime : 0m,
                OutstandingAmount = Round(recurringAmount + (dueDate.InstallmentNumber == 1 ? oneTime : 0m)),
                Status = (short)StudentFeeInstallmentStatus.Due
            });
        }

        var lines = items.Select(item =>
        {
            var discountAmount = item.ChargeNature == (short)StudentFeeComponentChargeNature.RecurringAnnual
                ? Round(item.Amount * discountPercent / 100m)
                : 0m;
            return new StudentAdmissionFeeQuoteLineResponseDTO
            {
                ComponentCode = item.ComponentCode,
                ComponentName = item.ComponentName,
                ChargeNature = item.ChargeNature,
                AnnualAmount = item.Amount,
                DiscountAmount = discountAmount,
                NetAmount = Round(item.Amount - discountAmount)
            };
        }).ToList();
        var lineDiscountTotal = Round(lines.Sum(item => item.DiscountAmount));
        var discountRemainder = Round(discount - lineDiscountTotal);
        if (discountRemainder != 0m)
        {
            var lastRecurringLine = lines.FindLastIndex(item =>
                item.ChargeNature == (short)StudentFeeComponentChargeNature.RecurringAnnual);
            if (lastRecurringLine < 0)
            {
                throw new ValidationErrorException("A recurring discount requires at least one recurring annual fee component.");
            }

            var adjustedLine = lines[lastRecurringLine];
            var adjustedDiscount = Round(adjustedLine.DiscountAmount + discountRemainder);
            if (adjustedDiscount < 0m || adjustedDiscount > adjustedLine.AnnualAmount)
            {
                throw new ValidationErrorException("The rounded discount cannot be allocated to the configured recurring fee components.");
            }

            adjustedLine.DiscountAmount = adjustedDiscount;
            adjustedLine.NetAmount = Round(adjustedLine.AnnualAmount - adjustedDiscount);
        }

        return new StudentAdmissionFeeQuoteResponseDTO
        {
            StudentAdmissionId = admissionId,
            StudentFeeStructureId = structureId,
            StudentFeeStructureFrequencyId = frequencyId,
            CurrencyCode = currencyCode,
            CurrencyDecimalPlaces = (short)currencyDecimalPlaces,
            DiscountPercent = discountPercent,
            GrossRecurringAmount = recurring,
            DiscountAmount = discount,
            OneTimeAmount = oneTime,
            NetAnnualAmount = Round(netRecurring + oneTime),
            InstallmentsPerYear = installmentsPerYear,
            Lines = lines,
            Installments = installments
        };
    }
}
