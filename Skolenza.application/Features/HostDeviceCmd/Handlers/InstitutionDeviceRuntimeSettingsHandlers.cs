// ================================================================
// Purpose : Queues typed Institution device-settings commands. The physical device
//           receives them only from its configured outbound transport; this
//           feature never opens a connection to a device LAN address.
// ================================================================

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using AutoMapper;
using skolenza.application.Common.Enums;
using skolenza.application.Constants;
using skolenza.application.DTOS.Host;
using skolenza.application.Exceptions;
using skolenza.application.Features.InstitutionConfigurationCmd.Handlers;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IDeviceCommunication;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.HostDeviceCmd.Handlers;

/// <summary>Queues exactly one typed device settings section.</summary>
public sealed class ApplyInstitutionDeviceSettingsCommand(
    InstitutionDeviceSettingRequestDTO dto,
    InstitutionDeviceSettingsSection section)
    : IRequest<ApiResponse<DeviceCommandSubmissionResponseDTO>>
{
    public InstitutionDeviceSettingRequestDTO DTO { get; } = dto;
    public InstitutionDeviceSettingsSection Section { get; } = section;
}

/// <summary>Sets the device UTC clock through the durable command queue.</summary>
public sealed class SyncInstitutionDeviceTimeCommand(SyncInstitutionDeviceTimeRequestDTO dto)
    : IRequest<ApiResponse<DeviceCommandSubmissionResponseDTO>>
{
    public SyncInstitutionDeviceTimeRequestDTO DTO { get; } = dto;
}

/// <summary>Changes only the assigned Institution location; DeviceMaster assignment remains Host-owned.</summary>
public sealed class UpdateInstitutionDeviceLocationCommand(UpdateInstitutionDeviceLocationRequestDTO dto)
    : IRequest<ApiResponse<InstitutionDeviceResponseDTO>>
{
    public UpdateInstitutionDeviceLocationRequestDTO DTO { get; } = dto;
}

/// <summary>Gets non-secret gateway address metadata for the selected device.</summary>
public sealed class GetInstitutionDeviceGatewayAddressQuery(string encryptedInstitutionDeviceId, InstitutionDeviceAccessRequestDTO accessRequest)
    : IRequest<ApiResponse<InstitutionDeviceGatewayAddressResponseDTO>>
{
    public string EncryptedInstitutionDeviceId { get; } = encryptedInstitutionDeviceId;
    public InstitutionDeviceAccessRequestDTO AccessRequest { get; } = accessRequest;
}

/// <summary>Queues a two-phase replacement of the opaque HTTPS gateway URL.</summary>
public sealed class ReplaceInstitutionDeviceHttpsGatewayUrlCommand(ReplaceInstitutionDeviceHttpsGatewayUrlRequestDTO dto)
    : IRequest<ApiResponse<DeviceCommandSubmissionResponseDTO>>
{
    public ReplaceInstitutionDeviceHttpsGatewayUrlRequestDTO DTO { get; } = dto;
}

/// <summary>Publishes the next queued command for an MQTTS device on explicit Institution request.</summary>
public sealed class DispatchInstitutionDeviceMqttsNowCommand(DispatchInstitutionDeviceMqttsNowRequestDTO dto)
    : IRequest<ApiResponse<ManualInstitutionDeviceCommandDispatchResponseDTO>>
{
    public DispatchInstitutionDeviceMqttsNowRequestDTO DTO { get; } = dto;
}

/// <summary>Handles all typed <c>setdevinfo</c> section changes without a raw JSON escape hatch.</summary>
public sealed class ApplyInstitutionDeviceSettingsCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService,
    IDeviceCommandSubmissionService deviceCommandSubmissionService,
    ILogger<InstitutionConfigurationHandlerBase> institutionLogger,
    ILogger<ApplyInstitutionDeviceSettingsCommandHandler> logger)
    : InstitutionDeviceAccessHandlerBase(unitOfWork, commonRequestService, idEncoderService, institutionLogger),
        IRequestHandler<ApplyInstitutionDeviceSettingsCommand, ApiResponse<DeviceCommandSubmissionResponseDTO>>
{
    public async Task<ApiResponse<DeviceCommandSubmissionResponseDTO>> Handle(
        ApplyInstitutionDeviceSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.DTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        var scope = await ResolveAuthorizedInstitutionConfigurationScopeAsync(dto, cancellationToken);
        InstitutionDeviceRuntimeSettingsValidation.ValidateCommon(dto);
        var institutionDeviceId = DecodeIdentifier(dto.InstitutionDeviceId, scope, "InstitutionDeviceId");

        _ = await UnitOfWork.InstitutionDeviceConfigurationRepository.GetForUpdateByInstitutionDeviceAsync(
                scope.InstitutionId,
                institutionDeviceId,
                cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.InstitutionDeviceConfigurationNotFound);

        var payload = InstitutionDeviceRuntimeSettingsPayloadBuilder.BuildSetDeviceInfo(dto, request.Section);
        var result = await deviceCommandSubmissionService.SubmitAsync(
            new DeviceCommandSubmission(
                scope.InstitutionId,
                institutionDeviceId,
                DeviceCommands.SetDeviceInfo,
                payload,
                scope.ActorId,
                ProtectPayload: true),
            cancellationToken);

        logger.LogInformation(
            "Queued Institution device {Section} settings for InstitutionDevice {InstitutionDeviceId}. Command {DeviceCommandId}.",
            request.Section,
            institutionDeviceId,
            result.DeviceCommandId);

        return ApiResponse<DeviceCommandSubmissionResponseDTO>.Success(
            ToSubmissionResponse(result),
            $"{InstitutionDeviceRuntimeSettingsPayloadBuilder.GetSectionLabel(request.Section)} settings have been queued for the device.");
    }

    private static DeviceCommandSubmissionResponseDTO ToSubmissionResponse(DeviceCommandSubmissionResult result) =>
        new()
        {
            DeviceCommandId = result.DeviceCommandId,
            InternalTrackingId = result.InternalTrackingId,
            Status = result.Status.ToString()
        };
}

