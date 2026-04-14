using SubTaskerBackend.DTOs.Users;
using SubTaskerBackend.Models;

namespace SubTaskerBackend.Mappers
{
	public static class UserMapper
	{
		public static UserReadDto ToDto(this User user)
		{
			return new UserReadDto
			{
				Id = user.Id,
				Username = user.Username,
				Email = user.Email,
				CreatedAt = user.CreatedAt,
				UpdatedAt = user.UpdatedAt
			};
		}

		public static IReadOnlyCollection<UserReadDto> ToDtoList(this IEnumerable<User> users)
		{
			return users.Select(user => user.ToDto()).ToList();
		}
	}
}