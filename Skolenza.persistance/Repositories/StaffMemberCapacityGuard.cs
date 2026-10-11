using skolenza.application.Exceptions;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Repositories;

/// <summary>Shared subscription capacity calculation for imports and ordinary StaffMember creation.</summary>
internal static class StaffMemberCapacityGuard
{
    public static async Task<int> AvailableSeatsAsync(SkolenzaDbContext context, long institutionId, CancellationToken token)
    {
        var now = DateTime.UtcNow;
        var limits = await context.InstitutionSubscriptions.AsNoTracking().Where(item => item.InstitutionId == institutionId && item.IsActive &&
                item.SubscriptionStartDate <= now && item.SubscriptionEndDate > now && item.SubscriptionPlan.IsActive && !item.SubscriptionPlan.IsSoftDeleted)
            .Select(item => item.SubscriptionPlan.MaxUsers).ToListAsync(token);
        if (limits.Count != 1 || limits[0] <= 0)
        {
            return -1;
        }
        // Suspended staffMembers retain their seat; only soft-deleted staffMembers are excluded.
        var used = await context.StaffMembers.CountAsync(item => item.InstitutionId == institutionId && !item.IsSoftDeleted, token);
        return limits[0] - used;
    }

    public static async Task EnsureSpaceAsync(SkolenzaDbContext context, long institutionId, CancellationToken token)
    {
        if (context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("StaffMember insertion requires its account-creation transaction.");
        }
        // Keep subscription/plan edits from changing capacity between the final check and insert.
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT \"Id\" FROM skolenza.\"InstitutionSubscription\" WHERE \"InstitutionId\" = {institutionId} FOR SHARE", token);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT p."Id" FROM skolenza."SubscriptionPlan" p
            JOIN skolenza."InstitutionSubscription" s ON s."SubscriptionPlanId" = p."Id"
            WHERE s."InstitutionId" = {institutionId} FOR SHARE OF p
            """, token);
        if (await AvailableSeatsAsync(context, institutionId, token) <= 0)
        {
            throw new ValidationErrorException("Subscription MaxUsers capacity is unavailable or exhausted, including the initial Institution Admin seat.");
        }
    }
}
