// ================================================================
// Purpose : Institution-safe staffMember/device enrollment. Device enrollid is the
//           decrypted database StaffMember.Id, never a UI supplied value.
// ================================================================

using System.Globalization;
using System.Text.Json;
using AutoMapper;
using skolenza.application.Common.Models.Security;
using skolenza.application.Constants;
using skolenza.application.DTOs.BaseDTO;
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
using StaffMemberDeviceEnrollmentEntity = skolenza.domain.Entity.StaffMemberDeviceEnrollment;

namespace skolenza.application.Features.StaffMemberCmd.StaffMemberDeviceEnrollment.Handlers;

public sealed class CreateStaffMemberDeviceEnrollmentCommand(CreateStaffMemberDeviceEnrollmentRequestDTO dto) : IRequest<ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>> { public CreateStaffMemberDeviceEnrollmentRequestDTO DTO { get; } = dto; }
public sealed class UpdateStaffMemberDeviceEnrollmentCommand(UpdateStaffMemberDeviceEnrollmentRequestDTO dto) : IRequest<ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>> { public UpdateStaffMemberDeviceEnrollmentRequestDTO DTO { get; } = dto; }
public sealed class DeleteStaffMemberDeviceEnrollmentCommand(string id, PermissionRequestDTO permissionRequest) : IRequest<ApiResponse<bool>> { public string Id { get; } = id; public PermissionRequestDTO PermissionRequest { get; } = permissionRequest; }
public sealed class UpdateStaffMemberDeviceEnrollmentStatusCommand(UpdateStaffMemberDeviceEnrollmentStatusRequestDTO dto) : IRequest<ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>> { public UpdateStaffMemberDeviceEnrollmentStatusRequestDTO DTO { get; } = dto; }
public sealed class GetStaffMemberDeviceEnrollmentByIdQuery(string id, PermissionRequestDTO permissionRequest) : IRequest<ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>> { public string Id { get; } = id; public PermissionRequestDTO PermissionRequest { get; } = permissionRequest; }
public sealed class GetStaffMemberDeviceEnrollmentsQuery(StaffMemberDeviceEnrollmentFilterRequestDTO filter) : IRequest<ApiResponse<List<StaffMemberDeviceEnrollmentResponseDTO>>> { public StaffMemberDeviceEnrollmentFilterRequestDTO Filter { get; } = filter; }

