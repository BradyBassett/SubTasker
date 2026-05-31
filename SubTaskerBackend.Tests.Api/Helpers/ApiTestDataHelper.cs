using Microsoft.AspNetCore.Identity;
using SubTaskerBackend.Models;
using SubTaskerBackend.Tests.Api.Fixtures;

namespace SubTaskerBackend.Tests.Api.Helpers
{
    public static class ApiTestDataHelper
    {
        public static async Task<User> SeedTestUserAsync(string username, string email, string password, ApiTestFactory factory)
        {
            var dbContext = factory.CreateDbContext();
            await using var _ = dbContext;

            User user = new User
            {
                Username = username,
                Email = email,
            };

            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);

            dbContext.Users.Add(user);

            await dbContext.SaveChangesAsync();

            return user;
        }

        public static async Task<User> SeedAuthenticatedUserAsync(string username, string email, string password, ApiTestFactory factory)
        {
            return await SeedTestUserAsync(username, email, password, factory);
        }
    }
}