/// <summary>Handles an explicit device clock synchronization.</summary>
public sealed class SyncInstitutionDeviceTimeCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService,
    IDeviceCommandSubmissionService deviceCommandSubmissionService,
    ILogger<InstitutionConfigurationHandlerBase> institutionLogger,
    ILogger<SyncInstitutionDeviceTimeCommandHandler> logger)
    : InstitutionDeviceAccessHandlerBase(unitOfWork, commonRequestService, idEncoderService, institutionLogger),
        IRequestHandler<SyncInstitutionDeviceTimeCommand, ApiResponse<DeviceCommandSubmissionResponseDTO>>
{
    public async Task<ApiResponse<DeviceCommandSubmissionResponseDTO>> Handle(
        SyncInstitutionDeviceTimeCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.DTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        var scope = await ResolveAuthorizedInstitutionConfigurationScopeAsync(dto, cancellationToken);
        InstitutionDeviceRuntimeSettingsValidation.ValidateCommon(dto);
        var institutionDeviceId = DecodeIdentifier(dto.InstitutionDeviceId, scope, "InstitutionDeviceId");

        _ = await UnitOfWork.InstitutionDeviceConfigurationRepository.GetForUpdateByInstitutionDeviceAsync(
                scope.InstitutionId,
                institutionDeviceId,
                cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.InstitutionDeviceConfigurationNotFound);

        var payload = InstitutionDeviceRuntimeSettingsPayloadBuilder.BuildSetTime(dto);
        var result = await deviceCommandSubmissionService.SubmitAsync(
            new DeviceCommandSubmission(
                scope.InstitutionId,
                institutionDeviceId,
                DeviceCommands.SetTime,
                payload,
                scope.ActorId,
                ProtectPayload: true),
            cancellationToken);

        logger.LogInformation("Queued device time synchronization for InstitutionDevice {InstitutionDeviceId}. Command {DeviceCommandId}.", institutionDeviceId, result.DeviceCommandId);
        return ApiResponse<DeviceCommandSubmissionResponseDTO>.Success(
            new DeviceCommandSubmissionResponseDTO
            {
                DeviceCommandId = result.DeviceCommandId,
                InternalTrackingId = result.InternalTrackingId,
                Status = result.Status.ToString()
            },
            "Device time synchronization has been queued.");
    }
}

/// <summary>Handles Institution-owned location changes without exposing the DeviceMaster assignment operation.</summary>
public sealed class UpdateInstitutionDeviceLocationCommandHandler(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService,
    ILogger<InstitutionConfigurationHandlerBase> institutionLogger,
    ILogger<UpdateInstitutionDeviceLocationCommandHandler> logger)
    : InstitutionDeviceAccessHandlerBase(unitOfWork, commonRequestService, idEncoderService, institutionLogger),
        IRequestHandler<UpdateInstitutionDeviceLocationCommand, ApiResponse<InstitutionDeviceResponseDTO>>
{
    public async Task<ApiResponse<InstitutionDeviceResponseDTO>> Handle(
        UpdateInstitutionDeviceLocationCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.DTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        var scope = await ResolveAuthorizedInstitutionConfigurationScopeAsync(dto, cancellationToken);
        var institutionDeviceId = DecodeIdentifier(dto.InstitutionDeviceId, scope, "InstitutionDeviceId");
        if (dto.CampusId <= 0 ||
            !await UnitOfWork.InstitutionDeviceRepository.IsActiveCampusAsync(dto.CampusId, cancellationToken) ||
            !await UnitOfWork.InstitutionDeviceRepository.CampusBelongsToInstitutionAsync(scope.InstitutionId, dto.CampusId, cancellationToken))
        {
            throw new ValidationErrorException("Select an active location that belongs to this Institution.");
        }

        var device = await UnitOfWork.InstitutionDeviceRepository.GetForUpdateAsync(scope.InstitutionId, institutionDeviceId, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.InstitutionDeviceNotFound);
        device.CampusId = dto.CampusId;
        device.UpdatedById = scope.ActorId;
        device.UpdatedDateTime = DateTime.UtcNow;
        await UnitOfWork.SaveChangesAsync(cancellationToken);

        var stored = await UnitOfWork.InstitutionDeviceRepository.GetByIdAsync(scope.InstitutionId, institutionDeviceId, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.InstitutionDeviceNotFound);
        logger.LogInformation("Moved InstitutionDevice {InstitutionDeviceId} to Campus {CampusId}.", institutionDeviceId, dto.CampusId);
        return ApiResponse<InstitutionDeviceResponseDTO>.Success(
            MapDeviceResponse(mapper, stored, scope),
            "Device location has been updated.");
    }
}