public abstract class StaffMemberDeviceEnrollmentHandlerBase(IUnitOfWork unitOfWork, ICommonRequestService common, ILogger<InstitutionConfigurationHandlerBase> logger, IIdEncoderService ids, IDeviceCommandSubmissionService commands, IEncryptionService encryption)
    : InstitutionConfigurationHandlerBase(unitOfWork, common, logger, ids)
{
    protected IIdEncoderService Ids { get; } = ids;
    protected IDeviceCommandSubmissionService Commands { get; } = commands;
    protected IEncryptionService Encryption { get; } = encryption;

    protected long Decode(string? value, string key, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ValidationErrorException($"A valid {name} is required.");
        try
        {
            var id = Ids.DecodeId_long(value.Trim(), key);
            if (id <= 0 || Ids.EncodeId_long(id, key) != value.Trim()) throw new ValidationErrorException($"A valid {name} is required.");
            return id;
        }
        catch (ValidationErrorException) { throw; }
        catch { throw new ValidationErrorException($"A valid {name} is required."); }
    }

    protected async Task<(CommonDecodedResult Context, long Id)> EnrollmentContext(string id, CancellationToken ct)
    {
        var context = await ValidateInstitutionDataAccessContextAsync();
        return (context, Decode(id, context.Claims.InstitutionEncriptionKey, "staffMember device enrollment identifier"));
    }

    protected static void ValidateWindows(IEnumerable<StaffMemberDeviceAccessWindowRequestDTO> windows, DateTime? from, DateTime? to)
    {
        if (from.HasValue && to.HasValue && from > to) throw new ValidationErrorException("Access validity end must be later than its start.");
        foreach (var item in windows)
            if (!Enum.IsDefined(item.DayOfWeek) || item.StartLocalTime >= item.EndLocalTime)
                throw new ValidationErrorException("Each working access window needs a valid day and start/end time.");
    }

    protected static void ReplaceWindows(StaffMemberDeviceEnrollmentEntity e, IEnumerable<StaffMemberDeviceAccessWindowRequestDTO> windows, long actorId)
    {
        var now = DateTime.UtcNow;
        foreach (var old in e.StaffMemberDeviceAccessWindows.Where(x => !x.IsSoftDeleted)) { old.IsSoftDeleted = true; old.IsActive = false; old.SoftDeletedById = actorId; old.SoftDeletedDateTime = now; }
        foreach (var item in windows) e.StaffMemberDeviceAccessWindows.Add(new StaffMemberDeviceAccessWindow { DayOfWeek = (short)item.DayOfWeek, StartLocalTime = item.StartLocalTime, EndLocalTime = item.EndLocalTime, IsActive = item.IsActive, AddedById = actorId, AddedDateTime = now });
    }

    protected async Task QueueBaselineAsync(StaffMemberDeviceEnrollmentEntity e, long actorId, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new
        {
            cmd = DeviceCommands.SetUserInfo,
            enrollid = long.Parse(e.EnrollId, CultureInfo.InvariantCulture),
            aliasid = e.StaffMember.EmployementCode ?? e.EnrollId,
            name = string.Join(" ", new[] { e.StaffMember.FirstName, e.StaffMember.MiddleName, e.StaffMember.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))),
            department = e.StaffMember.DepartmentId?.ToString(CultureInfo.InvariantCulture) ?? "0",
            admin = 0, // privilege is always User; Angular cannot alter it.
            enable = e.IsActive ? 1 : 0
        });
        await Commands.SubmitAsync(new DeviceCommandSubmission(e.InstitutionId, e.InstitutionDeviceId, DeviceCommands.SetUserInfo, payload, actorId, ProtectPayload: true), ct);
    }

    protected StaffMemberDeviceEnrollmentResponseDTO ToResponse(IMapper mapper, StaffMemberDeviceEnrollmentEntity e, string key)
    {
        var dto = mapper.Map<StaffMemberDeviceEnrollmentResponseDTO>(e);
        dto.Id = Ids.EncodeId_long(e.Id, key); dto.StaffMemberId = Ids.EncodeId_long(e.StaffMemberId, key); dto.InstitutionDeviceId = Ids.EncodeId_long(e.InstitutionDeviceId, key); dto.CampusId = Ids.EncodeId_long(e.CampusId, key);
        if (e.InstitutionCardMasterId.HasValue) dto.InstitutionCardId = Ids.EncodeId_long(e.InstitutionCardMasterId.Value, key);
        if (e.InstitutionCardMaster is not null)
        {
            var value = Encryption.Decrypt(e.InstitutionCardMaster.CardNumberEncrypted, key);
            dto.MaskedCardNumber = value.Length <= 4 ? "****" : $"****{value[^4..]}";
        }
        dto.FaceCommandStatus = e.FaceDeviceCommand is null ? null : (DeviceCommandStatus)e.FaceDeviceCommand.Status;
        dto.CardCommandStatus = e.CardDeviceCommand is null ? null : (DeviceCommandStatus)e.CardDeviceCommand.Status;
        dto.PinCommandStatus = e.PinDeviceCommand is null ? null : (DeviceCommandStatus)e.PinDeviceCommand.Status;
        dto.UserActivationCommandStatus = e.UserActivationDeviceCommand is null ? null : (DeviceCommandStatus)e.UserActivationDeviceCommand.Status;
        return dto;
    }
}

