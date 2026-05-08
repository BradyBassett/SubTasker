using SubTaskerBackend.DTOs.Users;
using SubTaskerBackend.Models;

namespace SubTaskerBackend.Interfaces
{
    public interface IAuthService
    {
        string CreateToken(User user);

        Task<User> RegisterAsync(UserCreateDto userCreateDto);

        Task<string> LoginAsync(UserLoginDto loginDto);
    }
}