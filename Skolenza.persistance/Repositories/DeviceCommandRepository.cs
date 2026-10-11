// ================================================================
// Purpose : Persists the transport-neutral command queue and protocol audit. The
//           database, not client-supplied institution data, resolves every device.
// ================================================================

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using skolenza.application.Constants;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IDeviceCommunication;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace skolenza.persistance.Repositories;

/// <summary>Provides durable command submission, transport dispatch coordination, and idempotent response completion.</summary>
public sealed class DeviceCommandRepository(
    SkolenzaDbContext context,
    ILogger<DeviceCommandRepository> logger,
    IEncryptionService encryptionService,
    IInstitutionKeyResolver institutionKeyResolver)
    : IDeviceCommandSubmissionService, IDeviceCommandDispatchStore, IDeviceHttpsPollingService
{
    private static readonly short Queued = (short)DeviceCommandStatus.Queued;
    private static readonly short Publishing = (short)DeviceCommandStatus.Publishing;
    private static readonly short AwaitingResponse = (short)DeviceCommandStatus.AwaitingResponse;
    private static readonly short Completed = (short)DeviceCommandStatus.Completed;
    private static readonly short Failed = (short)DeviceCommandStatus.Failed;
    private static readonly short RetryScheduled = (short)DeviceCommandStatus.RetryScheduled;

    #region Command Submission and Dispatch Lifecycle

    /// <inheritdoc />
    public async Task<DeviceCommandSubmissionResult> SubmitAsync(
        DeviceCommandSubmission submission,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(submission);

        var definition = DeviceProtocolCommandCatalog.GetRequired(submission.CommandName);
        var payload = DeviceProtocolCommandCatalog.ValidatePayload(definition.Name, submission.Payload);
        var institutionDevice = await context.InstitutionDevices
            .Include(device => device.DeviceMaster)
            .Include(device => device.InstitutionDeviceConfiguration)
            .FirstOrDefaultAsync(
                device => device.Id == submission.InstitutionDeviceId && device.InstitutionId == submission.InstitutionId,
                cancellationToken)
            ?? throw new NotFoundException("The requested Institution device was not found.");

        ValidateTarget(institutionDevice, definition);
        EnsurePayloadSerialDoesNotConflict(payload, institutionDevice.DeviceMaster.SNo);

        var now = DateTime.UtcNow;
        var storedPayload = payload;
        if (submission.ProtectPayload)
        {
            var institutionKey = await institutionKeyResolver.ResolveAsync(institutionDevice.InstitutionId);
            storedPayload = JsonSerializer.Serialize(new
            {
                encryptedPayload = encryptionService.Encrypt(payload, institutionKey)
            });
        }

        var command = new DeviceCommand
        {
            InternalTrackingId = Guid.NewGuid(),
            InstitutionId = institutionDevice.InstitutionId,
            InstitutionDeviceId = institutionDevice.Id,
            CampusId = institutionDevice.CampusId,
            DeviceSerialNumber = institutionDevice.DeviceMaster.SNo.Trim(),
            CommandName = definition.Name,
            RequestPayload = storedPayload,
            IsSensitivePayload = submission.ProtectPayload,
            MatchCriteria = DeviceProtocolCommandCatalog.BuildMatchCriteria(definition, payload),
            Status = Queued,
            ResponseMode = (short)definition.ResponseMode,
            AccessLevel = (short)definition.AccessLevel,
            MaxAttempts = Math.Clamp(submission.MaxAttempts, 1, 10),
            RequestedById = submission.RequestedById,
            AddedDateTime = now
        };

        await context.DeviceCommands.AddAsync(command, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Queued DeviceCommand {DeviceCommandId} ({CommandName}) for InstitutionDevice {InstitutionDeviceId}.",
            command.Id,
            command.CommandName,
            command.InstitutionDeviceId);

        return new DeviceCommandSubmissionResult(
            command.Id,
            command.InternalTrackingId,
            command.DeviceSerialNumber,
            DeviceCommandStatus.Queued);
    }

    /// <inheritdoc />
    public async Task<DeviceCommandDispatch?> TryAcquireNextAsync(
        IReadOnlyCollection<DeviceCommunicationProtocol> transports,
        long? institutionDeviceId = null,
        CancellationToken cancellationToken = default)
    {
        if (transports.Count == 0)
        {
            return null;
        }

        var transportCodes = transports.Select(transport => (short)transport).Distinct().ToArray();
        var now = DateTime.UtcNow;
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var activeStatuses = new[] { Publishing, AwaitingResponse, RetryScheduled };
            var candidate = await context.DeviceCommands
                .Where(command =>
                    (!institutionDeviceId.HasValue || command.InstitutionDeviceId == institutionDeviceId.Value) &&
                    (command.Status == Queued ||
                     (command.Status == RetryScheduled && command.NextAttemptDateTime <= now)) &&
                    context.InstitutionDeviceConfigurations.Any(configuration =>
                        configuration.InstitutionDeviceId == command.InstitutionDeviceId &&
                        transportCodes.Contains(configuration.CommandTransport ?? configuration.MqttTransport ?? 0)) &&
                    !context.DeviceCommands.Any(other =>
                        other.DeviceSerialNumber == command.DeviceSerialNumber &&
                        other.Id != command.Id &&
                        activeStatuses.Contains(other.Status)) &&
                    !context.DeviceCommands.Any(other =>
                        other.DeviceSerialNumber == command.DeviceSerialNumber &&
                        other.Id != command.Id &&
                        other.Status == Queued &&
                        (other.AddedDateTime < command.AddedDateTime ||
                         (other.AddedDateTime == command.AddedDateTime && other.Id < command.Id))))
                .OrderBy(command => command.AddedDateTime)
                .ThenBy(command => command.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (candidate is null)
            {
                await transaction.CommitAsync(cancellationToken);
                return null;
            }

            string dispatchPayload;
            try
            {
                dispatchPayload = candidate.IsSensitivePayload
                    ? await DecryptSensitivePayloadAsync(candidate, cancellationToken)
                    : candidate.RequestPayload;
            }
            catch (Exception exception)
            {
                candidate.Status = Failed;
                candidate.FailureReason = "The protected device command could not be decrypted for delivery.";
                candidate.CompletedDateTime = now;
                candidate.UpdatedDateTime = now;
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                logger.LogError(exception, "Unable to decrypt protected DeviceCommand {DeviceCommandId}.", candidate.Id);
                return null;
            }

            candidate.Status = Publishing;
            candidate.AttemptCount++;
            candidate.NextAttemptDateTime = null;
            candidate.UpdatedDateTime = now;
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new DeviceCommandDispatch(
                candidate.Id,
                candidate.InstitutionId,
                candidate.InstitutionDeviceId,
                candidate.DeviceSerialNumber,
                candidate.CommandName,
                dispatchPayload,
                (DeviceCommandResponseMode)candidate.ResponseMode,
                candidate.AttemptCount);
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogDebug(exception, "Another dispatcher acquired a command first.");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task RecoverExpiredResponseDeadlinesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiredCommands = await context.DeviceCommands
            .Where(command =>
                command.Status == AwaitingResponse &&
                command.ResponseDeadlineDateTime != null &&
                command.ResponseDeadlineDateTime <= now)
            .ToListAsync(cancellationToken);

        foreach (var command in expiredCommands)
        {
            command.FailureReason = "The device did not return its documented response before the response deadline.";
            command.UpdatedDateTime = now;
            command.ResponseDeadlineDateTime = null;
            if (command.AttemptCount >= command.MaxAttempts)
            {
                command.Status = Failed;
                command.CompletedDateTime = now;
            }
            else
            {
                command.Status = RetryScheduled;
                command.NextAttemptDateTime = now.AddSeconds(Math.Min(60, command.AttemptCount * 10));
            }
        }

        if (expiredCommands.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogWarning(
                "Scheduled retry or final failure for {ExpiredCommandCount} expired device command response deadline(s).",
                expiredCommands.Count);
        }
    }

    /// <inheritdoc />
    public async Task MarkPublishedAsync(
        DeviceCommandDispatch dispatch,
        string topic,
        int qualityOfService,
        DateTime publishedDateTime,
        CancellationToken cancellationToken = default)
    {
        var command = await context.DeviceCommands.FirstOrDefaultAsync(item => item.Id == dispatch.DeviceCommandId, cancellationToken);
        if (command is null || command.Status != Publishing)
        {
            return;
        }

        await context.DeviceMessageLogs.AddAsync(new DeviceMessageLog
        {
            InstitutionId = command.InstitutionId,
            InstitutionDeviceId = command.InstitutionDeviceId,
            DeviceSerialNumber = command.DeviceSerialNumber,
            Topic = topic,
            Direction = (short)DeviceMessageDirection.Outbound,
            QualityOfService = qualityOfService,
            IsDuplicateDelivery = false,
            PayloadHash = Hash(command.RequestPayload),
            RawPayload = RedactSensitiveJson(command.RequestPayload),
            OccurredDateTime = publishedDateTime,
            AddedDateTime = publishedDateTime
        }, cancellationToken);

        command.PublishedDateTime = publishedDateTime;
        command.UpdatedDateTime = publishedDateTime;
        if ((DeviceCommandResponseMode)command.ResponseMode == DeviceCommandResponseMode.PublishOnly)
        {
            command.Status = Completed;
            command.CompletedDateTime = publishedDateTime;
        }
        else
        {
            command.Status = AwaitingResponse;
            command.ResponseDeadlineDateTime = publishedDateTime.AddSeconds(60);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ScheduleRetryOrFailAsync(
        DeviceCommandDispatch dispatch,
        string failureReason,
        DateTime failedDateTime,
        CancellationToken cancellationToken = default)
    {
        var command = await context.DeviceCommands.FirstOrDefaultAsync(item => item.Id == dispatch.DeviceCommandId, cancellationToken);
        if (command is null || command.Status != Publishing)
        {
            return;
        }

        // Preserve every outbound attempt, including an MQTT client/broker failure.
        // The raw audit is therefore complete even when the command is scheduled to retry.
        await context.DeviceMessageLogs.AddAsync(new DeviceMessageLog
        {
            InstitutionId = command.InstitutionId,
            InstitutionDeviceId = command.InstitutionDeviceId,
            DeviceSerialNumber = command.DeviceSerialNumber,
            Topic = $"aiface/{command.DeviceSerialNumber}/pub",
            Direction = (short)DeviceMessageDirection.Outbound,
            QualityOfService = 1,
            IsDuplicateDelivery = false,
            PayloadHash = Hash(command.RequestPayload),
            RawPayload = RedactSensitiveJson(command.RequestPayload),
            OccurredDateTime = failedDateTime,
            AddedDateTime = failedDateTime
        }, cancellationToken);

        command.FailureReason = Truncate(failureReason, 2000);
        command.UpdatedDateTime = failedDateTime;
        if (command.AttemptCount >= command.MaxAttempts)
        {
            command.Status = Failed;
            command.CompletedDateTime = failedDateTime;
        }
        else
        {
            command.Status = RetryScheduled;
            command.NextAttemptDateTime = failedDateTime.AddSeconds(Math.Min(60, command.AttemptCount * 10));
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region HTTPS Device Polling

    /// <inheritdoc />
    public async Task<DeviceHttpsPollingResponse> ProcessAsync(
        DeviceHttpsPollingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!DeviceHttpsGatewaySecurity.IsValidIngressToken(request.IngressToken) ||
            string.IsNullOrWhiteSpace(request.Payload))
        {
            return DeviceHttpsPollingResponse.Rejected;
        }

        var parsedPayload = TryParseHttpsPollingPayload(request.Payload);
        if (parsedPayload is null)
        {
            return DeviceHttpsPollingResponse.Rejected;
        }

        using var document = parsedPayload.Value.Document;
        var root = parsedPayload.Value.Root;
        var serialNumber = parsedPayload.Value.SerialNumber;
        var tokenHash = DeviceHttpsGatewaySecurity.HashIngressToken(request.IngressToken);
        var devices = await context.InstitutionDevices
            .Include(device => device.DeviceMaster)
            .Include(device => device.InstitutionDeviceConfiguration)
            .Where(device =>
                device.IsActive && !device.IsSoftDeleted &&
                device.DeviceMaster.IsActive && !device.DeviceMaster.IsSoftDeleted &&
                device.DeviceMaster.SupportsHttps &&
                device.InstitutionDeviceConfiguration != null &&
                (device.InstitutionDeviceConfiguration.CommandTransport ?? device.InstitutionDeviceConfiguration.MqttTransport) ==
                    (short)DeviceCommunicationProtocol.Https &&
                (device.InstitutionDeviceConfiguration.HttpsIngressTokenHash == tokenHash ||
                 (device.InstitutionDeviceConfiguration.PendingHttpsIngressTokenHash == tokenHash &&
                  device.InstitutionDeviceConfiguration.PendingHttpsIngressTokenExpiresDateTime > request.ReceivedDateTime)))
            .Take(2)
            .ToListAsync(cancellationToken);

        var device = devices.Count == 1 ? devices[0] : null;
        if (device is null ||
            !string.Equals(device.DeviceMaster.SNo.Trim(), serialNumber, StringComparison.Ordinal))
        {
            return DeviceHttpsPollingResponse.Rejected;
        }

        var promotePendingGateway =
            string.Equals(device.InstitutionDeviceConfiguration!.PendingHttpsIngressTokenHash, tokenHash, StringComparison.Ordinal) &&
            device.InstitutionDeviceConfiguration.PendingHttpsIngressTokenExpiresDateTime > request.ReceivedDateTime;

        return await ProcessValidatedHttpsDeviceAsync(
            device,
            root,
            serialNumber,
            request,
            revokeInitialProvisioning: true,
            promotePendingGateway: promotePendingGateway,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DeviceHttpsPollingResponse> ProcessInitialAsync(
        long deviceMasterId,
        string expectedSerialNumber,
        DeviceHttpsPollingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (deviceMasterId <= 0 || string.IsNullOrWhiteSpace(expectedSerialNumber))
        {
            return DeviceHttpsPollingResponse.Rejected;
        }

        var parsedPayload = TryParseHttpsPollingPayload(request.Payload);
        if (parsedPayload is null)
        {
            return DeviceHttpsPollingResponse.Rejected;
        }

        using var document = parsedPayload.Value.Document;
        var root = parsedPayload.Value.Root;
        var serialNumber = parsedPayload.Value.SerialNumber;
        if (!string.Equals(serialNumber, expectedSerialNumber.Trim(), StringComparison.Ordinal))
        {
            return DeviceHttpsPollingResponse.Rejected;
        }

        var candidates = await context.InstitutionDevices
            .Include(device => device.DeviceMaster)
            .Include(device => device.InstitutionDeviceConfiguration)
            .Where(device =>
                device.DeviceMasterId == deviceMasterId &&
                device.IsActive && !device.IsSoftDeleted &&
                device.DeviceMaster.IsActive && !device.DeviceMaster.IsSoftDeleted &&
                device.DeviceMaster.SupportsHttps &&
                device.InstitutionDeviceConfiguration != null &&
                (device.InstitutionDeviceConfiguration.CommandTransport ?? device.InstitutionDeviceConfiguration.MqttTransport) ==
                    (short)DeviceCommunicationProtocol.Https)
            .Take(2)
            .ToListAsync(cancellationToken);

        var device = candidates.Count == 1 ? candidates[0] : null;
        if (device is null)
        {
            // The device is correctly authenticated but has not yet been
            // assigned to a Institution. Never reveal whether such a record exists.
            return new DeviceHttpsPollingResponse(true, BuildHttpsAcknowledgement(root, serialNumber, 20));
        }

        return await ProcessValidatedHttpsDeviceAsync(
            device,
            root,
            serialNumber,
            request,
            revokeInitialProvisioning: false,
            promotePendingGateway: false,
            cancellationToken: cancellationToken);
    }

    #endregion

    #region Inbound Device Message Audit and Completion

    /// <inheritdoc />
    public async Task RecordInboundAsync(DeviceMqttInboundMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        var devicesQuery = context.InstitutionDevices
            .Include(device => device.DeviceMaster)
            .Include(device => device.InstitutionDeviceConfiguration)
            .Where(device =>
                device.DeviceMaster.SNo == message.DeviceSerialNumber &&
                device.IsActive && !device.IsSoftDeleted &&
                device.DeviceMaster.IsActive && !device.DeviceMaster.IsSoftDeleted &&
                device.InstitutionDeviceConfiguration != null &&
                (device.InstitutionDeviceConfiguration.CommandTransport ?? device.InstitutionDeviceConfiguration.MqttTransport) ==
                    (short)message.Transport);

        devicesQuery = message.Transport switch
        {
            DeviceCommunicationProtocol.Mqtt => devicesQuery.Where(device => device.DeviceMaster.SupportsMqtt),
            DeviceCommunicationProtocol.Mqtts => devicesQuery.Where(device => device.DeviceMaster.SupportsMqtts),
            DeviceCommunicationProtocol.Https => devicesQuery.Where(device => device.DeviceMaster.SupportsHttps),
            _ => devicesQuery.Where(_ => false)
        };

        var devices = await devicesQuery
            .Take(2)
            .ToListAsync(cancellationToken);
        var device = devices.Count == 1 ? devices[0] : null;

        // Raw audit is committed before response matching. A duplicate delivery therefore
        // remains visible even when the command completion below is intentionally idempotent.
        await context.DeviceMessageLogs.AddAsync(new DeviceMessageLog
        {
            InstitutionId = device?.InstitutionId,
            InstitutionDeviceId = device?.Id,
            DeviceSerialNumber = message.DeviceSerialNumber,
            Topic = message.Topic,
            Direction = (short)DeviceMessageDirection.Inbound,
            QualityOfService = message.QualityOfService,
            IsDuplicateDelivery = message.IsDuplicateDelivery,
            PayloadHash = Hash(message.Payload),
            RawPayload = RedactSensitiveJson(message.Payload),
            OccurredDateTime = message.ReceivedDateTime,
            AddedDateTime = DateTime.UtcNow
        }, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        if (!message.IsProtocolIdentityValid)
        {
            logger.LogWarning(
                "Ignored {Transport} payload processing for serial {DeviceSerialNumber}: topic and payload identity did not agree.",
                message.Transport,
                message.DeviceSerialNumber);
            return;
        }

        if (device is null)
        {
            logger.LogWarning(
                "Ignored {Transport} command processing for serial {DeviceSerialNumber}: active InstitutionDevice mapping was missing or ambiguous.",
                message.Transport,
                message.DeviceSerialNumber);
            return;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(message.Payload);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "{Transport} payload for serial {DeviceSerialNumber} is not JSON.", message.Transport, message.DeviceSerialNumber);
            return;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.TryGetProperty("cmd", out var inboundCommand) && inboundCommand.ValueKind == JsonValueKind.String &&
                string.Equals(inboundCommand.GetString(), DeviceCommands.CheckLive, StringComparison.OrdinalIgnoreCase))
            {
                var configuration = await context.InstitutionDeviceConfigurations
                    .FirstOrDefaultAsync(configuration => configuration.InstitutionDeviceId == device.Id, cancellationToken);
                if (configuration is not null)
                {
                    configuration.LastHeartbeatDateTime = message.ReceivedDateTime;
                    configuration.LastSuccessfulConnectionDateTime = message.ReceivedDateTime;
                    configuration.LastConnectionError = null;
                    configuration.UpdatedDateTime = message.ReceivedDateTime;
                    await context.SaveChangesAsync(cancellationToken);
                }
            }

            if (!root.TryGetProperty("ret", out var ret) || ret.ValueKind != JsonValueKind.String)
            {
                return;
            }

            var commandName = ret.GetString()?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(commandName))
            {
                return;
            }

            var awaitingCommands = await context.DeviceCommands
                .Where(command =>
                    command.InstitutionDeviceId == device.Id &&
                    (command.Status == AwaitingResponse || command.Status == RetryScheduled) &&
                    command.CommandName == commandName)
                .OrderBy(command => command.PublishedDateTime)
                .ThenBy(command => command.Id)
                .ToListAsync(cancellationToken);
            var matchedCommand = awaitingCommands.FirstOrDefault(command => DeviceProtocolCommandCatalog.ResponseMatches(command, root));
            var result = root.TryGetProperty("result", out var resultProperty) && resultProperty.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? resultProperty.GetBoolean()
                : (bool?)null;

            var redactedResponsePayload = RedactSensitiveJson(message.Payload);
            if (message.IsDuplicateDelivery && await context.DeviceCommandResponses.AnyAsync(
                    response =>
                        response.InstitutionDeviceId == device.Id &&
                        response.ResponseCommandName == commandName &&
                        response.ResponsePayload == redactedResponsePayload,
                    cancellationToken))
            {
                // The redacted response audit is retained above, but QoS 1 redelivery
                // must not create another parsed response or change command state.
                return;
            }

            await context.DeviceCommandResponses.AddAsync(new DeviceCommandResponse
            {
                DeviceCommandId = matchedCommand?.Id,
                InstitutionId = device.InstitutionId,
                InstitutionDeviceId = device.Id,
                DeviceSerialNumber = message.DeviceSerialNumber,
                ResponseCommandName = commandName,
                Result = result,
                FailureReason = result == false ? ExtractFailureReason(root) : null,
                ResponsePayload = redactedResponsePayload,
                ReceivedDateTime = message.ReceivedDateTime
            }, cancellationToken);

            if (matchedCommand is not null)
            {
                matchedCommand.Status = result == false ? Failed : Completed;
                matchedCommand.CompletedDateTime = message.ReceivedDateTime;
                matchedCommand.FailureReason = result == false ? ExtractFailureReason(root) : null;
                matchedCommand.UpdatedDateTime = message.ReceivedDateTime;
            }

            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (matchedCommand is not null)
            {
                // The unique response FK is the durable idempotency boundary for QoS 1 redelivery.
                logger.LogInformation(exception, "Duplicate response for DeviceCommand {DeviceCommandId} was ignored.", matchedCommand.Id);
                context.ChangeTracker.Clear();
            }
        }
    }

    #endregion

    #region Shared HTTPS Polling

    /// <summary>
    /// Performs the shared post-authentication processing for both normal and
    /// bootstrap HTTPS routes. The device never controls the command returned.
    /// </summary>
    private async Task<DeviceHttpsPollingResponse> ProcessValidatedHttpsDeviceAsync(
        InstitutionDevice device,
        JsonElement requestRoot,
        string serialNumber,
        DeviceHttpsPollingRequest request,
        bool revokeInitialProvisioning,
        bool promotePendingGateway,
        CancellationToken cancellationToken)
    {
        await RecordInboundAsync(
            new DeviceMqttInboundMessage(
                DeviceCommunicationProtocol.Https,
                "https",
                serialNumber,
                request.Payload,
                QualityOfService: 0,
                IsDuplicateDelivery: false,
                IsProtocolIdentityValid: true,
                ReceivedDateTime: request.ReceivedDateTime),
            cancellationToken);

        var configuration = device.InstitutionDeviceConfiguration!;
        configuration.LastHeartbeatDateTime = request.ReceivedDateTime;
        configuration.LastSuccessfulConnectionDateTime = request.ReceivedDateTime;
        configuration.LastConnectionError = null;
        configuration.UpdatedDateTime = request.ReceivedDateTime;

        // The replacement URL becomes authoritative only after the physical
        // device authenticates on it. Until that first poll the previous URL
        // remains usable, so a failed configuration command cannot strand it.
        if (promotePendingGateway)
        {
            configuration.HttpsIngressTokenHash = configuration.PendingHttpsIngressTokenHash;
            configuration.PendingHttpsIngressTokenHash = null;
            configuration.PendingHttpsIngressTokenExpiresDateTime = null;
        }

        if (revokeInitialProvisioning)
        {
            var activeBootstrapRoutes = await context.DeviceInitialProvisionings
                .Where(item => item.DeviceMasterId == device.DeviceMasterId && item.RevokedDateTime == null)
                .ToListAsync(cancellationToken);
            foreach (var bootstrapRoute in activeBootstrapRoutes)
            {
                bootstrapRoute.RevokedDateTime = request.ReceivedDateTime;
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        var dispatch = await TryAcquireNextAsync(
            new[] { DeviceCommunicationProtocol.Https },
            device.Id,
            cancellationToken);
        if (dispatch is not null)
        {
            await MarkPublishedAsync(dispatch, "https", qualityOfService: 0, request.ReceivedDateTime, cancellationToken);
            return new DeviceHttpsPollingResponse(true, dispatch.Payload);
        }

        return new DeviceHttpsPollingResponse(
            true,
            BuildHttpsAcknowledgement(requestRoot, serialNumber, configuration.HeartbeatIntervalSeconds ?? 20));
    }

    /// <summary>
    /// Parses the common device envelope once for both normal and bootstrap HTTPS
    /// routes. The <c>JsonDocument</c> remains owned by the caller and must be disposed.
    /// </summary>
    private static (JsonDocument Document, JsonElement Root, string SerialNumber)? TryParseHttpsPollingPayload(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(payload);
        }
        catch (JsonException)
        {
            return null;
        }

        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("sn", out var serialProperty) ||
            serialProperty.ValueKind != JsonValueKind.String)
        {
            document.Dispose();
            return null;
        }

        var serialNumber = serialProperty.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(serialNumber) || serialNumber.Length > 100)
        {
            document.Dispose();
            return null;
        }

        return (document, root, serialNumber);
    }

    #endregion

    #region Payload Protection and Validation

    /// <summary>Decrypts a command only in process, immediately before outbound delivery.</summary>
    private async Task<string> DecryptSensitivePayloadAsync(DeviceCommand command, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(command.RequestPayload);
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty("encryptedPayload", out var ciphertextProperty) ||
            ciphertextProperty.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(ciphertextProperty.GetString()))
        {
            throw new InvalidOperationException("Protected command payload has an invalid storage format.");
        }

        var institutionKey = await institutionKeyResolver.ResolveAsync(command.InstitutionId);
        var payload = encryptionService.Decrypt(ciphertextProperty.GetString()!, institutionKey);
        _ = DeviceProtocolCommandCatalog.ValidatePayload(command.CommandName, payload);
        return payload;
    }

    /// <summary>
    /// Removes passwords, secrets, tokens and credentials before a raw device
    /// message reaches diagnostic audit storage. The message hash still supports
    /// tamper/integrity investigations without retaining the secret.
    /// </summary>
    private static string RedactSensitiveJson(string payload)
    {
        try
        {
            var node = JsonNode.Parse(payload);
            if (node is null)
            {
                return "[REDACTED_EMPTY_PAYLOAD]";
            }

            RedactNode(node);
            return node.ToJsonString();
        }
        catch (JsonException)
        {
            return "[REDACTED_NON_JSON_PAYLOAD]";
        }
    }

    private static void RedactNode(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj.ToList())
            {
                if (IsSensitivePropertyName(property.Key))
                {
                    obj[property.Key] = "[REDACTED]";
                }
                else if (property.Value is not null)
                {
                    RedactNode(property.Value);
                }
            }

            return;
        }

        if (node is JsonArray array)
        {
            foreach (var item in array.Where(item => item is not null))
            {
                RedactNode(item!);
            }
        }
    }

    private static bool IsSensitivePropertyName(string propertyName) =>
        propertyName.Contains("password", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Contains("pwd", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Contains("token", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Contains("credential", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Contains("apikey", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Contains("authorization", StringComparison.OrdinalIgnoreCase);

    private static void ValidateTarget(InstitutionDevice institutionDevice, DeviceProtocolCommandDefinition definition)
    {
        if (!institutionDevice.IsActive || institutionDevice.IsSoftDeleted ||
            !institutionDevice.DeviceMaster.IsActive || institutionDevice.DeviceMaster.IsSoftDeleted)
        {
            throw new ConflictException("The requested Institution device is not active.");
        }

        var transportCode = institutionDevice.InstitutionDeviceConfiguration?.CommandTransport
            ?? institutionDevice.InstitutionDeviceConfiguration?.MqttTransport;
        if (!transportCode.HasValue || !Enum.IsDefined((DeviceCommunicationProtocol)transportCode.Value))
        {
            throw new ValidationErrorException("The Institution device must have a valid command transport configuration before a command can be queued.");
        }

        var transport = (DeviceCommunicationProtocol)transportCode.Value;
        if (!SupportsTransport(institutionDevice.DeviceMaster, transport))
        {
            throw new ValidationErrorException("The selected device model does not support its configured command transport.");
        }

        if (transport is not (DeviceCommunicationProtocol.Mqtt or DeviceCommunicationProtocol.Mqtts or DeviceCommunicationProtocol.Https))
        {
            throw new ValidationErrorException("The selected command transport does not yet have an enabled Skolenza transport adapter.");
        }

        if (transport == DeviceCommunicationProtocol.Https &&
            string.IsNullOrWhiteSpace(institutionDevice.InstitutionDeviceConfiguration?.HttpsIngressTokenHash))
        {
            throw new ValidationErrorException("Generate the HTTPS device gateway URL before queueing a command for this device.");
        }

        if (definition.AccessLevel == DeviceCommandAccessLevel.InstitutionAccessControlPermission &&
            !institutionDevice.DeviceMaster.IsAccessControlDevice)
        {
            throw new ValidationErrorException("The requested door-control command requires an access-control device.");
        }
    }

    private static bool SupportsTransport(DeviceMaster deviceMaster, DeviceCommunicationProtocol transport) => transport switch
    {
        DeviceCommunicationProtocol.Mqtt => deviceMaster.SupportsMqtt,
        DeviceCommunicationProtocol.Mqtts => deviceMaster.SupportsMqtts,
        DeviceCommunicationProtocol.Http => deviceMaster.SupportsHttp,
        DeviceCommunicationProtocol.Https => deviceMaster.SupportsHttps,
        DeviceCommunicationProtocol.WebSocket or DeviceCommunicationProtocol.WebSocketSecure => deviceMaster.SupportsWebSocket,
        _ => false
    };

    private static void EnsurePayloadSerialDoesNotConflict(string payload, string deviceSerialNumber)
    {
        using var document = JsonDocument.Parse(payload);
        if (document.RootElement.TryGetProperty("sn", out var serialProperty) &&
            serialProperty.ValueKind == JsonValueKind.String &&
            !string.Equals(serialProperty.GetString()?.Trim(), deviceSerialNumber.Trim(), StringComparison.Ordinal))
        {
            throw new ValidationErrorException("The command payload serial number does not match the selected Institution device.");
        }
    }

    private static string BuildHttpsAcknowledgement(JsonElement request, string serialNumber, int heartbeatIntervalSeconds)
    {
        var responseCommand = request.TryGetProperty("cmd", out var commandProperty) &&
                              commandProperty.ValueKind == JsonValueKind.String
            ? commandProperty.GetString()?.Trim()
            : request.TryGetProperty("ret", out var responseProperty) && responseProperty.ValueKind == JsonValueKind.String
                ? responseProperty.GetString()?.Trim()
                : DeviceCommands.CheckLive;

        return JsonSerializer.Serialize(new
        {
            ret = string.IsNullOrWhiteSpace(responseCommand) ? DeviceCommands.CheckLive : responseCommand,
            sn = serialNumber,
            result = true,
            tryseconds = Math.Clamp(heartbeatIntervalSeconds, 10, 3600)
        });
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string? ExtractFailureReason(JsonElement root)
    {
        foreach (var name in new[] { "message", "msg", "reason", "error" })
        {
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return Truncate(value.GetString(), 2000);
            }
        }

        return "The device returned result=false.";
    }

    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) || value.Length <= maxLength ? value : value[..maxLength];

    #endregion
}
