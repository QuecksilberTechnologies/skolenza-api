// ================================================================
// Purpose : Handles authenticated MQTT command submission. The durable storage
//           service resolves serial, Institution, and location from InstitutionDevice.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOS.Host;
using skolenza.application.Features.HostDeviceCmd.Handlers;
using skolenza.application.Features.InstitutionConfigurationCmd.Handlers;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IDeviceCommunication;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.DeviceCommandCmd;

/// <summary>Queues a supported MQTT command after authorization has completed.</summary>
public sealed class SubmitDeviceCommand(SubmitDeviceCommandRequestDTO dto)
    : IRequest<ApiResponse<DeviceCommandSubmissionResponseDTO>>
{
    public SubmitDeviceCommandRequestDTO DTO { get; } = dto;
}

/// <summary>Submits an authorized command into the per-device sequential pipeline.</summary>
public sealed class SubmitDeviceCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService,
    IDeviceCommandSubmissionService deviceCommandSubmissionService,
    ILogger<InstitutionConfigurationHandlerBase> institutionLogger)
    : InstitutionDeviceAccessHandlerBase(unitOfWork, commonRequestService, idEncoderService, institutionLogger),
        IRequestHandler<SubmitDeviceCommand, ApiResponse<DeviceCommandSubmissionResponseDTO>>
{
    /// <inheritdoc />
    public async Task<ApiResponse<DeviceCommandSubmissionResponseDTO>> Handle(
        SubmitDeviceCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DTO is null || request.DTO.InstitutionDeviceId <= 0)
        {
            throw new ArgumentException("A valid Institution device identifier is required.");
        }

        var definition = DeviceProtocolCommandCatalog.GetRequired(request.DTO.CommandName);
        var scope = await ResolveInstitutionScopeAsync(request.DTO, cancellationToken);
        var payload = DeviceProtocolCommandCatalog.ValidatePayload(definition.Name, request.DTO.Payload);
        var result = await deviceCommandSubmissionService.SubmitAsync(
            new DeviceCommandSubmission(
                scope.InstitutionId,
                request.DTO.InstitutionDeviceId,
                definition.Name,
                payload,
                scope.ActorId),
            cancellationToken);

        return ApiResponse<DeviceCommandSubmissionResponseDTO>.Success(
            new DeviceCommandSubmissionResponseDTO
            {
                DeviceCommandId = result.DeviceCommandId,
                InternalTrackingId = result.InternalTrackingId,
                DeviceSerialNumber = result.DeviceSerialNumber,
                Status = result.Status.ToString()
            },
            "Device command queued successfully.");
    }
}
