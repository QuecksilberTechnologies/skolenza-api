
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Interfaces.IRepositories
{
    public interface ITravelRepository
    {
        // Create a new role
        Task<List<TravelMode>> CreateTravelTypeAsync(TravelMode leaveType);

        // Get a Travel by its Id
        Task<TravelMode> GetClientTypeByIdAsync(int Id);

        // Get all roles
        Task<List<TravelMode>> GetAllTravelModeTypeAsync();

        // Update an existing role
        Task<List<TravelMode>> UpdateClientTypeAsync(TravelMode leaveType);

        // Delete a role by its Id
        Task<bool> DeleteClientTypeAsync(int Id);
    }
}
