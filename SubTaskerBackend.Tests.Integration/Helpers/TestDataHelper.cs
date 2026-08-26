using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using SubTaskerBackend.Models;
using SubTaskerBackend.Data;

namespace SubTaskerBackend.Tests.Integration.Helpers
{
    public static class TestDataHelper
    {
        public static void SetHttpContextUser(IHttpContextAccessor httpContextAccessor,int userId)
        {
            DefaultHttpContext httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                })
            );

            httpContextAccessor.HttpContext = httpContext;
        }

        public static async Task<User> SeedTestUserAsync(SubTaskerEfCoreDbContext dbContext,string username = "testuser", string email = "testuser@mail.com")
        {
            User user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = "somehash"
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            return user;
        }

        public static async Task<Tag> SeedTagAsync(SubTaskerEfCoreDbContext dbContext, int userId, string name)
        {
            Tag tag = new Tag
            {
                Name = name,
                UserId = userId
            };

            dbContext.Tags.Add(tag);
            await dbContext.SaveChangesAsync();

            return tag;
        }

        public static async Task<Category> SeedCategoryAsync(SubTaskerEfCoreDbContext dbContext, int userId, string name)
        {
            Category category = new Category
            {
                Name = name,
                UserId = userId
            };

            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();

            return category;
        }

        public static async Task<TaskItem> SeedTaskItemAsync(SubTaskerEfCoreDbContext dbContext, int userId, string title)
        {
            TaskItem taskItem = new TaskItem
            {
                Title = title,
                UserId = userId
            };

            dbContext.TaskItems.Add(taskItem);
            await dbContext.SaveChangesAsync();

            return taskItem;
        }

        public static async Task<TaskItem> SeedSubTaskItemAsync(SubTaskerEfCoreDbContext dbContext, int userId, string title, int parentTaskItemId)
        {
            TaskItem subTaskItem = new TaskItem
            {
                Title = title,
                UserId = userId,
                ParentTaskId = parentTaskItemId
            };

            dbContext.TaskItems.Add(subTaskItem);
            await dbContext.SaveChangesAsync();

            return subTaskItem;
        }
    }
}