using SubTaskerBackend.DTOs;
using SubTaskerBackend.Models;

namespace SubTaskerBackend.Mappers
{
	public static class CategoriesMapper
	{
		public static CategoriesDto ToDto(this Categories category)
		{
			return new CategoriesDto
			{
				Id = category.Id,
				Name = category.Name,
				UserId = category.UserId,
				CreatedAt = category.CreatedAt,
				UpdatedAt = category.UpdatedAt
			};
		}

		public static IReadOnlyCollection<CategoriesDto> ToDtoList(this IEnumerable<Categories> categories)
		{
			return categories.Select(category => category.ToDto()).ToList();
		}
	}
}