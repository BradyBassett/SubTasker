using SubTaskerBackend.Data;
using SubTaskerBackend.DTOs.Categories;
using SubTaskerBackend.Interfaces;
using SubTaskerBackend.Models;
using SubTaskerBackend.Utilities;
using Microsoft.EntityFrameworkCore;
using SubTaskerBackend.Exceptions;

namespace SubTaskerBackend.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly SubTaskerEfCoreDbContext _dbContext;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public CategoryService(SubTaskerEfCoreDbContext dbContext, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            int userId = ClaimHelper.GetUserIdFromClaims(_httpContextAccessor);

            List<Category> categories = await _dbContext.Categories
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return categories;
        }

        public async Task<Category> GetCategoryByIdAsync(int id)
        {
            int userId = ClaimHelper.GetUserIdFromClaims(_httpContextAccessor);

            Category? category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (category == null)
            {
                throw new NotFoundException("Category not found.");
            }

            return category;
        }

        public async Task<Category> CreateCategoryAsync(CategoryWriteDto categoryCreateDto)
        {
            if (string.IsNullOrWhiteSpace(categoryCreateDto.Name))
            {
                throw new BadRequestException("Category name is required.");
            }

            int userId = ClaimHelper.GetUserIdFromClaims(_httpContextAccessor);

            Category category = new Category
            {
                Name = categoryCreateDto.Name.Trim(),
                UserId = userId
            };

            if (await _dbContext.Categories.AnyAsync(c => c.Name == category.Name && c.UserId == userId))
            {
                throw new ConflictException("A category with the same name already exists.");
            }

            _dbContext.Categories.Add(category);
            await _dbContext.SaveChangesAsync();

            return category;
        }

        public async Task<Category> UpdateCategoryAsync(int id, CategoryWriteDto categoryUpdateDto)
        {
            if (string.IsNullOrWhiteSpace(categoryUpdateDto.Name))
            {
                throw new BadRequestException("Category name is required.");
            }

            int userId = ClaimHelper.GetUserIdFromClaims(_httpContextAccessor);

            Category? category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (category == null)
            {
                throw new NotFoundException("Category not found.");
            }

            category.Name = categoryUpdateDto.Name.Trim();

            if (await _dbContext.Categories.AnyAsync(c => c.Name == category.Name && c.UserId == userId && c.Id != id))
            {
                throw new ConflictException("A category with the same name already exists.");
            }

            _dbContext.Categories.Update(category);
            await _dbContext.SaveChangesAsync();

            return category;
        }

        public async Task DeleteCategoryAsync(int id)
        {
            int userId = ClaimHelper.GetUserIdFromClaims(_httpContextAccessor);

            Category? category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (category == null)
            {
                throw new NotFoundException("Category not found.");
            }

            _dbContext.Categories.Remove(category);
            await _dbContext.SaveChangesAsync();
        }
    }
}