public sealed class CreateStaffMemberDeviceEnrollmentCommandHandler(IUnitOfWork u, IMapper m, ICommonRequestService c, ILogger<InstitutionConfigurationHandlerBase> l, IIdEncoderService ids, IDeviceCommandSubmissionService commands, IEncryptionService encryption)
    : StaffMemberDeviceEnrollmentHandlerBase(u, c, l, ids, commands, encryption), IRequestHandler<CreateStaffMemberDeviceEnrollmentCommand, ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>>
{
    public async Task<ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>> Handle(CreateStaffMemberDeviceEnrollmentCommand request, CancellationToken ct)
    {
        var dto = request.DTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.RequiredDataMissing);
        var context = await ValidateInstitutionDataAccessContextAsync();
        var staffMemberId = Decode(dto.StaffMemberId, context.Claims.InstitutionEncriptionKey, "StaffMemberId"); var deviceId = Decode(dto.InstitutionDeviceId, context.Claims.InstitutionEncriptionKey, "InstitutionDeviceId");
        await EnsureStaffMemberDataAccessAsync(context, staffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, ct); ValidateWindows(dto.AccessWindows, dto.AccessEffectiveFromDateTime, dto.AccessEffectiveToDateTime);
        var device = await UnitOfWork.StaffMemberDeviceEnrollmentRepository.GetEligibleInstitutionDeviceAsync(context.InstitutionId, deviceId, ct) ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidInstitutionConfigurationReference);
        if (!await UnitOfWork.StaffMemberDeviceEnrollmentRepository.IsEligibleStaffMemberAsync(context.InstitutionId, staffMemberId, ct) || await UnitOfWork.StaffMemberDeviceEnrollmentRepository.EnrollmentExistsAsync(context.InstitutionId, staffMemberId, deviceId, null, ct) || !await UnitOfWork.StaffMemberDeviceEnrollmentRepository.HasEligibleStaffMemberLocationAssignmentAsync(context.InstitutionId, staffMemberId, device.CampusId, DateOnly.FromDateTime(dto.AccessEffectiveFromDateTime ?? DateTime.UtcNow), ct)) throw new ValidationErrorException("The staffMember, device, or staffMember-location assignment is not eligible.");
        var entity = new StaffMemberDeviceEnrollmentEntity { InstitutionId = context.InstitutionId, StaffMemberId = staffMemberId, InstitutionDeviceId = deviceId, CampusId = device.CampusId, EnrollId = staffMemberId.ToString(CultureInfo.InvariantCulture), IsActive = dto.IsActive, AccessEffectiveFromDateTime = dto.AccessEffectiveFromDateTime, AccessEffectiveToDateTime = dto.AccessEffectiveToDateTime, AddedById = context.LoggedInStaffMemberId, AddedDateTime = DateTime.UtcNow };
        ReplaceWindows(entity, dto.AccessWindows, context.LoggedInStaffMemberId); await UnitOfWork.StaffMemberDeviceEnrollmentRepository.AddAsync(entity, ct); await UnitOfWork.SaveChangesAsync(ct);
        var tracked = await UnitOfWork.StaffMemberDeviceEnrollmentRepository.GetForUpdateAsync(context.InstitutionId, entity.Id, ct) ?? throw new NotFoundException(AppConstants.ErrorMessages.StaffMemberDeviceEnrollmentNotFound);
        await QueueBaselineAsync(tracked, context.LoggedInStaffMemberId, ct);
        var read = await UnitOfWork.StaffMemberDeviceEnrollmentRepository.GetByIdAsync(context.InstitutionId, entity.Id, ct) ?? tracked;
        Logger.LogInformation("Queued user enrollment for StaffMember {StaffMemberId} on InstitutionDevice {InstitutionDeviceId}.", staffMemberId, deviceId);
        return ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>.Success(ToResponse(m, read, context.Claims.InstitutionEncriptionKey), AppConstants.SuccessMessages.StaffMemberDeviceEnrollmentCreated);
    }
}

public sealed class UpdateStaffMemberDeviceEnrollmentCommandHandler(IUnitOfWork u, IMapper m, ICommonRequestService c, ILogger<InstitutionConfigurationHandlerBase> l, IIdEncoderService ids, IDeviceCommandSubmissionService commands, IEncryptionService encryption)
    : StaffMemberDeviceEnrollmentHandlerBase(u, c, l, ids, commands, encryption), IRequestHandler<UpdateStaffMemberDeviceEnrollmentCommand, ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>>
{
    public async Task<ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>> Handle(UpdateStaffMemberDeviceEnrollmentCommand request, CancellationToken ct)
    {
        var dto = request.DTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.RequiredDataMissing); var (context, id) = await EnrollmentContext(dto.Id, ct); var entity = await UnitOfWork.StaffMemberDeviceEnrollmentRepository.GetForUpdateAsync(context.InstitutionId, id, ct) ?? throw new NotFoundException(AppConstants.ErrorMessages.StaffMemberDeviceEnrollmentNotFound);
        await EnsureStaffMemberDataAccessAsync(context, entity.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, ct);
        if (Decode(dto.StaffMemberId, context.Claims.InstitutionEncriptionKey, "StaffMemberId") != entity.StaffMemberId || Decode(dto.InstitutionDeviceId, context.Claims.InstitutionEncriptionKey, "InstitutionDeviceId") != entity.InstitutionDeviceId) throw new ValidationErrorException("Create a new enrollment to change its staffMember or physical device.");
        if (dto.IsActive != entity.IsActive) throw new ValidationErrorException("Use update-status to enable or disable a device user.");
        ValidateWindows(dto.AccessWindows, dto.AccessEffectiveFromDateTime, dto.AccessEffectiveToDateTime); entity.AccessEffectiveFromDateTime = dto.AccessEffectiveFromDateTime; entity.AccessEffectiveToDateTime = dto.AccessEffectiveToDateTime; entity.UpdatedById = context.LoggedInStaffMemberId; entity.UpdatedDateTime = DateTime.UtcNow; ReplaceWindows(entity, dto.AccessWindows, context.LoggedInStaffMemberId); await UnitOfWork.SaveChangesAsync(ct);
        var read = await UnitOfWork.StaffMemberDeviceEnrollmentRepository.GetByIdAsync(context.InstitutionId, id, ct) ?? entity; return ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>.Success(ToResponse(m, read, context.Claims.InstitutionEncriptionKey), AppConstants.SuccessMessages.StaffMemberDeviceEnrollmentUpdated);
    }
}

