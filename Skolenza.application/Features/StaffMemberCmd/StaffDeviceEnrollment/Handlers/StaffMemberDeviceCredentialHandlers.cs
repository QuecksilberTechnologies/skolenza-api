// ================================================================
// Purpose : Queues write-only face, PIN and card credentials. Raw biometric
//           data and PINs are protected queue payloads and are never stored.
// ================================================================

using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using AutoMapper;
using skolenza.application.Constants;
using skolenza.application.DTOS.InstitutionConfiguration;
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

namespace skolenza.application.Features.StaffMemberCmd.StaffMemberDeviceEnrollment.Handlers;

public sealed class UpsertStaffMemberDeviceFaceCommand(UpsertStaffMemberDeviceFaceRequestDTO dto) : IRequest<ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>> { public UpsertStaffMemberDeviceFaceRequestDTO DTO { get; } = dto; }
public sealed class UpsertStaffMemberDevicePinCommand(UpsertStaffMemberDevicePinRequestDTO dto) : IRequest<ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>> { public UpsertStaffMemberDevicePinRequestDTO DTO { get; } = dto; }
public sealed class BindStaffMemberDeviceCardCommand(BindStaffMemberDeviceCardRequestDTO dto) : IRequest<ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>> { public BindStaffMemberDeviceCardRequestDTO DTO { get; } = dto; }
public sealed class RemoveStaffMemberDeviceCredentialCommand(RemoveStaffMemberDeviceCredentialRequestDTO dto) : IRequest<ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>> { public RemoveStaffMemberDeviceCredentialRequestDTO DTO { get; } = dto; }

public sealed class UpsertStaffMemberDeviceFaceCommandHandler(IUnitOfWork u, IMapper m, ICommonRequestService c, ILogger<InstitutionConfigurationHandlerBase> l, IIdEncoderService ids, IDeviceCommandSubmissionService commands, IEncryptionService encryption)
    : StaffMemberDeviceEnrollmentHandlerBase(u, c, l, ids, commands, encryption), IRequestHandler<UpsertStaffMemberDeviceFaceCommand, ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>>
{
    public async Task<ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>> Handle(UpsertStaffMemberDeviceFaceCommand request, CancellationToken ct)
    {
        var dto = request.DTO ?? throw new ValidationErrorException("Face image is required."); var (context, id) = await EnrollmentContext(dto.EnrollmentId, ct);
        if (dto.FaceImage is null || dto.FaceImage.Length is <= 0 or > 2_000_000 || dto.FaceImage.ContentType is not ("image/jpeg" or "image/png")) throw new ValidationErrorException("Upload a JPEG or PNG face image no larger than 2 MB.");
        var e = await UnitOfWork.StaffMemberDeviceEnrollmentRepository.GetForUpdateAsync(context.InstitutionId, id, ct) ?? throw new NotFoundException("StaffMember device enrollment was not found."); await EnsureStaffMemberDataAccessAsync(context, e.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, ct);
        await using var stream = new MemoryStream(); await dto.FaceImage.CopyToAsync(stream, ct); var bytes = stream.ToArray();
        var command = await Commands.SubmitAsync(new DeviceCommandSubmission(e.InstitutionId, e.InstitutionDeviceId, DeviceCommands.SetUserInfo, JsonSerializer.Serialize(new { cmd = DeviceCommands.SetUserInfo, enrollid = long.Parse(e.EnrollId, CultureInfo.InvariantCulture), admin = 0, backupnum = 50, record = Convert.ToBase64String(bytes) }), context.LoggedInStaffMemberId, ProtectPayload: true), ct);
        e.FaceImageHash = Convert.ToHexString(SHA256.HashData(bytes)); e.FaceDeviceCommandId = command.DeviceCommandId; e.FaceDeploymentStatus = (short)DeviceCredentialDeploymentStatus.Queued; e.UpdatedById = context.LoggedInStaffMemberId; e.UpdatedDateTime = DateTime.UtcNow; await UnitOfWork.SaveChangesAsync(ct);
        var read = await UnitOfWork.StaffMemberDeviceEnrollmentRepository.GetByIdAsync(context.InstitutionId, id, ct) ?? e; return ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>.Success(ToResponse(m, read, context.Claims.InstitutionEncriptionKey), "Face enrollment has been queued for the device.");
    }
}

