// ================================================================
// Purpose : Persists secure Host-issued initial device bootstrap identities.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IDeviceCommunication;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace skolenza.persistance.Repositories;

/// <summary>
/// Provides a short-lived, revocable, opaque initial URL. A serial number alone
/// is deliberately never accepted as device authentication.
/// </summary>
public sealed class DeviceInitialProvisioningRepository(
    SkolenzaDbContext context,
    IConfiguration configuration) : IDeviceInitialProvisioningService
{
    private const int BootstrapHeartbeatSeconds = 20;
    private const int MinimumLifetimeMinutes = 5;
    private const int MaximumLifetimeMinutes = 24 * 60;

    #region Initial Provisioning Lifecycle

    /// <inheritdoc />
    public async Task<DeviceInitialProvisioningIssue> IssueAsync(
        long deviceMasterId,
        int lifetimeMinutes,
        long issuedById,
        CancellationToken cancellationToken = default)
    {
        if (deviceMasterId <= 0 || issuedById <= 0 ||
            lifetimeMinutes is < MinimumLifetimeMinutes or > MaximumLifetimeMinutes)
        {
            throw new ValidationErrorException("A valid unassigned HTTPS device and a bootstrap lifetime of 5 to 1,440 minutes are required.");
        }

        var deviceMaster = await context.DeviceMasters
            .FirstOrDefaultAsync(device => device.Id == deviceMasterId && !device.IsSoftDeleted, cancellationToken)
            ?? throw new NotFoundException("The requested device was not found.");

        if (!deviceMaster.IsActive || deviceMaster.IsOccupied || !deviceMaster.SupportsHttps ||
            string.IsNullOrWhiteSpace(deviceMaster.SNo))
        {
            throw new ValidationErrorException("Initial provisioning requires an active, unassigned device that supports HTTPS polling.");
        }

        var now = DateTime.UtcNow;
        var activeProvisionings = await context.DeviceInitialProvisionings
            .Where(item => item.DeviceMasterId == deviceMasterId &&
                           item.RevokedDateTime == null &&
                           item.ExpiresDateTime > now)
            .ToListAsync(cancellationToken);

        foreach (var active in activeProvisionings)
        {
            active.RevokedDateTime = now;
            active.RevokedById = issuedById;
        }

        var rawToken = DeviceHttpsGatewaySecurity.GenerateIngressToken();
        var expiresAt = now.AddMinutes(lifetimeMinutes);
        await context.DeviceInitialProvisionings.AddAsync(new DeviceInitialProvisioning
        {
            DeviceMasterId = deviceMaster.Id,
            IngressTokenHash = DeviceHttpsGatewaySecurity.HashIngressToken(rawToken),
            HeartbeatIntervalSeconds = BootstrapHeartbeatSeconds,
            ExpiresDateTime = expiresAt,
            IssuedById = issuedById,
            IssuedDateTime = now
        }, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var baseUrl = ResolvePublicBaseUrl();
        var serial = deviceMaster.SNo.Trim();
        return new DeviceInitialProvisioningIssue(
            serial,
            $"{baseUrl}/api/initial/{Uri.EscapeDataString(serial)}/{rawToken}",
            BootstrapHeartbeatSeconds,
            expiresAt);
    }

    /// <inheritdoc />
    public async Task<DeviceInitialProvisioningValidation?> ValidateAsync(
        string deviceSerialNumber,
        string ingressToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceSerialNumber) ||
            !DeviceHttpsGatewaySecurity.IsValidIngressToken(ingressToken))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var tokenHash = DeviceHttpsGatewaySecurity.HashIngressToken(ingressToken);
        var candidates = await context.DeviceInitialProvisionings
            .Include(item => item.DeviceMaster)
            .Where(item => item.IngressTokenHash == tokenHash &&
                           item.RevokedDateTime == null &&
                           item.ExpiresDateTime > now &&
                           item.DeviceMaster.IsActive &&
                           !item.DeviceMaster.IsSoftDeleted)
            .Take(2)
            .ToListAsync(cancellationToken);

        var provisioning = candidates.Count == 1 ? candidates[0] : null;
        if (provisioning is null ||
            !string.Equals(provisioning.DeviceMaster.SNo.Trim(), deviceSerialNumber.Trim(), StringComparison.Ordinal))
        {
            return null;
        }

        return new DeviceInitialProvisioningValidation(
            provisioning.Id,
            provisioning.DeviceMasterId,
            provisioning.DeviceMaster.SNo.Trim(),
            provisioning.HeartbeatIntervalSeconds);
    }

    #endregion

    #region Connection Audit and Revocation

    /// <inheritdoc />
    public async Task RecordConnectionAsync(
        long provisioningId,
        DateTime connectedDateTime,
        CancellationToken cancellationToken = default)
    {
        var provisioning = await context.DeviceInitialProvisionings
            .FirstOrDefaultAsync(item => item.Id == provisioningId, cancellationToken);
        if (provisioning is null)
        {
            return;
        }

        provisioning.FirstConnectedDateTime ??= connectedDateTime;
        provisioning.LastConnectedDateTime = connectedDateTime;
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RevokeForDeviceMasterAsync(
        long deviceMasterId,
        DateTime revokedDateTime,
        CancellationToken cancellationToken = default)
    {
        var activeProvisionings = await context.DeviceInitialProvisionings
            .Where(item => item.DeviceMasterId == deviceMasterId && item.RevokedDateTime == null)
            .ToListAsync(cancellationToken);

        if (activeProvisionings.Count == 0)
        {
            return;
        }

        foreach (var provisioning in activeProvisionings)
        {
            provisioning.RevokedDateTime = revokedDateTime;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Deployment Configuration

    private string ResolvePublicBaseUrl()
    {
        var configuredUrl = configuration["DeviceGateway:PublicBaseUrl"];
        if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            // This is intentionally a safe production fallback rather than a local
            // address. A deployment may override it with DeviceGateway__PublicBaseUrl.
            return "https://skolenza-api.onrender.com";
        }

        return uri.GetLeftPart(UriPartial.Authority) + uri.AbsolutePath.TrimEnd('/');
    }

    #endregion
}
