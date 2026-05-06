using SubTaskerBackend.Data;
using SubTaskerBackend.Exceptions;
using SubTaskerBackend.Models;

namespace SubTaskerBackend.Services
{

    public class UserService
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
            string? userIdString = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                throw new UnauthorizedException("User is not authenticated.");
            }

            return await GetUserByIdAsync(userId);
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            User? user = await _dbContext.Users.FindAsync(id);

            if (user == null)
            {
                throw new NotFoundException("User not found.");
            }

            return user;
        }
    }
}