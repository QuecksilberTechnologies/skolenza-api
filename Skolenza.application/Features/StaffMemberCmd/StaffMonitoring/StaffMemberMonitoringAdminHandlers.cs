using System.Security.Cryptography;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOS.StaffMonitoring;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;

namespace skolenza.application.Features.StaffMemberCmd.StaffMemberMonitoring;

#region Commands and queries

public sealed record UpsertStaffMemberMonitoringPolicyCommand(UpsertStaffMemberMonitoringPolicyRequestDTO DTO)
    : IRequest<ApiResponse<StaffMemberMonitoringPolicyResponseDTO>>;

public sealed record RegisterStaffMemberMonitoringAgentCommand(RegisterStaffMemberMonitoringAgentRequestDTO DTO)
    : IRequest<ApiResponse<StaffMemberMonitoringAgentRegistrationResponseDTO>>;

public sealed record GetStaffMemberMonitoringAgentsQuery(StaffMemberMonitoringPermissionRequestDTO PermissionRequest)
    : IRequest<ApiResponse<List<StaffMemberMonitoringAgentStatusResponseDTO>>>;

#endregion

#region Handlers

public sealed class UpsertStaffMemberMonitoringPolicyCommandHandler(
    IStaffMemberMonitoringRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<UpsertStaffMemberMonitoringPolicyCommand, ApiResponse<StaffMemberMonitoringPolicyResponseDTO>>
{
    public async Task<ApiResponse<StaffMemberMonitoringPolicyResponseDTO>> Handle(
        UpsertStaffMemberMonitoringPolicyCommand request,
        CancellationToken cancellationToken)
    {
        ValidatePolicy(request.DTO);
        var context = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!context.Success)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        var policy = await repository.GetPolicyAsync(context.InstitutionId, cancellationToken);
        if (policy is null)
        {
            policy = new StaffMemberMonitoringPolicy
            {
                InstitutionId = context.InstitutionId,
                IsActive = true,
                IsSoftDeleted = false,
                AddedById = context.LoggedInStaffMemberId,
                AddedDateTime = DateTime.UtcNow
            };
            await repository.AddPolicyAsync(policy, cancellationToken);
        }
        else
        {
            policy.UpdatedById = context.LoggedInStaffMemberId;
            policy.UpdatedDateTime = DateTime.UtcNow;
        }

        ApplyPolicy(policy, request.DTO);
        await repository.SaveChangesAsync(cancellationToken);
        return ApiResponse<StaffMemberMonitoringPolicyResponseDTO>.Success(ToResponse(policy), "StaffMember monitoring policy saved successfully.");
    }

    private static void ValidatePolicy(UpsertStaffMemberMonitoringPolicyRequestDTO dto)
    {
        if (dto.MinimumCaptureIntervalSeconds < 60 || dto.MaximumCaptureIntervalSeconds < dto.MinimumCaptureIntervalSeconds)
            throw new ValidationErrorException("The capture interval range is invalid.");
        if (dto.HeartbeatIntervalSeconds < 15 || dto.DelayedAfterSeconds <= dto.HeartbeatIntervalSeconds || dto.UnreachableAfterSeconds <= dto.DelayedAfterSeconds)
            throw new ValidationErrorException("The heartbeat status thresholds are invalid.");
        if (dto.OfflineRetentionDays < 1 || dto.MaximumOfflineBytes < 1 || dto.ImageQuality is < 1 or > 100)
            throw new ValidationErrorException("The offline retention, storage limit, or image quality is invalid.");
    }

    private static void ApplyPolicy(StaffMemberMonitoringPolicy policy, UpsertStaffMemberMonitoringPolicyRequestDTO dto)
    {
        policy.MinimumCaptureIntervalSeconds = dto.MinimumCaptureIntervalSeconds;
        policy.MaximumCaptureIntervalSeconds = dto.MaximumCaptureIntervalSeconds;
        policy.HeartbeatIntervalSeconds = dto.HeartbeatIntervalSeconds;
        policy.DelayedAfterSeconds = dto.DelayedAfterSeconds;
        policy.UnreachableAfterSeconds = dto.UnreachableAfterSeconds;
        policy.OfflineRetentionDays = dto.OfflineRetentionDays;
        policy.MaximumOfflineBytes = dto.MaximumOfflineBytes;
        policy.ImageQuality = dto.ImageQuality;
        policy.CaptureAllMonitors = dto.CaptureAllMonitors;
    }

    internal static StaffMemberMonitoringPolicyResponseDTO ToResponse(StaffMemberMonitoringPolicy policy) => new(
        policy.MinimumCaptureIntervalSeconds,
        policy.MaximumCaptureIntervalSeconds,
        policy.HeartbeatIntervalSeconds,
        policy.DelayedAfterSeconds,
        policy.UnreachableAfterSeconds,
        policy.OfflineRetentionDays,
        policy.MaximumOfflineBytes,
        policy.ImageQuality,
        policy.CaptureAllMonitors);
}

