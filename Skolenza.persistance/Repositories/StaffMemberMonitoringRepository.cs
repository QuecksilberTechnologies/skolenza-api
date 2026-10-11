using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Repositories;

/// <summary>Provides institution-scoped persistence for staffMember monitoring agents and captures.</summary>
public sealed class StaffMemberMonitoringRepository(SkolenzaDbContext context) : IStaffMemberMonitoringRepository
{
    public Task<StaffMemberMonitoringPolicy?> GetPolicyAsync(long institutionId, CancellationToken cancellationToken) =>
        context.StaffMemberMonitoringPolicies
            .SingleOrDefaultAsync(x => x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted, cancellationToken);

    public Task<StaffMemberMonitoringAgent?> GetAgentByCredentialHashAsync(string credentialHash, CancellationToken cancellationToken) =>
        context.StaffMemberMonitoringAgents
            .Include(x => x.Policy)
            .SingleOrDefaultAsync(x => x.CredentialHash == credentialHash && x.IsActive && !x.IsSoftDeleted, cancellationToken);

    public Task<StaffMemberMonitoringAgent?> GetAgentAsync(long institutionId, Guid agentInstanceId, CancellationToken cancellationToken) =>
        context.StaffMemberMonitoringAgents
            .Include(x => x.Policy)
            .SingleOrDefaultAsync(x => x.InstitutionId == institutionId && x.AgentInstanceId == agentInstanceId && !x.IsSoftDeleted, cancellationToken);

    public Task<bool> StaffMemberExistsAsync(long institutionId, long staffMemberId, CancellationToken cancellationToken) =>
        context.StaffMembers.AnyAsync(x => x.InstitutionId == institutionId && x.Id == staffMemberId && !x.IsSoftDeleted, cancellationToken);

    public Task<bool> CaptureExistsAsync(long monitoringAgentId, Guid captureId, CancellationToken cancellationToken) =>
        context.StaffMemberScreenCaptures.AnyAsync(x => x.MonitoringAgentId == monitoringAgentId && x.CaptureId == captureId, cancellationToken);

    public Task AddPolicyAsync(StaffMemberMonitoringPolicy policy, CancellationToken cancellationToken) =>
        context.StaffMemberMonitoringPolicies.AddAsync(policy, cancellationToken).AsTask();

    public Task AddAgentAsync(StaffMemberMonitoringAgent agent, CancellationToken cancellationToken) =>
        context.StaffMemberMonitoringAgents.AddAsync(agent, cancellationToken).AsTask();

    public Task AddCaptureAsync(StaffMemberScreenCapture capture, CancellationToken cancellationToken) =>
        context.StaffMemberScreenCaptures.AddAsync(capture, cancellationToken).AsTask();

    public Task<List<StaffMemberMonitoringAgent>> GetAgentsAsync(long institutionId, CancellationToken cancellationToken) =>
        context.StaffMemberMonitoringAgents
            .AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && !x.IsSoftDeleted)
            .OrderBy(x => x.StaffMemberId)
            .ThenBy(x => x.DeviceName)
            .ToListAsync(cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
