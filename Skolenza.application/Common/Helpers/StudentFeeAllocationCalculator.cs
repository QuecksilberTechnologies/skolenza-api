using skolenza.application.Exceptions;

namespace skolenza.application.Common.Helpers;

/// <summary>Calculates allocation lines from already-authorized, locked fee balances.</summary>
public static class StudentFeeAllocationCalculator
{
    public sealed record OutstandingHead(
        long InstallmentId,
        DateOnly DueDate,
        long HeadId,
        int Priority,
        decimal OutstandingAmount,
        bool IsLateFee = false);

    public sealed record Allocation(long InstallmentId, long HeadId, decimal Amount, bool IsLateFee = false);

    public sealed record PostedAllocation(
        long AllocationId,
        long? HeadId,
        int Priority,
        bool IsLateFee,
        decimal Amount,
        decimal AlreadyReversed);

    public sealed record ReversalAllocation(long AllocationId, decimal Amount);

    /// <summary>Reverses the most recently allocated charge categories first.</summary>
    public static IReadOnlyList<ReversalAllocation> AllocateReversal(
        decimal amount,
        int currencyDecimalPlaces,
        IReadOnlyCollection<PostedAllocation> allocations)
    {
        if (currencyDecimalPlaces is not 0 and not 2 and not 3 || amount <= 0 ||
            amount != StudentFeeQuoteCalculator.RoundCurrency(amount, currencyDecimalPlaces) ||
            allocations.Any(item => item.AllocationId <= 0 ||
                (item.IsLateFee ? item.HeadId.HasValue : !item.HeadId.HasValue || item.HeadId <= 0) ||
                item.Amount <= 0 ||
                item.AlreadyReversed < 0 || item.AlreadyReversed > item.Amount ||
                item.Amount != StudentFeeQuoteCalculator.RoundCurrency(item.Amount, currencyDecimalPlaces) ||
                item.AlreadyReversed != StudentFeeQuoteCalculator.RoundCurrency(item.AlreadyReversed, currencyDecimalPlaces)) ||
            allocations.GroupBy(item => item.AllocationId).Any(group => group.Count() > 1) ||
            allocations.GroupBy(item => item.HeadId).Any(group => group.Count() > 1))
        {
            throw new ValidationErrorException("Invalid fee reversal amount, currency precision or payment allocations.");
        }

        if (amount > allocations.Sum(item => item.Amount - item.AlreadyReversed))
        {
            throw new ValidationErrorException("Reversal cannot exceed the payment amount still available for reversal.");
        }

        var remaining = amount;
        var result = new List<ReversalAllocation>();
        foreach (var item in allocations.OrderByDescending(item => item.IsLateFee)
                     .ThenByDescending(item => item.Priority)
                     .ThenByDescending(item => item.HeadId)
                     .ThenByDescending(item => item.AllocationId))
        {
            if (remaining == 0m)
            {
                break;
            }

            var applied = Math.Min(remaining, item.Amount - item.AlreadyReversed);
            if (applied > 0m)
            {
                result.Add(new ReversalAllocation(item.AllocationId, applied));
                remaining -= applied;
            }
        }

        return result;
    }

    /// <summary>Applies a payment to the oldest installment first and then configured head priority.</summary>
    public static IReadOnlyList<Allocation> AllocateOldestFirst(
        decimal amount,
        int currencyDecimalPlaces,
        IReadOnlyCollection<OutstandingHead> heads) =>
        Allocate(amount, currencyDecimalPlaces, heads, null);

    /// <summary>Applies a payment to a selected installment before other outstanding installments.</summary>
    public static IReadOnlyList<Allocation> AllocateSelectedInstallmentFirst(
        decimal amount,
        int currencyDecimalPlaces,
        IReadOnlyCollection<OutstandingHead> heads,
        long selectedInstallmentId)
    {
        if (selectedInstallmentId <= 0 ||
            !heads.Any(item => item.InstallmentId == selectedInstallmentId && item.OutstandingAmount > 0))
        {
            throw new ValidationErrorException("The selected installment must have an outstanding fee head.");
        }

        return Allocate(amount, currencyDecimalPlaces, heads, selectedInstallmentId);
    }

    private static IReadOnlyList<Allocation> Allocate(
        decimal amount,
        int currencyDecimalPlaces,
        IReadOnlyCollection<OutstandingHead> heads,
        long? selectedInstallmentId)
    {
        if (currencyDecimalPlaces is not 0 and not 2 and not 3 ||
            amount <= 0 || amount != StudentFeeQuoteCalculator.RoundCurrency(amount, currencyDecimalPlaces) ||
            heads.Any(item => item.InstallmentId <= 0 ||
                (item.IsLateFee ? item.HeadId != 0 : item.HeadId <= 0) ||
                item.OutstandingAmount < 0 ||
                item.OutstandingAmount != StudentFeeQuoteCalculator.RoundCurrency(item.OutstandingAmount, currencyDecimalPlaces)) ||
            heads.GroupBy(item => (item.InstallmentId, item.HeadId)).Any(group => group.Count() > 1) ||
            heads.GroupBy(item => item.InstallmentId).Any(group => group.Select(item => item.DueDate).Distinct().Count() > 1))
        {
            throw new ValidationErrorException("Invalid fee allocation amount, currency precision or outstanding heads.");
        }

        if (amount > heads.Sum(item => item.OutstandingAmount))
        {
            throw new ValidationErrorException("Payment cannot exceed the outstanding fee balance.");
        }

        var remaining = amount;
        var allocations = new List<Allocation>();
        foreach (var head in heads.OrderBy(item =>
                         selectedInstallmentId.HasValue && item.InstallmentId == selectedInstallmentId.Value ? 0 : 1)
                     .ThenBy(item => item.DueDate)
                     .ThenBy(item => item.InstallmentId)
                     .ThenBy(item => item.IsLateFee)
                     .ThenBy(item => item.Priority)
                     .ThenBy(item => item.HeadId))
        {
            if (remaining == 0)
            {
                break;
            }

            var applied = Math.Min(remaining, head.OutstandingAmount);
            if (applied > 0)
            {
                allocations.Add(new Allocation(head.InstallmentId, head.HeadId, applied, head.IsLateFee));
                remaining -= applied;
            }
        }

        return allocations;
    }
}