public sealed class UpdateStaffMemberDeviceEnrollmentStatusCommandHandler(IUnitOfWork u, IMapper m, ICommonRequestService c, ILogger<InstitutionConfigurationHandlerBase> l, IIdEncoderService ids, IDeviceCommandSubmissionService commands, IEncryptionService encryption)
    : StaffMemberDeviceEnrollmentHandlerBase(u, c, l, ids, commands, encryption), IRequestHandler<UpdateStaffMemberDeviceEnrollmentStatusCommand, ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>>
{
    public async Task<ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>> Handle(UpdateStaffMemberDeviceEnrollmentStatusCommand request, CancellationToken ct)
    {
        var dto = request.DTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.RequiredDataMissing); var (context, id) = await EnrollmentContext(dto.Id, ct); var entity = await UnitOfWork.StaffMemberDeviceEnrollmentRepository.GetForUpdateAsync(context.InstitutionId, id, ct) ?? throw new NotFoundException(AppConstants.ErrorMessages.StaffMemberDeviceEnrollmentNotFound); await EnsureStaffMemberDataAccessAsync(context, entity.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, ct);
        entity.IsActive = dto.IsActive; entity.UpdatedById = context.LoggedInStaffMemberId; entity.UpdatedDateTime = DateTime.UtcNow;
        var command = await Commands.SubmitAsync(new DeviceCommandSubmission(entity.InstitutionId, entity.InstitutionDeviceId, DeviceCommands.EnableUser, JsonSerializer.Serialize(new { cmd = DeviceCommands.EnableUser, enrollid = long.Parse(entity.EnrollId, CultureInfo.InvariantCulture), enflag = dto.IsActive }), context.LoggedInStaffMemberId, ProtectPayload: true), ct);
        entity.UserActivationDeviceCommandId = command.DeviceCommandId;
        await UnitOfWork.SaveChangesAsync(ct);
        var read = await UnitOfWork.StaffMemberDeviceEnrollmentRepository.GetByIdAsync(context.InstitutionId, id, ct) ?? entity; return ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>.Success(ToResponse(m, read, context.Claims.InstitutionEncriptionKey), AppConstants.SuccessMessages.StaffMemberDeviceEnrollmentStatusUpdated);
    }
}

