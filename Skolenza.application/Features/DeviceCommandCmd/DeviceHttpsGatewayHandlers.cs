// ================================================================
// Purpose : Processes device-initiated HTTPS polling without treating the
//           anonymous device endpoint as an authenticated user API.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.Interfaces.IDeviceCommunication;
using MediatR;

namespace skolenza.application.Features.DeviceCommandCmd;

#region HTTPS Polling Commands and Handlers

/// <summary>Represents one raw inbound HTTPS device request after controller size checks.</summary>
public sealed record ProcessDeviceHttpsPolling(
    string IngressToken,
    string Payload,
    DateTime ReceivedDateTime) : IRequest<DeviceHttpsPollingResponse>;

/// <summary>Delegates a validated device poll to the durable command pipeline.</summary>
public sealed class ProcessDeviceHttpsPollingHandler(IDeviceHttpsPollingService pollingService)
    : IRequestHandler<ProcessDeviceHttpsPolling, DeviceHttpsPollingResponse>
{
    /// <inheritdoc />
    public Task<DeviceHttpsPollingResponse> Handle(
        ProcessDeviceHttpsPolling request,
        CancellationToken cancellationToken) =>
        pollingService.ProcessAsync(
            new DeviceHttpsPollingRequest(request.IngressToken, request.Payload, request.ReceivedDateTime),
            cancellationToken);
}

/// <summary>Represents a raw device poll through the short-lived Host bootstrap route.</summary>
public sealed record ProcessInitialDeviceHttpsPolling(
    string DeviceSerialNumber,
    string IngressToken,
    string Payload,
    DateTime ReceivedDateTime) : IRequest<DeviceHttpsPollingResponse>;

/// <summary>
/// Authenticates the device against its Host-issued bootstrap token before the
/// normal HTTPS command pipeline is allowed to handle the request.
/// </summary>
public sealed class ProcessInitialDeviceHttpsPollingHandler(
    IDeviceInitialProvisioningService initialProvisioningService,
    IDeviceHttpsPollingService pollingService)
    : IRequestHandler<ProcessInitialDeviceHttpsPolling, DeviceHttpsPollingResponse>
{
    /// <inheritdoc />
    public async Task<DeviceHttpsPollingResponse> Handle(
        ProcessInitialDeviceHttpsPolling request,
        CancellationToken cancellationToken)
    {
        var validation = await initialProvisioningService.ValidateAsync(
            request.DeviceSerialNumber,
            request.IngressToken,
            cancellationToken);
        if (validation is null)
        {
            return DeviceHttpsPollingResponse.Rejected;
        }

        await initialProvisioningService.RecordConnectionAsync(
            validation.ProvisioningId,
            request.ReceivedDateTime,
            cancellationToken);

        return await pollingService.ProcessInitialAsync(
            validation.DeviceMasterId,
            validation.DeviceSerialNumber,
            new DeviceHttpsPollingRequest(request.IngressToken, request.Payload, request.ReceivedDateTime),
            cancellationToken);
    }
}

#endregion

#region HTTPS Gateway Security

/// <summary>
/// Fails closed before persistence when a caller does not resemble a provisioned
/// device gateway request. The controller deliberately turns this into a 404 so
/// a caller cannot enumerate valid device URLs.
/// </summary>
public sealed class DeviceHttpsGatewaySecurityBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc />
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ProcessDeviceHttpsPolling poll)
        {
            if (request is not ProcessInitialDeviceHttpsPolling initialPoll)
            {
                return next(cancellationToken);
            }

            if (!DeviceHttpsGatewaySecurity.IsValidIngressToken(initialPoll.IngressToken) ||
                string.IsNullOrWhiteSpace(initialPoll.DeviceSerialNumber) ||
                initialPoll.DeviceSerialNumber.Length > 100 ||
                string.IsNullOrWhiteSpace(initialPoll.Payload) ||
                initialPoll.Payload.Length > 131_072)
            {
                return Task.FromResult((TResponse)(object)DeviceHttpsPollingResponse.Rejected);
            }

            return next(cancellationToken);
        }

        if (!DeviceHttpsGatewaySecurity.IsValidIngressToken(poll.IngressToken) ||
            string.IsNullOrWhiteSpace(poll.Payload) ||
            poll.Payload.Length > 131_072)
        {
            return Task.FromResult((TResponse)(object)DeviceHttpsPollingResponse.Rejected);
        }

        return next(cancellationToken);
    }
}

#endregion
