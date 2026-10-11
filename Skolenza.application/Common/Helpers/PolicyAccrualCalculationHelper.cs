namespace skolenza.application.Common.Helpers;

/// <summary>
/// Calculates the persisted accrual-cycle amount from the authoritative annual entitlement.
/// The consuming leave ledger must credit the remaining balance in the final cycle so a rounded
/// recurring amount can never exceed or undershoot the annual entitlement.
/// </summary>
public static class PolicyAccrualCalculationHelper
{
    public const int CalculationScale = 6;

    public static decimal CalculateAmountPerCycle(decimal annualEntitlement, string frequencyCode)
    {
        if (annualEntitlement <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(annualEntitlement),
                "Annual entitlement must be greater than zero.");
        }

        var cycleCount = frequencyCode?.Trim().ToUpperInvariant() switch
        {
            "MONTHLY" => 12,
            "QUARTERLY" => 4,
            "YEARLY" => 1,
            _ => throw new ArgumentException(
                "Accrual frequency must be MONTHLY, QUARTERLY, or YEARLY.",
                nameof(frequencyCode))
        };

        return Math.Round(
            annualEntitlement / cycleCount,
            CalculationScale,
            MidpointRounding.AwayFromZero);
    }
}