/// <summary>Returns gateway metadata without ever returning the bearer URL token.</summary>
public sealed class GetInstitutionDeviceGatewayAddressQueryHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService,
    ILogger<InstitutionConfigurationHandlerBase> institutionLogger)
    : InstitutionDeviceAccessHandlerBase(unitOfWork, commonRequestService, idEncoderService, institutionLogger),
        IRequestHandler<GetInstitutionDeviceGatewayAddressQuery, ApiResponse<InstitutionDeviceGatewayAddressResponseDTO>>
{
    public async Task<ApiResponse<InstitutionDeviceGatewayAddressResponseDTO>> Handle(
        GetInstitutionDeviceGatewayAddressQuery request,
        CancellationToken cancellationToken)
    {
        var scope = await ResolveAuthorizedInstitutionConfigurationScopeAsync(request.AccessRequest, cancellationToken);
        var institutionDeviceId = DecodeIdentifier(request.EncryptedInstitutionDeviceId, scope, "InstitutionDeviceId");
        var configuration = await UnitOfWork.InstitutionDeviceConfigurationRepository.GetForUpdateByInstitutionDeviceAsync(
                scope.InstitutionId,
                institutionDeviceId,
                cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.InstitutionDeviceConfigurationNotFound);

        return ApiResponse<InstitutionDeviceGatewayAddressResponseDTO>.Success(
            new InstitutionDeviceGatewayAddressResponseDTO
            {
                InstitutionDeviceId = EncryptIdentifier(institutionDeviceId, scope.InstitutionEncryptionKey),
                ServerUrl = configuration.ServerUrl ?? string.Empty,
                ServerPath = configuration.ServerPath ?? string.Empty,
                ServerPort = configuration.ServerPort ?? 443,
                HasActiveGatewayUrl = !string.IsNullOrWhiteSpace(configuration.HttpsIngressTokenHash),
                IsReplacementPending = !string.IsNullOrWhiteSpace(configuration.PendingHttpsIngressTokenHash) &&
                                       configuration.PendingHttpsIngressTokenExpiresDateTime > DateTime.UtcNow,
                ReplacementExpiresDateTime = configuration.PendingHttpsIngressTokenExpiresDateTime
            },
            "Device gateway address details retrieved successfully.");
    }
}