public sealed class DeleteStaffMemberDeviceEnrollmentCommandHandler(IUnitOfWork u, ICommonRequestService c, ILogger<InstitutionConfigurationHandlerBase> l, IIdEncoderService ids, IDeviceCommandSubmissionService commands, IEncryptionService encryption)
    : StaffMemberDeviceEnrollmentHandlerBase(u, c, l, ids, commands, encryption), IRequestHandler<DeleteStaffMemberDeviceEnrollmentCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(DeleteStaffMemberDeviceEnrollmentCommand request, CancellationToken ct)
    {
        var (context, id) = await EnrollmentContext(request.Id, ct); var e = await UnitOfWork.StaffMemberDeviceEnrollmentRepository.GetForUpdateAsync(context.InstitutionId, id, ct) ?? throw new NotFoundException(AppConstants.ErrorMessages.StaffMemberDeviceEnrollmentNotFound); await EnsureStaffMemberDataAccessAsync(context, e.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, ct);
        await Commands.SubmitAsync(new DeviceCommandSubmission(e.InstitutionId, e.InstitutionDeviceId, DeviceCommands.DeleteUser, JsonSerializer.Serialize(new { cmd = DeviceCommands.DeleteUser, enrollid = long.Parse(e.EnrollId, CultureInfo.InvariantCulture), backupnum = 12 }), context.LoggedInStaffMemberId, ProtectPayload: true), ct);
        if (e.InstitutionCardMaster is not null) e.InstitutionCardMaster.CardStatus = (short)InstitutionCardStatus.Available; e.IsActive = false; e.IsSoftDeleted = true; e.SoftDeletedById = context.LoggedInStaffMemberId; e.SoftDeletedDateTime = DateTime.UtcNow; await UnitOfWork.SaveChangesAsync(ct); return ApiResponse<bool>.Success(true, AppConstants.SuccessMessages.StaffMemberDeviceEnrollmentDeleted);
    }
}

public sealed class GetStaffMemberDeviceEnrollmentByIdQueryHandler(IUnitOfWork u, IMapper m, ICommonRequestService c, ILogger<InstitutionConfigurationHandlerBase> l, IIdEncoderService ids, IDeviceCommandSubmissionService commands, IEncryptionService encryption)
    : StaffMemberDeviceEnrollmentHandlerBase(u, c, l, ids, commands, encryption), IRequestHandler<GetStaffMemberDeviceEnrollmentByIdQuery, ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>>
{
    public async Task<ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>> Handle(GetStaffMemberDeviceEnrollmentByIdQuery request, CancellationToken ct) { var (context, id) = await EnrollmentContext(request.Id, ct); var e = await UnitOfWork.StaffMemberDeviceEnrollmentRepository.GetByIdAsync(context.InstitutionId, id, ct) ?? throw new NotFoundException(AppConstants.ErrorMessages.StaffMemberDeviceEnrollmentNotFound); await EnsureStaffMemberDataAccessAsync(context, e.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, ct); return ApiResponse<StaffMemberDeviceEnrollmentResponseDTO>.Success(ToResponse(m, e, context.Claims.InstitutionEncriptionKey)); }
}

public sealed class GetStaffMemberDeviceEnrollmentsQueryHandler(IUnitOfWork u, IMapper m, ICommonRequestService c, ILogger<InstitutionConfigurationHandlerBase> l, IIdEncoderService ids, IDeviceCommandSubmissionService commands, IEncryptionService encryption)
    : StaffMemberDeviceEnrollmentHandlerBase(u, c, l, ids, commands, encryption), IRequestHandler<GetStaffMemberDeviceEnrollmentsQuery, ApiResponse<List<StaffMemberDeviceEnrollmentResponseDTO>>>
{
    public async Task<ApiResponse<List<StaffMemberDeviceEnrollmentResponseDTO>>> Handle(GetStaffMemberDeviceEnrollmentsQuery request, CancellationToken ct)
    {
        var filter = request.Filter ?? new(); var context = await ValidateInstitutionDataAccessContextAsync();
        if (!string.IsNullOrWhiteSpace(filter.StaffMemberId)) { filter.ResolvedStaffMemberId = Decode(filter.StaffMemberId, context.Claims.InstitutionEncriptionKey, "StaffMemberId"); await EnsureStaffMemberDataAccessAsync(context, filter.ResolvedStaffMemberId.Value, StaffMemberDataAccessRequirement.PersonalDetails, ct); }
        if (!string.IsNullOrWhiteSpace(filter.InstitutionDeviceId)) filter.ResolvedInstitutionDeviceId = Decode(filter.InstitutionDeviceId, context.Claims.InstitutionEncriptionKey, "InstitutionDeviceId");
        var page = await UnitOfWork.StaffMemberDeviceEnrollmentRepository.GetPagedAsync(context.InstitutionId, filter, context.LoggedInStaffMemberId, context.RoleTypeId, ct); return Paged(page.Data.Select(e => ToResponse(m, e, context.Claims.InstitutionEncriptionKey)).ToList(), page.PageNumber, page.PageSize, page.TotalCount, "StaffMember device enrollments retrieved successfully.");
    }
}
