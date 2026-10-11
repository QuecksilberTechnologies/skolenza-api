using System.Text.Json;
using skolenza.application.Constants;
using skolenza.application.DTOS.Device.Enroll;
using skolenza.application.Interfaces.IDeviceCommunication;
using skolenza.application.Interfaces.IRepositories;
using Microsoft.Extensions.Logging;

/// <summary>
/// Submits staffMember enrollment work to the durable MQTT command pipeline.
/// Device serial numbers supplied by older callers are intentionally ignored;
/// the pipeline resolves the authoritative serial from InstitutionDeviceId.
/// </summary>
public sealed class StaffMemberEnrollmentAtDeviceRepository(
    IDeviceCommandSubmissionService deviceCommandSubmissionService,
    ILogger<StaffMemberEnrollmentAtDeviceRepository> logger)
    : IStaffMemberEnrollmentAtDeviceRepository
{
    /// <inheritdoc />
    public async Task<bool> EnrollStaffMemberAsync(RegisterStaffMemberDTORequest dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var payload = JsonSerializer.Serialize(new
        {
            cmd = DeviceCommands.SetUserInfo,
            enrollid = dto.StaffMemberId,
            name = dto.Name,
            backupnum = 0,
            admin = 0,
            record = string.Empty
        });

        var result = await deviceCommandSubmissionService.SubmitAsync(
            new DeviceCommandSubmission(
                dto.InstitutionId,
                dto.DeviceId,
                DeviceCommands.SetUserInfo,
                payload,
                RequestedById: null));

        logger.LogInformation(
            "Queued staffMember enrollment command {DeviceCommandId} for InstitutionDevice {InstitutionDeviceId}.",
            result.DeviceCommandId,
            dto.DeviceId);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteStaffMemberAsync(DeleteStaffMemberDTORequest dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var payload = JsonSerializer.Serialize(new
        {
            cmd = DeviceCommands.DeleteUser,
            enrollid = dto.StaffMemberCode
        });

        var result = await deviceCommandSubmissionService.SubmitAsync(
            new DeviceCommandSubmission(
                dto.InstitutionId,
                dto.DeviceId,
                DeviceCommands.DeleteUser,
                payload,
                RequestedById: null));

        logger.LogInformation(
            "Queued staffMember delete command {DeviceCommandId} for InstitutionDevice {InstitutionDeviceId}.",
            result.DeviceCommandId,
            dto.DeviceId);
        return true;
    }
}