/// <summary>Queues a seamless HTTPS URL replacement through the still-valid current device gateway.</summary>
public sealed class ReplaceInstitutionDeviceHttpsGatewayUrlCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService,
    IDeviceCommandSubmissionService deviceCommandSubmissionService,
    ILogger<InstitutionConfigurationHandlerBase> institutionLogger,
    ILogger<ReplaceInstitutionDeviceHttpsGatewayUrlCommandHandler> logger)
    : InstitutionDeviceAccessHandlerBase(unitOfWork, commonRequestService, idEncoderService, institutionLogger),
        IRequestHandler<ReplaceInstitutionDeviceHttpsGatewayUrlCommand, ApiResponse<DeviceCommandSubmissionResponseDTO>>
{
    public async Task<ApiResponse<DeviceCommandSubmissionResponseDTO>> Handle(
        ReplaceInstitutionDeviceHttpsGatewayUrlCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.DTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        var scope = await ResolveAuthorizedInstitutionConfigurationScopeAsync(dto, cancellationToken);
        InstitutionDeviceRuntimeSettingsValidation.ValidateCommon(dto);
        if (dto.ReplacementLifetimeMinutes is < 5 or > 1_440)
        {
            throw new ValidationErrorException("Gateway replacement lifetime must be between 5 and 1,440 minutes.");
        }

        var institutionDeviceId = DecodeIdentifier(dto.InstitutionDeviceId, scope, "InstitutionDeviceId");
        var configuration = await UnitOfWork.InstitutionDeviceConfigurationRepository.GetForUpdateByInstitutionDeviceAsync(
                scope.InstitutionId,
                institutionDeviceId,
                cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.InstitutionDeviceConfigurationNotFound);
        InstitutionDeviceConfigurationValidation.EnsureHttpsGateway(configuration);
        if (string.IsNullOrWhiteSpace(configuration.HttpsIngressTokenHash))
        {
            throw new ValidationErrorException("Generate the current HTTPS gateway URL before replacing it remotely.");
        }

        if (!string.IsNullOrWhiteSpace(configuration.PendingHttpsIngressTokenHash) &&
            configuration.PendingHttpsIngressTokenExpiresDateTime > DateTime.UtcNow)
        {
            throw new ConflictException("A device gateway URL replacement is already awaiting device confirmation.");
        }

        var rawPendingToken = DeviceHttpsGatewaySecurity.GenerateIngressToken();
        var pendingUrl = $"{configuration.ServerUrl!.TrimEnd('/')}{DeviceHttpsGatewaySecurity.RoutePrefix}/{rawPendingToken}";
        var expiresDateTime = DateTime.UtcNow.AddMinutes(dto.ReplacementLifetimeMinutes);

        await UnitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            configuration.PendingHttpsIngressTokenHash = DeviceHttpsGatewaySecurity.HashIngressToken(rawPendingToken);
            configuration.PendingHttpsIngressTokenExpiresDateTime = expiresDateTime;
            configuration.UpdatedById = scope.ActorId;
            configuration.UpdatedDateTime = DateTime.UtcNow;
            await UnitOfWork.SaveChangesAsync(cancellationToken);

            var payload = InstitutionDeviceRuntimeSettingsPayloadBuilder.BuildGatewayReplacement(dto.CurrentWebServerPassword, pendingUrl);
            var result = await deviceCommandSubmissionService.SubmitAsync(
                new DeviceCommandSubmission(
                    scope.InstitutionId,
                    institutionDeviceId,
                    DeviceCommands.SetDeviceInfo,
                    payload,
                    scope.ActorId,
                    ProtectPayload: true),
                cancellationToken);

            await UnitOfWork.CommitTransactionAsync(cancellationToken);
            logger.LogInformation("Queued seamless HTTPS gateway replacement for InstitutionDevice {InstitutionDeviceId}. Command {DeviceCommandId}.", institutionDeviceId, result.DeviceCommandId);
            return ApiResponse<DeviceCommandSubmissionResponseDTO>.Success(
                new DeviceCommandSubmissionResponseDTO
                {
                    DeviceCommandId = result.DeviceCommandId,
                    InternalTrackingId = result.InternalTrackingId,
                    Status = result.Status.ToString()
                },
                "Device gateway URL replacement has been queued. The existing URL remains active until the device confirms the replacement.");
        }
        catch
        {
            await UnitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}

/// <summary>Runs one existing MQTTS queue item now without accepting device JSON from Angular.</summary>
public sealed class DispatchInstitutionDeviceMqttsNowCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService,
    IDeviceCommandManualDispatcher manualDispatcher,
    ILogger<InstitutionConfigurationHandlerBase> institutionLogger,
    ILogger<DispatchInstitutionDeviceMqttsNowCommandHandler> logger)
    : InstitutionDeviceAccessHandlerBase(unitOfWork, commonRequestService, idEncoderService, institutionLogger),
        IRequestHandler<DispatchInstitutionDeviceMqttsNowCommand, ApiResponse<ManualInstitutionDeviceCommandDispatchResponseDTO>>
{
    public async Task<ApiResponse<ManualInstitutionDeviceCommandDispatchResponseDTO>> Handle(
        DispatchInstitutionDeviceMqttsNowCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.DTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        var scope = await ResolveAuthorizedInstitutionConfigurationScopeAsync(dto, cancellationToken);
        var institutionDeviceId = DecodeIdentifier(dto.InstitutionDeviceId, scope, "InstitutionDeviceId");
        var configuration = await UnitOfWork.InstitutionDeviceConfigurationRepository.GetForUpdateByInstitutionDeviceAsync(
                scope.InstitutionId,
                institutionDeviceId,
                cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.InstitutionDeviceConfigurationNotFound);

        var transport = configuration.CommandTransport ?? configuration.MqttTransport;
        if (transport != (short)DeviceCommunicationProtocol.Mqtts)
        {
            throw new ValidationErrorException(
                "Manual dispatch is available only for MQTTS devices. HTTPS devices receive queued commands on their next heartbeat poll.");
        }

        var result = await manualDispatcher.DispatchNextMqttsAsync(institutionDeviceId, cancellationToken);
        logger.LogInformation(
            "Institution requested manual MQTTS dispatch for InstitutionDevice {InstitutionDeviceId}. Dispatched: {WasDispatched}.",
            institutionDeviceId,
            result.WasDispatched);
        return ApiResponse<ManualInstitutionDeviceCommandDispatchResponseDTO>.Success(
            new ManualInstitutionDeviceCommandDispatchResponseDTO
            {
                WasDispatched = result.WasDispatched,
                Message = result.Message
            },
            result.Message);
    }
}

