using SubTaskerBackend.Models;
using SubTaskerBackend.DTOs.Categories;

namespace SubTaskerBackend.Mappers
{
	public static class CategoryMapper
	{
		public static CategoryReadDto ToDto(this Category category)
		{
			return new CategoryReadDto
			{
				Id = category.Id,
				Name = category.Name,
				UserId = category.UserId,
				CreatedAt = category.CreatedAt,
				UpdatedAt = category.UpdatedAt
			};
		}

		public static IReadOnlyCollection<CategoryReadDto> ToDtoList(this IEnumerable<Category> categories)
		{
			return categories.Select(category => category.ToDto()).ToList();
		}
	}
}