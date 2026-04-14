using SubTaskerBackend.Models;
using SubTaskerBackend.DTOs;

namespace SubTaskerBackend.Mappers
{
	public static class CategoriesMapper
	{
		public static CategoryReadDto ToDto(this Categories category)
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

		public static IReadOnlyCollection<CategoryReadDto> ToDtoList(this IEnumerable<Categories> categories)
		{
			return categories.Select(category => category.ToDto()).ToList();
		}
	}
}