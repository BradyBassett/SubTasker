using SubTaskerBackend.DTOs;
using SubTaskerBackend.Models;

namespace SubTaskerBackend.Mappers
{
	public static class TagsMapper
	{
		public static ReadTagsDto ToDto(this Tags tag)
		{
			return new ReadTagsDto
			{
				Id = tag.Id,
				Name = tag.Name,
				UserId = tag.UserId,
				CreatedAt = tag.CreatedAt,
				UpdatedAt = tag.UpdatedAt
			};
		}

		public static IReadOnlyCollection<ReadTagsDto> ToDtoList(this IEnumerable<Tags> tags)
		{
			return tags.Select(tag => tag.ToDto()).ToList();
		}
	}
}