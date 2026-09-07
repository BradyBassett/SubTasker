using Microsoft.AspNetCore.Identity;
using SubTaskerBackend.Data;
using SubTaskerBackend.DTOs.Users;
using SubTaskerBackend.Models;
using SubTaskerBackend.Tests.Api.Fixtures;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SubTaskerBackend.Tests.Api.Helpers
{
    public static class ApiTestDataHelper
    {
        public static async Task<User> SeedTestUserAsync(ApiTestFactory factory, string username, string email, string password)
        {
            var dbContext = factory.CreateDbContext();

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

        public static async Task DeleteTestUserAsync(ApiTestFactory factory, int userId)
        {
            var dbContext = factory.CreateDbContext();

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

        public static async Task<Tag> SeedTagAsync(ApiTestFactory factory, int userId, string name)
        {
            var dbContext = factory.CreateDbContext();

            Tag tag = new Tag
            {
                Name = name,
                UserId = userId
            };

            dbContext.Tags.Add(tag);
            await dbContext.SaveChangesAsync();

            return tag;
        }

        public static async Task<TaskItem> SeedTaskItemAsync(ApiTestFactory factory,int userId, string title, DateTime? dueDate = null)
        {
            var dbContext = factory.CreateDbContext();

            TaskItem taskItem = new TaskItem
            {
                Title = title,
                UserId = userId,
                DueDate = dueDate
            };

            dbContext.TaskItems.Add(taskItem);
            await dbContext.SaveChangesAsync();

            return taskItem;
        }
    }
}