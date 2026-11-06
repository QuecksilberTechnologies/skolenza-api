using skolenza.application.Exceptions;

namespace skolenza.application.Common.Helpers;

/// <summary>Splits immutable quote head amounts across installment totals without losing currency units.</summary>
public static class StudentFeeHeadScheduleCalculator
{
    public sealed record Head(long Id, decimal Amount);
    public sealed record Installment(short Number, decimal Amount);
    public sealed record ScheduledHead(short InstallmentNumber, long HeadId, decimal Amount);

    public static IReadOnlyList<ScheduledHead> Distribute(
        IReadOnlyCollection<Head> heads,
        IReadOnlyCollection<Installment> installments,
        int currencyDecimalPlaces)
    {
        if (currencyDecimalPlaces is not 0 and not 2 and not 3 ||
            heads.Any(item => item.Id <= 0 || item.Amount < 0 ||
                item.Amount != StudentFeeQuoteCalculator.RoundCurrency(item.Amount, currencyDecimalPlaces)) ||
            installments.Any(item => item.Number <= 0 || item.Amount < 0 ||
                item.Amount != StudentFeeQuoteCalculator.RoundCurrency(item.Amount, currencyDecimalPlaces)) ||
            heads.GroupBy(item => item.Id).Any(group => group.Count() > 1) ||
            installments.GroupBy(item => item.Number).Any(group => group.Count() > 1) ||
            heads.Sum(item => item.Amount) != installments.Sum(item => item.Amount))
        {
            throw new ValidationErrorException("Fee heads and installments must have matching nonnegative currency totals.");
        }

        var scale = currencyDecimalPlaces switch { 0 => 1m, 2 => 100m, _ => 1000m };
        var remaining = heads.OrderBy(item => item.Id).ToDictionary(item => item.Id, item => item.Amount * scale);
        var result = new List<ScheduledHead>();
        foreach (var installment in installments.OrderBy(item => item.Number))
        {
            var units = installment.Amount * scale;
            var totalRemaining = remaining.Values.Sum();
            if (units == 0)
            {
                continue;
            }

            var candidates = remaining.Select(item =>
            {
                var exact = item.Value / totalRemaining * units;
                var floor = Math.Min(item.Value, decimal.Floor(exact));
                return new { Id = item.Key, Exact = exact, Floor = floor };
            }).ToArray();
            var assigned = candidates.ToDictionary(item => item.Id, item => item.Floor);
            var remainder = units - assigned.Values.Sum();
            foreach (var candidate in candidates.OrderByDescending(item => item.Exact - item.Floor).ThenBy(item => item.Id))
            {
                if (remainder == 0)
                {
                    break;
                }

                if (assigned[candidate.Id] < remaining[candidate.Id])
                {
                    assigned[candidate.Id]++;
                    remainder--;
                }
            }

            if (remainder != 0)
            {
                throw new ValidationErrorException("Fee head schedule could not be reconciled.");
            }

            foreach (var item in assigned.Where(item => item.Value > 0).OrderBy(item => item.Key))
            {
                remaining[item.Key] -= item.Value;
                result.Add(new ScheduledHead(installment.Number, item.Key, item.Value / scale));
            }
        }

        if (remaining.Values.Any(amount => amount != 0))
        {
            throw new ValidationErrorException("Fee head schedule left an unallocated balance.");
        }

        return result;
    }
}
