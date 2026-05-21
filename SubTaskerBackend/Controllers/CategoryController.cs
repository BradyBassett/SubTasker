using Microsoft.AspNetCore.Mvc;
using SubTaskerBackend.DTOs.Categories;
using SubTaskerBackend.Interfaces;
using SubTaskerBackend.Mappers;
using SubTaskerBackend.Models;

namespace SubTaskerBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCategories()
        {
            List<Category> categories = await _categoryService.GetAllCategoriesAsync();

            return Ok(categories.ToDtoList());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            Category category = await _categoryService.GetCategoryByIdAsync(id);

            return Ok(category.ToDto());
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryWriteDto categoryCreateDto)
        {
            Category category = await _categoryService.CreateCategoryAsync(categoryCreateDto);

            return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, category.ToDto());
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryWriteDto categoryUpdateDto)
        {
            Category category = await _categoryService.UpdateCategoryAsync(id, categoryUpdateDto);

            return Ok(category.ToDto());
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            await _categoryService.DeleteCategoryAsync(id);

            return NoContent();
        }
    }
}