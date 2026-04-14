using SubTaskerBackend.DTOs;
using SubTaskerBackend.Models;

namespace SubTaskerBackend.Mappers
{
	public static class UsersMapper
	{
		public static ReadUsersDto ToDto(this Users user)
		{
			return new ReadUsersDto
			{
				Id = user.Id,
				Username = user.Username,
				Email = user.Email,
				CreatedAt = user.CreatedAt,
				UpdatedAt = user.UpdatedAt
			};
		}

		public static IReadOnlyCollection<ReadUsersDto> ToDtoList(this IEnumerable<Users> users)
		{
			return users.Select(user => user.ToDto()).ToList();
		}
	}
}