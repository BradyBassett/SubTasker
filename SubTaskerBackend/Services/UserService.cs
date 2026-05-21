using SubTaskerBackend.Data;
using SubTaskerBackend.Exceptions;
using SubTaskerBackend.Interfaces;
using SubTaskerBackend.Models;
using SubTaskerBackend.Utilities;

namespace SubTaskerBackend.Services
{

    public class UserService : IUserService
    {
        private readonly SubTaskerEfCoreDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(SubTaskerEfCoreDbContext dbContext, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<User> GetCurrentUserAsync()
        {
            int userId = ClaimHelper.GetUserIdFromClaims(_httpContextAccessor);

            return await GetUserByIdAsync(userId);
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            // Ensure that the user can only access their own information
            int currentUserId = ClaimHelper.GetUserIdFromClaims(_httpContextAccessor);
            if (id != currentUserId)
            {
                throw new NotFoundException("User not found.");
            }

            User? user = await _dbContext.Users.FindAsync(id);

            if (user == null)
            {
                throw new NotFoundException("User not found.");
            }

            return user;
        }
    }
}