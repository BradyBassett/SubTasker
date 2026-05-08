using SubTaskerBackend.Models;

namespace SubTaskerBackend.Interfaces
{
    public interface IUserService
    {
        Task<User> GetCurrentUserAsync();

        Task<User> GetUserByIdAsync(int id);
    }
}