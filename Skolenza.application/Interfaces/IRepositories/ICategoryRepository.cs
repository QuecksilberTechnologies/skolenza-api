using skolenza.application.DTOs.Category;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Interfaces.IRepositories
{
    public interface ICategoryRepository
    {
        // Create: Add a new category
        Task<CategoryResponseDTO> AddCategoryAsync(CategoryRequestDTO category);

        // Read: Get all categories
        Task<IEnumerable<Category>> GetAllMainCategoriesAsync();
        Task<IEnumerable<CategoryResponseDTO>> GetAllCategoriesAsync();

        // Read: Get a category by ID
        Task<CategoryResponseDTO> GetCategoryByIdAsync(int id);

        // Read: Get a category by ID
        Task<IEnumerable<Category>> GetAllChildCategoryByIdAsync(long requestId, int categoryParentId );


        // Update: Update an existing category
        Task<CategoryResponseDTO> UpdateCategoryAsync(Category category);

        // Delete: Delete a category by ID
        Task<bool> DeleteCategoryAsync(int id);
        Task<IEnumerable<CategoryResponseDTO>> GetAllMainCategoriesAsync(CategoryResponseDTO categoryRequestDTO);
    }

}
