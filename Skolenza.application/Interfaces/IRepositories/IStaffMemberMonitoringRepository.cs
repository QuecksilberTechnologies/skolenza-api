using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.IRepositories;

public interface IStaffMemberMonitoringRepository
{
    Task<StaffMemberMonitoringPolicy?> GetPolicyAsync(long institutionId, CancellationToken cancellationToken);
    Task<StaffMemberMonitoringAgent?> GetAgentByCredentialHashAsync(string credentialHash, CancellationToken cancellationToken);
    Task<StaffMemberMonitoringAgent?> GetAgentAsync(long institutionId, Guid agentInstanceId, CancellationToken cancellationToken);
    Task<bool> StaffMemberExistsAsync(long institutionId, long staffMemberId, CancellationToken cancellationToken);
    Task<bool> CaptureExistsAsync(long monitoringAgentId, Guid captureId, CancellationToken cancellationToken);
    Task AddPolicyAsync(StaffMemberMonitoringPolicy policy, CancellationToken cancellationToken);
    Task AddAgentAsync(StaffMemberMonitoringAgent agent, CancellationToken cancellationToken);
    Task AddCaptureAsync(StaffMemberScreenCapture capture, CancellationToken cancellationToken);
    Task<List<StaffMemberMonitoringAgent>> GetAgentsAsync(long institutionId, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