public sealed class UpsertStaffMemberDevicePinCommandHandler(IUnitOfWork u, IMapper m, ICommonRequestService c, ILogger<InstitutionConfigurationHandlerBase> l, IIdEncoderService ids, IDeviceCommandSubmissionService commands, IEncryptionService encryption)
    : StaffMemberDeviceEnrollmentHandlerBase(u, c, l, ids, commands, encryption), IRequestHandler<UpsertStaffMemberDevicePinCommand, ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>>
{
    public async Task<ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>> Handle(UpsertStaffMemberDevicePinCommand request, CancellationToken ct)
    {
        var dto = request.DTO ?? throw new ValidationErrorException("PIN is required."); if (dto.Pin.Length is < 4 or > 12 || dto.Pin.Any(x => !char.IsAsciiDigit(x))) throw new ValidationErrorException("Device PIN must contain 4 to 12 digits.");
        var (context, id) = await EnrollmentContext(dto.EnrollmentId, ct); var e = await UnitOfWork.StaffMemberDeviceEnrollmentRepository.GetForUpdateAsync(context.InstitutionId, id, ct) ?? throw new NotFoundException("StaffMember device enrollment was not found."); await EnsureStaffMemberDataAccessAsync(context, e.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, ct);
        var command = await Commands.SubmitAsync(new DeviceCommandSubmission(e.InstitutionId, e.InstitutionDeviceId, DeviceCommands.SetUserInfo, JsonSerializer.Serialize(new { cmd = DeviceCommands.SetUserInfo, enrollid = long.Parse(e.EnrollId, CultureInfo.InvariantCulture), admin = 0, backupnum = 10, pwd = dto.Pin }), context.LoggedInStaffMemberId, ProtectPayload: true), ct);
        e.PinDeviceCommandId = command.DeviceCommandId; e.PinDeploymentStatus = (short)DeviceCredentialDeploymentStatus.Queued; e.UpdatedById = context.LoggedInStaffMemberId; e.UpdatedDateTime = DateTime.UtcNow; await UnitOfWork.SaveChangesAsync(ct); var read = await UnitOfWork.StaffMemberDeviceEnrollmentRepository.GetByIdAsync(context.InstitutionId, id, ct) ?? e; return ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>.Success(ToResponse(m, read, context.Claims.InstitutionEncriptionKey), "PIN enrollment has been queued for the device.");
    }
}

public sealed class BindStaffMemberDeviceCardCommandHandler(IUnitOfWork u, IMapper m, ICommonRequestService c, ILogger<InstitutionConfigurationHandlerBase> l, IIdEncoderService ids, IDeviceCommandSubmissionService commands, IEncryptionService encryption)
    : StaffMemberDeviceEnrollmentHandlerBase(u, c, l, ids, commands, encryption), IRequestHandler<BindStaffMemberDeviceCardCommand, ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>>
{
    public async Task<ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>> Handle(BindStaffMemberDeviceCardCommand request, CancellationToken ct)
    {
        var dto = request.DTO ?? throw new ValidationErrorException("Card is required."); var (context, id) = await EnrollmentContext(dto.EnrollmentId, ct); var cardId = Decode(dto.InstitutionCardId, context.Claims.InstitutionEncriptionKey, "InstitutionCardId");
        var e = await UnitOfWork.StaffMemberDeviceEnrollmentRepository.GetForUpdateAsync(context.InstitutionId, id, ct) ?? throw new NotFoundException("StaffMember device enrollment was not found."); await EnsureStaffMemberDataAccessAsync(context, e.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, ct);
        var card = await UnitOfWork.StaffMemberDeviceEnrollmentRepository.GetCardForUpdateAsync(context.InstitutionId, cardId, ct) ?? throw new ValidationErrorException("The selected card is not available."); if (!card.IsActive || card.CardStatus != (short)InstitutionCardStatus.Available) throw new ValidationErrorException("The selected card is not available.");
        var value = Encryption.Decrypt(card.CardNumberEncrypted, context.Claims.InstitutionEncriptionKey); if (!long.TryParse(value, out _)) throw new ValidationErrorException("The selected card number cannot be sent to this device.");
        if (e.InstitutionCardMaster is not null) e.InstitutionCardMaster.CardStatus = (short)InstitutionCardStatus.Available; e.InstitutionCardMasterId = card.Id; e.InstitutionCardMaster = card; card.CardStatus = (short)InstitutionCardStatus.Assigned; e.CardDeploymentStatus = (short)DeviceCredentialDeploymentStatus.Queued; e.UpdatedById = context.LoggedInStaffMemberId; e.UpdatedDateTime = DateTime.UtcNow;
        var command = await Commands.SubmitAsync(new DeviceCommandSubmission(e.InstitutionId, e.InstitutionDeviceId, DeviceCommands.SetUserInfo, JsonSerializer.Serialize(new { cmd = DeviceCommands.SetUserInfo, enrollid = long.Parse(e.EnrollId, CultureInfo.InvariantCulture), admin = 0, backupnum = 11, card = value }), context.LoggedInStaffMemberId, ProtectPayload: true), ct); e.CardDeviceCommandId = command.DeviceCommandId; await UnitOfWork.SaveChangesAsync(ct);
        var read = await UnitOfWork.StaffMemberDeviceEnrollmentRepository.GetByIdAsync(context.InstitutionId, id, ct) ?? e; return ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>.Success(ToResponse(m, read, context.Claims.InstitutionEncriptionKey), "Card binding has been queued for the device.");
    }
}