public sealed class RegisterStaffMemberMonitoringAgentCommandHandler(
    IStaffMemberMonitoringRepository repository,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService)
    : IRequestHandler<RegisterStaffMemberMonitoringAgentCommand, ApiResponse<StaffMemberMonitoringAgentRegistrationResponseDTO>>
{
    public async Task<ApiResponse<StaffMemberMonitoringAgentRegistrationResponseDTO>> Handle(
        RegisterStaffMemberMonitoringAgentCommand request,
        CancellationToken cancellationToken)
    {
        var context = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!context.Success)
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        if (string.IsNullOrWhiteSpace(request.DTO.DeviceName) || request.DTO.DeviceName.Length > 200)
            throw new ValidationErrorException("A valid device name is required.");

        long staffMemberId;
        try
        {
            staffMemberId = skolenza.application.Common.Helpers.RequestHelper.RequestCommonHelper.DecodeOnlyStaffMemberId(
                request.DTO.StaffMemberId,
                context.Claims.InstitutionEncriptionKey,
                idEncoderService);
        }
        catch
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        if (!await repository.StaffMemberExistsAsync(context.InstitutionId, staffMemberId, cancellationToken))
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);

        var policy = await repository.GetPolicyAsync(context.InstitutionId, cancellationToken)
            ?? throw new ValidationErrorException("Configure the staffMember monitoring policy before registering an agent.");
        var credentialBytes = RandomNumberGenerator.GetBytes(32);
        var credential = Convert.ToBase64String(credentialBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var agent = new skolenza.domain.Entity.StaffMemberMonitoringAgent
        {
            AgentInstanceId = Guid.NewGuid(),
            InstitutionId = context.InstitutionId,
            StaffMemberId = staffMemberId,
            PolicyId = policy.Id,
            DeviceName = request.DTO.DeviceName.Trim(),
            CredentialHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(credential))),
            IsActive = true,
            IsSoftDeleted = false,
            AddedById = context.LoggedInStaffMemberId,
            AddedDateTime = DateTime.UtcNow
        };
        await repository.AddAgentAsync(agent, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        var response = new StaffMemberMonitoringAgentRegistrationResponseDTO(
            agent.AgentInstanceId,
            credential,
            UpsertStaffMemberMonitoringPolicyCommandHandler.ToResponse(policy));
        return ApiResponse<StaffMemberMonitoringAgentRegistrationResponseDTO>.Success(response, "StaffMember monitoring agent registered. Store the credential now; it is not returned again.");
    }
}

public sealed class GetStaffMemberMonitoringAgentsQueryHandler(
    IStaffMemberMonitoringRepository repository,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService)
    : IRequestHandler<GetStaffMemberMonitoringAgentsQuery, ApiResponse<List<StaffMemberMonitoringAgentStatusResponseDTO>>>
{
    public async Task<ApiResponse<List<StaffMemberMonitoringAgentStatusResponseDTO>>> Handle(
        GetStaffMemberMonitoringAgentsQuery request,
        CancellationToken cancellationToken)
    {
        var context = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!context.Success)
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        var policy = await repository.GetPolicyAsync(context.InstitutionId, cancellationToken)
            ?? throw new NotFoundException("StaffMember monitoring policy was not found.");
        var now = DateTime.UtcNow;
        var agents = await repository.GetAgentsAsync(context.InstitutionId, cancellationToken);
        var response = agents.Select(agent => new StaffMemberMonitoringAgentStatusResponseDTO(
            agent.AgentInstanceId,
            idEncoderService.EncodeId_long(agent.StaffMemberId, context.Claims.InstitutionEncriptionKey),
            agent.DeviceName,
            ResolveStatus(agent, policy, now),
            agent.LastHeartbeatDateTime,
            agent.LastCaptureDateTime,
            agent.LastSuccessfulUploadDateTime,
            agent.PendingCaptureCount,
            agent.LastErrorCode,
            agent.AgentVersion)).ToList();
        return ApiResponse<List<StaffMemberMonitoringAgentStatusResponseDTO>>.Success(response);
    }

    private static string ResolveStatus(skolenza.domain.Entity.StaffMemberMonitoringAgent agent, StaffMemberMonitoringPolicy policy, DateTime now)
    {
        if (!agent.IsActive || agent.IsSoftDeleted) return StaffMemberMonitoringConnectionStatus.Inactive.ToString();
        if (!agent.LastHeartbeatDateTime.HasValue) return StaffMemberMonitoringConnectionStatus.NeverConnected.ToString();
        var elapsed = (now - agent.LastHeartbeatDateTime.Value).TotalSeconds;
        if (elapsed >= policy.UnreachableAfterSeconds) return StaffMemberMonitoringConnectionStatus.Unreachable.ToString();
        if (elapsed >= policy.DelayedAfterSeconds) return StaffMemberMonitoringConnectionStatus.Delayed.ToString();
        if (!string.IsNullOrWhiteSpace(agent.LastErrorCode)) return StaffMemberMonitoringConnectionStatus.UploadFailure.ToString();
        return StaffMemberMonitoringConnectionStatus.Online.ToString();
    }
}

#endregion