/// <summary>Validates typed device settings before vendor JSON is generated.</summary>
internal static class InstitutionDeviceRuntimeSettingsValidation
{
    internal static void ValidateCommon(InstitutionDeviceSettingRequestDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.InstitutionDeviceId) ||
            string.IsNullOrWhiteSpace(dto.CurrentWebServerPassword) ||
            dto.CurrentWebServerPassword.Length is < 4 or > 128)
        {
            throw new ValidationErrorException("A valid Institution device and current device Web UI/API password are required.");
        }
    }

    internal static void ValidateRange(int value, int minimum, int maximum, string label)
    {
        if (value < minimum || value > maximum)
        {
            throw new ValidationErrorException($"{label} must be between {minimum} and {maximum}.");
        }
    }

    internal static void ValidateOneOf(int value, string label, params int[] allowedValues)
    {
        if (!allowedValues.Contains(value))
        {
            throw new ValidationErrorException($"Select a supported {label} option.");
        }
    }

    internal static int ParseMonthDay(string? value, string label)
    {
        if (!DateTime.TryParseExact(value?.Trim(), ["M/d", "MM/dd"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            throw new ValidationErrorException($"{label} must use M/d format, for example 3/21.");
        }

        return parsed.Month * 32 + parsed.Day;
    }

    internal static int ParseMinuteOfDay(string? value, string label)
    {
        if (!TimeOnly.TryParseExact(value?.Trim(), ["H:mm", "HH:mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            throw new ValidationErrorException($"{label} must use 24-hour HH:mm format.");
        }

        return parsed.Hour * 60 + parsed.Minute;
    }

    internal static uint ParseIpv4(string? value, string label)
    {
        if (!IPAddress.TryParse(value, out var address) || address.AddressFamily != AddressFamily.InterNetwork)
        {
            throw new ValidationErrorException($"{label} must be a valid IPv4 address.");
        }

        var bytes = address.GetAddressBytes();
        return ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
    }
}

/// <summary>Converts readable request properties into the exact keys accepted by the connected AiFace firmware.</summary>
internal static class InstitutionDeviceRuntimeSettingsPayloadBuilder
{
    internal static string BuildSetDeviceInfo(InstitutionDeviceSettingRequestDTO dto, InstitutionDeviceSettingsSection section)
    {
        var fields = section switch
        {
            InstitutionDeviceSettingsSection.Time when dto is UpdateInstitutionDeviceTimeSettingsRequestDTO time => BuildTime(time),
            InstitutionDeviceSettingsSection.Bell when dto is UpdateInstitutionDeviceBellSettingsRequestDTO bell => BuildBell(bell),
            InstitutionDeviceSettingsSection.DeviceSetup when dto is UpdateInstitutionDeviceSetupSettingsRequestDTO setup => BuildDeviceSetup(setup),
            InstitutionDeviceSettingsSection.Advanced when dto is UpdateInstitutionDeviceAdvancedSettingsRequestDTO advanced => BuildAdvanced(advanced),
            InstitutionDeviceSettingsSection.Lock when dto is UpdateInstitutionDeviceLockSettingsRequestDTO lockSettings => BuildLock(lockSettings),
            InstitutionDeviceSettingsSection.Serial when dto is UpdateInstitutionDeviceSerialSettingsRequestDTO serial => BuildSerial(serial),
            InstitutionDeviceSettingsSection.Ethernet when dto is UpdateInstitutionDeviceEthernetSettingsRequestDTO ethernet => BuildEthernet(ethernet),
            InstitutionDeviceSettingsSection.Wifi when dto is UpdateInstitutionDeviceWifiSettingsRequestDTO wifi => BuildWifi(wifi),
            InstitutionDeviceSettingsSection.AppNotification when dto is UpdateInstitutionDeviceAppNotificationSettingsRequestDTO notification => BuildAppNotification(notification),
            InstitutionDeviceSettingsSection.WebAccess when dto is UpdateInstitutionDeviceWebAccessRequestDTO webAccess => BuildWebAccess(webAccess),
            InstitutionDeviceSettingsSection.ScreenMenuPin when dto is UpdateInstitutionDeviceScreenMenuPinRequestDTO pin => BuildScreenMenuPin(pin),
            _ => throw new ValidationErrorException("The supplied settings do not match the requested device section.")
        };

        fields["cmd"] = DeviceCommands.SetDeviceInfo;
        fields["password"] = dto.CurrentWebServerPassword;
        fields["nowtime"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return JsonSerializer.Serialize(fields);
    }

    internal static string BuildSetTime(SyncInstitutionDeviceTimeRequestDTO dto)
    {
        var utcDateTime = dto.UtcDateTime?.ToUniversalTime() ?? DateTime.UtcNow;
        return JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["cmd"] = DeviceCommands.SetTime,
            ["password"] = dto.CurrentWebServerPassword,
            ["cloudtime"] = utcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            ["nowtime"] = new DateTimeOffset(utcDateTime).ToUnixTimeMilliseconds()
        });
    }

    internal static string BuildGatewayReplacement(string currentWebServerPassword, string pendingGatewayUrl) =>
        JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["cmd"] = DeviceCommands.SetDeviceInfo,
            ["password"] = currentWebServerPassword,
            ["nowtime"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ["use_bs"] = 1,
            ["use_domain_name"] = 1,
            ["bs_domain_name"] = pendingGatewayUrl,
            ["serverport"] = 443
        });

    internal static string GetSectionLabel(InstitutionDeviceSettingsSection section) => section switch
    {
        InstitutionDeviceSettingsSection.Time => "Time",
        InstitutionDeviceSettingsSection.Bell => "Bell",
        InstitutionDeviceSettingsSection.DeviceSetup => "Device setup",
        InstitutionDeviceSettingsSection.Advanced => "Advanced",
        InstitutionDeviceSettingsSection.Lock => "Door and access control",
        InstitutionDeviceSettingsSection.Serial => "Serial connection",
        InstitutionDeviceSettingsSection.Ethernet => "Ethernet network",
        InstitutionDeviceSettingsSection.Wifi => "Wi-Fi network",
        InstitutionDeviceSettingsSection.AppNotification => "App notification",
        InstitutionDeviceSettingsSection.WebAccess => "Local Web UI/API access",
        InstitutionDeviceSettingsSection.ScreenMenuPin => "Screen System-menu PIN",
        _ => "Device"
    };

    private static Dictionary<string, object?> BuildTime(UpdateInstitutionDeviceTimeSettingsRequestDTO dto)
    {
        InstitutionDeviceRuntimeSettingsValidation.ValidateOneOf(dto.TimeFormat, "clock format", 0, 1);
        InstitutionDeviceRuntimeSettingsValidation.ValidateOneOf(dto.DateFormat, "date format", 0, 1, 2);
        InstitutionDeviceRuntimeSettingsValidation.ValidateRange(dto.TimeZone, 0, 32, "Time zone");
        return new()
        {
            ["time_format"] = dto.TimeFormat,
            ["date_format"] = dto.DateFormat,
            ["use_dst"] = Bool(dto.DaylightSavingEnabled),
            ["dst_start"] = InstitutionDeviceRuntimeSettingsValidation.ParseMonthDay(dto.DaylightSavingStart, "DST start"),
            ["dst_end"] = InstitutionDeviceRuntimeSettingsValidation.ParseMonthDay(dto.DaylightSavingEnd, "DST end"),
            ["use_ntp"] = Bool(dto.NetworkTimeEnabled),
            ["ntp_timezone"] = dto.TimeZone,
            ["reboottime1"] = InstitutionDeviceRuntimeSettingsValidation.ParseMinuteOfDay(dto.RebootTime1, "Reboot time 1"),
            ["reboottime2"] = InstitutionDeviceRuntimeSettingsValidation.ParseMinuteOfDay(dto.RebootTime2, "Reboot time 2"),
            ["reboottime3"] = InstitutionDeviceRuntimeSettingsValidation.ParseMinuteOfDay(dto.RebootTime3, "Reboot time 3")
        };
    }

    private static Dictionary<string, object?> BuildBell(UpdateInstitutionDeviceBellSettingsRequestDTO dto)
    {
        InstitutionDeviceRuntimeSettingsValidation.ValidateRange(dto.BellCount, 0, 100, "Bell count");
        InstitutionDeviceRuntimeSettingsValidation.ValidateOneOf(dto.RingStyle, "bell ring pattern", 0, 1);
        InstitutionDeviceRuntimeSettingsValidation.ValidateOneOf(dto.BellOutput, "bell output", 0, 1);
        return new() { ["bellcount"] = dto.BellCount, ["ring_style"] = dto.RingStyle, ["belloutput"] = dto.BellOutput };
    }

    private static Dictionary<string, object?> BuildDeviceSetup(UpdateInstitutionDeviceSetupSettingsRequestDTO dto)
    {
        InstitutionDeviceRuntimeSettingsValidation.ValidateOneOf(dto.Language, "language", 0, 24);
        InstitutionDeviceRuntimeSettingsValidation.ValidateRange(dto.VoiceVolume, 0, 10, "Voice volume");
        InstitutionDeviceRuntimeSettingsValidation.ValidateRange(dto.ResultDisplaySeconds, 0, 99, "Result display time");
        InstitutionDeviceRuntimeSettingsValidation.ValidateRange(dto.ScreenSaverIdleSeconds, 0, 99_999_999, "Screen saver idle time");
        InstitutionDeviceRuntimeSettingsValidation.ValidateRange(dto.SleepModeSeconds, 0, 99_999_999, "Sleep mode time");
        InstitutionDeviceRuntimeSettingsValidation.ValidateOneOf(dto.ScreenWakeUpMethod, "screen wake-up method", 0, 1);
        InstitutionDeviceRuntimeSettingsValidation.ValidateRange(dto.FaceWakeUpSeconds, 0, 10, "Face wake-up time");
        InstitutionDeviceRuntimeSettingsValidation.ValidateOneOf(dto.ResultDisplayStyle, "result display style", 0, 1, 2);
        InstitutionDeviceRuntimeSettingsValidation.ValidateOneOf(dto.FaceRecognitionDistance, "face recognition distance", 0, 1, 2);
        return new()
        {
            ["language"] = dto.Language,
            ["volume"] = dto.VoiceVolume,
            ["tts_voice"] = Bool(dto.AnnouncePersonName),
            ["multifaces"] = Bool(dto.DetectMultipleFaces),
            ["showresulttime"] = dto.ResultDisplaySeconds,
            ["screensavertime"] = dto.ScreenSaverIdleSeconds,
            ["sleeptime"] = dto.SleepModeSeconds,
            ["screensaver_wakeup"] = dto.ScreenWakeUpMethod,
            ["face_wakeup_seconds"] = dto.FaceWakeUpSeconds,
            ["label_style"] = dto.ResultDisplayStyle,
            ["identify_distance"] = dto.FaceRecognitionDistance,
            ["live_detect"] = Bool(dto.LivenessDetectionEnabled),
            ["show_avatar"] = Bool(dto.ShowAvatar)
        };
    }

    private static Dictionary<string, object?> BuildAdvanced(UpdateInstitutionDeviceAdvancedSettingsRequestDTO dto)
    {
        InstitutionDeviceRuntimeSettingsValidation.ValidateRange(dto.MaximumAdministrators, 0, 99_999_999, "Maximum administrators");
        InstitutionDeviceRuntimeSettingsValidation.ValidateOneOf(dto.VerificationMode, "verification method", 0, 2, 3, 8, 9, 10, 11, 14);
        InstitutionDeviceRuntimeSettingsValidation.ValidateOneOf(dto.QrCodeMode, "QR code mode", 0, 1, 2, 3);
        InstitutionDeviceRuntimeSettingsValidation.ValidateRange(dto.FaceMatchThreshold, 0, 99, "Face match threshold");
        InstitutionDeviceRuntimeSettingsValidation.ValidateRange(dto.LivenessThreshold, 0, 99, "Liveness threshold");
        InstitutionDeviceRuntimeSettingsValidation.ValidateRange(dto.FingerprintMatchThreshold, 0, 10, "Fingerprint threshold");
        InstitutionDeviceRuntimeSettingsValidation.ValidateRange(dto.FingerprintsPerUser, 0, 10, "Fingerprints per user");
        InstitutionDeviceRuntimeSettingsValidation.ValidateRange(dto.MaskThreshold, 0, 99, "Mask threshold");
        InstitutionDeviceRuntimeSettingsValidation.ValidateOneOf(dto.FillLightMode, "fill light mode", 0, 1, 2);
        InstitutionDeviceRuntimeSettingsValidation.ValidateRange(dto.ExposureCompensation, 0, 100, "Exposure compensation");
        InstitutionDeviceRuntimeSettingsValidation.ValidateRange(dto.PalmVeinMatchThreshold, 0, 99, "Palm vein threshold");
        InstitutionDeviceRuntimeSettingsValidation.ValidateRange(dto.PalmDetectionThreshold, 0, 99, "Palm detection threshold");
        return new()
        {
            ["managers"] = dto.MaximumAdministrators,
            ["verifymode"] = dto.VerificationMode,
            ["use_qrcode"] = dto.QrCodeMode,
            ["hide_privacy"] = Bool(dto.HidePrivacyInformation),
            ["fcmatch_level"] = dto.FaceMatchThreshold,
            ["live_treshold"] = dto.LivenessThreshold,
            ["fpmatch_level"] = dto.FingerprintMatchThreshold,
            ["fps_peruser"] = dto.FingerprintsPerUser,
            ["wear_mask"] = Bool(dto.MaskDetectionEnabled),
            ["mask_treshold"] = dto.MaskThreshold,
            ["filllight"] = dto.FillLightMode,
            ["filllight_period"] = ParseTimeRange(dto.ConstantFillLightPeriod, "Constant fill-light period"),
            ["exposure_value"] = dto.ExposureCompensation,
            ["palm_vein_level"] = dto.PalmVeinMatchThreshold,
            ["palm_det_level"] = dto.PalmDetectionThreshold,
            ["disable_face"] = Bool(dto.DisableFaceRecognition),
            ["online_debug"] = Bool(dto.OnlineDebugEnabled)
        };
    }

    private static Dictionary<string, object?> BuildLock(UpdateInstitutionDeviceLockSettingsRequestDTO dto)
    {
        InstitutionDeviceRuntimeSettingsValidation.ValidateRange(dto.DoorOpenDelaySeconds, 0, 99_999_999, "Door open delay");
        InstitutionDeviceRuntimeSettingsValidation.ValidateOneOf(dto.DoorSensorMode, "door sensor mode", 0, 1, 2);
        InstitutionDeviceRuntimeSettingsValidation.ValidateRange(dto.DoorSensorDelaySeconds, 0, 99_999_999, "Door sensor delay");
        InstitutionDeviceRuntimeSettingsValidation.ValidateRange(dto.DoorPassword, 0, 99_999_999, "Door password");
        InstitutionDeviceRuntimeSettingsValidation.ValidateRange(dto.RequiredUsersForDoorOpen, 0, 99, "Required users");
        InstitutionDeviceRuntimeSettingsValidation.ValidateOneOf(dto.AntiPassbackMode, "anti-passback mode", 0, 1, 2, 3);
        InstitutionDeviceRuntimeSettingsValidation.ValidateOneOf(dto.WiegandOutput, "Wiegand output", 0, 1, 2, 3, 4);
        InstitutionDeviceRuntimeSettingsValidation.ValidateOneOf(dto.WiegandFormat, "Wiegand format", 0, 1, 2, 3, 4, 5);
        InstitutionDeviceRuntimeSettingsValidation.ValidateRange(dto.AccessLimit, 0, 65_535, "Access limit");
        InstitutionDeviceRuntimeSettingsValidation.ValidateOneOf(dto.CardDisplayFormat, "card display format", 0, 1, 2);
        InstitutionDeviceRuntimeSettingsValidation.ValidateRange(dto.FailedVerificationLimit, 0, 99, "Failed verification limit");
        InstitutionDeviceRuntimeSettingsValidation.ValidateRange(dto.TimeZonePunchLimit, 0, 255, "Time-zone punch limit");
        return new()
        {
            ["door_opentime"] = dto.DoorOpenDelaySeconds,
            ["door_sensor_type"] = dto.DoorSensorMode,
            ["door_alarm_time"] = dto.DoorSensorDelaySeconds,
            ["stranger_lock"] = Bool(dto.BlockStrangerAccess),
            ["door_password"] = dto.DoorPassword,
            ["door_mutiopen"] = dto.RequiredUsersForDoorOpen,
            ["door_antipass"] = dto.AntiPassbackMode,
            ["wiegand_output_mode"] = dto.WiegandOutput,
            ["wiegand_output_bit"] = dto.WiegandFormat,
            ["access_times"] = dto.AccessLimit,
            ["card_disp_format"] = dto.CardDisplayFormat,
            ["card_reversal"] = Bool(dto.ReverseCardPin),
            ["wg_reversal"] = Bool(dto.ReverseWiegandOutput),
            ["outwg_shot"] = Bool(dto.ExternalWiegandSnapshotEnabled),
            ["door_interlock"] = Bool(dto.InterlockEnabled),
            ["door_firealarmin"] = Bool(dto.AlarmProcessingEnabled),
            ["door_trycount"] = dto.FailedVerificationLimit,
            ["timezone_punchs"] = dto.TimeZonePunchLimit,
            ["access_denied_nolog"] = Bool(dto.SuppressAccessDeniedLog),
            ["outof_notimzone_denied"] = Bool(dto.DenyOutsideNormallyOpenTimeZone)
        };
    }

    private static Dictionary<string, object?> BuildSerial(UpdateInstitutionDeviceSerialSettingsRequestDTO dto)
    {
        InstitutionDeviceRuntimeSettingsValidation.ValidateRange(dto.DeviceAddress, 0, 9_999, "Device address");
        InstitutionDeviceRuntimeSettingsValidation.ValidateRange(dto.NetworkPort, 1, 99_999_999, "Network port");
        InstitutionDeviceRuntimeSettingsValidation.ValidateOneOf(dto.BaudRate, "baud rate", 0, 1, 2, 3, 4);
        InstitutionDeviceRuntimeSettingsValidation.ValidateOneOf(dto.SerialFunction, "serial function", 0, 1, 2);
        return new() { ["deviceid"] = dto.DeviceAddress, ["netportnum"] = dto.NetworkPort, ["combaurate"] = dto.BaudRate, ["rs485_fun"] = dto.SerialFunction };
    }

    private static Dictionary<string, object?> BuildEthernet(UpdateInstitutionDeviceEthernetSettingsRequestDTO dto)
    {
        var fields = new Dictionary<string, object?> { ["use_eth_dhcp"] = Bool(dto.DhcpEnabled), ["hide_ip"] = Bool(dto.HideIpAddress) };
        if (!dto.DhcpEnabled)
        {
            fields["netipaddress"] = InstitutionDeviceRuntimeSettingsValidation.ParseIpv4(dto.IpAddress, "Ethernet IP address");
            fields["netmask"] = InstitutionDeviceRuntimeSettingsValidation.ParseIpv4(dto.SubnetMask, "Ethernet subnet mask");
            fields["netgateway"] = InstitutionDeviceRuntimeSettingsValidation.ParseIpv4(dto.Gateway, "Ethernet gateway");
            fields["dns_server_ip"] = InstitutionDeviceRuntimeSettingsValidation.ParseIpv4(dto.DnsServer, "DNS server");
        }

        return fields;
    }

    private static Dictionary<string, object?> BuildWifi(UpdateInstitutionDeviceWifiSettingsRequestDTO dto)
    {
        var fields = new Dictionary<string, object?> { ["use_wlan_dhcp"] = Bool(dto.DhcpEnabled) };
        if (!dto.DhcpEnabled)
        {
            fields["wlan_ip_address"] = InstitutionDeviceRuntimeSettingsValidation.ParseIpv4(dto.IpAddress, "Wi-Fi IP address");
            fields["wlan_net_mask"] = InstitutionDeviceRuntimeSettingsValidation.ParseIpv4(dto.SubnetMask, "Wi-Fi subnet mask");
            fields["wlan_gateway"] = InstitutionDeviceRuntimeSettingsValidation.ParseIpv4(dto.Gateway, "Wi-Fi gateway");
        }

        return fields;
    }

    private static Dictionary<string, object?> BuildAppNotification(UpdateInstitutionDeviceAppNotificationSettingsRequestDTO dto)
    {
        InstitutionDeviceRuntimeSettingsValidation.ValidateOneOf(dto.NotificationType, "notification frequency", 0, 1, 2, 3);
        if (dto.AppNotificationEnabled && string.IsNullOrWhiteSpace(dto.AppToken))
        {
            throw new ValidationErrorException("An app token is required when app notifications are enabled.");
        }

        if (dto.AppToken?.Length > 4_000)
        {
            throw new ValidationErrorException("App token cannot exceed 4,000 characters.");
        }

        var fields = new Dictionary<string, object?>
        {
            ["line_notify"] = Bool(dto.AppNotificationEnabled),
            ["line_push_type"] = dto.NotificationType
        };
        if (!string.IsNullOrWhiteSpace(dto.AppToken))
        {
            fields["app_token"] = dto.AppToken;
        }

        return fields;
    }

    private static Dictionary<string, object?> BuildWebAccess(UpdateInstitutionDeviceWebAccessRequestDTO dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.NewWebServerPassword) && dto.NewWebServerPassword.Length is < 4 or > 128)
        {
            throw new ValidationErrorException("New local Web UI/API password must be between 4 and 128 characters.");
        }

        var fields = new Dictionary<string, object?> { ["use_webserver"] = Bool(dto.LocalWebServerEnabled) };
        if (!string.IsNullOrWhiteSpace(dto.NewWebServerPassword))
        {
            fields["webserver_pwd"] = dto.NewWebServerPassword;
        }

        return fields;
    }

    private static Dictionary<string, object?> BuildScreenMenuPin(UpdateInstitutionDeviceScreenMenuPinRequestDTO dto)
    {
        if (dto.ScreenMenuPin.Length is < 4 or > 12 || !dto.ScreenMenuPin.All(char.IsDigit))
        {
            throw new ValidationErrorException("Screen System-menu PIN must contain 4 to 12 digits.");
        }

        return new() { ["local_manager_pwd"] = long.Parse(dto.ScreenMenuPin, CultureInfo.InvariantCulture) };
    }

    private static int Bool(bool value) => value ? 1 : 0;

    private static int ParseTimeRange(string? value, string label)
    {
        var parts = value?.Split('~', StringSplitOptions.TrimEntries);
        if (parts is null || parts.Length != 2)
        {
            throw new ValidationErrorException($"{label} must use HH:mm~HH:mm format.");
        }

        var start = InstitutionDeviceRuntimeSettingsValidation.ParseMinuteOfDay(parts[0], label);
        var end = InstitutionDeviceRuntimeSettingsValidation.ParseMinuteOfDay(parts[1], label);
        return ((start / 60) << 24) | ((start % 60) << 16) | ((end / 60) << 8) | (end % 60);
    }
}
