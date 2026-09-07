using Microsoft.AspNetCore.Identity;
using SubTaskerBackend.DTOs.Users;
using SubTaskerBackend.Models;
using SubTaskerBackend.Tests.Api.Fixtures;
using System.Net.Http.Headers;
using System.Net.Http.Json;

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

        public static async Task DeleteTestUserAsync(int userId, ApiTestFactory factory)
        {
            var dbContext = factory.CreateDbContext();
            await using var _ = dbContext;

            User? user = await dbContext.Users.FindAsync(userId);
            if (user != null)
            {
                dbContext.Users.Remove(user);
                await dbContext.SaveChangesAsync();
            }
        }

        public static async Task AuthenticateClientAsync(HttpClient client, string email, string password)
        {
            HttpResponseMessage response = await client.PostAsJsonAsync(
                "/api/auth/login",
                new UserLoginDto
                {
                    Email = email,
                    Password = password
                });

            response.EnsureSuccessStatusCode();

            LoginResponseDto? login =
                await response.Content.ReadFromJsonAsync<LoginResponseDto>();

            Assert.NotNull(login);
            Assert.False(string.IsNullOrWhiteSpace(login.Token));

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", login.Token);
        }
    }
}