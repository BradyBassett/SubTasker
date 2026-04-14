using SubTaskerBackend.DTOs.Tags;
using SubTaskerBackend.Models;

namespace SubTaskerBackend.Mappers
{
	public static class TagMapper
	{
		public static TagReadDto ToDto(this Tag tag)
		{
			return new TagReadDto
			{
				Id = tag.Id,
				Name = tag.Name,
				UserId = tag.UserId,
				CreatedAt = tag.CreatedAt,
				UpdatedAt = tag.UpdatedAt
			};
		}

		public static IReadOnlyCollection<TagReadDto> ToDtoList(this IEnumerable<Tag> tags)
		{
			return tags.Select(tag => tag.ToDto()).ToList();
		}
	}
}