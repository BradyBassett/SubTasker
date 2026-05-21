using SubTaskerBackend.DTOs.Categories;
using SubTaskerBackend.Models;

namespace SubTaskerBackend.Interfaces
{
    public interface ICategoryService
    {
        Task<List<Category>> GetAllCategoriesAsync();

        Task<Category> GetCategoryByIdAsync(int id);

        Task<Category> CreateCategoryAsync(CategoryWriteDto categoryCreateDto);

        Task<Category> UpdateCategoryAsync(int id, CategoryWriteDto categoryUpdateDto);

        Task DeleteCategoryAsync(int id);
    }
}