public sealed class RemoveStaffMemberDeviceCredentialCommandHandler(IUnitOfWork u, IMapper m, ICommonRequestService c, ILogger<InstitutionConfigurationHandlerBase> l, IIdEncoderService ids, IDeviceCommandSubmissionService commands, IEncryptionService encryption)
    : StaffMemberDeviceEnrollmentHandlerBase(u, c, l, ids, commands, encryption), IRequestHandler<RemoveStaffMemberDeviceCredentialCommand, ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>>
{
    public async Task<ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>> Handle(RemoveStaffMemberDeviceCredentialCommand request, CancellationToken ct)
    {
        var dto = request.DTO ?? throw new ValidationErrorException("Credential type is required."); var (context, id) = await EnrollmentContext(dto.EnrollmentId, ct); var e = await UnitOfWork.StaffMemberDeviceEnrollmentRepository.GetForUpdateAsync(context.InstitutionId, id, ct) ?? throw new NotFoundException("StaffMember device enrollment was not found."); await EnsureStaffMemberDataAccessAsync(context, e.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, ct);
        var backup = dto.CredentialType switch { StaffMemberDeviceCredentialType.Face => 50, StaffMemberDeviceCredentialType.Card => 11, StaffMemberDeviceCredentialType.Pin => 10, _ => throw new ValidationErrorException("Select a valid credential.") };
        await Commands.SubmitAsync(new DeviceCommandSubmission(e.InstitutionId, e.InstitutionDeviceId, DeviceCommands.DeleteUser, JsonSerializer.Serialize(new { cmd = DeviceCommands.DeleteUser, enrollid = long.Parse(e.EnrollId, CultureInfo.InvariantCulture), backupnum = backup }), context.LoggedInStaffMemberId, ProtectPayload: true), ct);
        if (dto.CredentialType == StaffMemberDeviceCredentialType.Face) { e.FaceImageHash = null; e.FaceDeploymentStatus = (short)DeviceCredentialDeploymentStatus.Removed; e.IsFaceEnrolled = false; }
        if (dto.CredentialType == StaffMemberDeviceCredentialType.Pin) e.PinDeploymentStatus = (short)DeviceCredentialDeploymentStatus.Removed;
        if (dto.CredentialType == StaffMemberDeviceCredentialType.Card) { if (e.InstitutionCardMaster is not null) e.InstitutionCardMaster.CardStatus = (short)InstitutionCardStatus.Available; e.InstitutionCardMasterId = null; e.CardDeploymentStatus = (short)DeviceCredentialDeploymentStatus.Removed; e.IsCardEnrolled = false; }
        e.UpdatedById = context.LoggedInStaffMemberId; e.UpdatedDateTime = DateTime.UtcNow; await UnitOfWork.SaveChangesAsync(ct); var read = await UnitOfWork.StaffMemberDeviceEnrollmentRepository.GetByIdAsync(context.InstitutionId, id, ct) ?? e; return ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>.Success(ToResponse(m, read, context.Claims.InstitutionEncriptionKey), "Credential removal has been queued for the device.");
    }
}
