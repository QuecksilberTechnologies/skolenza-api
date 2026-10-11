using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Interfaces.IRepositories
{
     public interface IClientRepository
    {
        // Create a new role
        Task<List<ClientType>> CreateClientTypeAsync(ClientType leaveType);

        // Get a role by its Id
        Task<ClientType> GetClientTypeByIdAsync(int Id);

        // Get all roles
        Task<List<ClientType>> GetAllClientTypeAsync();

        // Update an existing role
        Task<List<ClientType>> UpdateClientTypeAsync(ClientType leaveType);

        // Delete a role by its Id
        Task<bool> DeleteClientTypeAsync(int Id);
    }
}
