using skolenza.application.DTOs.Category;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Interfaces.IRepositories
{
    public interface ITenderCategoryRespository
     
    {
        // Create: Add a new category
        Task<TenderCategoryResponseDTO> AddTenderCategoryAsync(TenderCategoryRequestDTO category);

        // Read: Get all categories
        Task<IEnumerable<TenderCategoryResponseDTO>> GetAllTenderMainCategoriesAsync();
        Task<IEnumerable<TenderCategoryResponseDTO>> GetAllTenderCategoriesAsync();

        // Read: Get a category by ID
        Task<TenderCategoryResponseDTO> GetTenderCategoryByIdAsync(int id);

        // Read: Get a category by ID
        Task<IEnumerable<Category>> GetAllTenderChildCategoryByIdAsync(long requestId, int categoryParentId);


        // Update: Update an existing category
        Task<TenderCategoryResponseDTO> UpdateTenderCategoryAsync(Category category);

        // Delete: Delete a category by ID
        Task<bool> DeleteCategoryAsync(int id);
        Task<IEnumerable<TenderCategoryResponseDTO>> GetAllTenderMainCategoriesAsync(TenderCategoryResponseDTO categoryRequestDTO);
    }
}
