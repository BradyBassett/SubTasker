using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SubTaskerBackend.Data;
using SubTaskerBackend.DTOs.Tags;
using SubTaskerBackend.Exceptions;
using SubTaskerBackend.Models;
using SubTaskerBackend.Services;
using SubTaskerBackend.Tests.Integration.Fixtures;

namespace SubTaskerBackend.Tests.Integration.Services
{
    public class TagServiceTests : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly PostgresFixture _postgresFixture;

        private SubTaskerEfCoreDbContext _dbContext = null!;
        private IHttpContextAccessor _httpContextAccessor = null!;
        private TagService _tagService = null!;

        public TagServiceTests(PostgresFixture postgresFixture)
        {
            _postgresFixture = postgresFixture;
        }

        public async Task InitializeAsync()
        {
            await _postgresFixture.ResetDatabaseAsync();

            _dbContext = _postgresFixture.CreateDbContext();
            _httpContextAccessor = new HttpContextAccessor();
            _tagService = new TagService(_dbContext, _httpContextAccessor);
        }

        public async Task DisposeAsync()
        {
            if (_dbContext is not null)
            {
                await _dbContext.DisposeAsync();
            }
        }

        [Fact]
        public async Task GetAllTagsAsync_WithMixedUsers_ReturnsOnlyCurrentUsersTagsOrderedByName()
        {
            User currentUser = await SeedTestUserAsync("currentuser", "current@mail.com");
            User otherUser = await SeedTestUserAsync("otheruser", "other@mail.com");

            await SeedTagAsync(currentUser.Id, "Urgent");
            await SeedTagAsync(currentUser.Id, "Backend");
            await SeedTagAsync(otherUser.Id, "Frontend");

            SetHttpContextUser(currentUser.Id);

            List<Tag> result = await _tagService.GetAllTagsAsync();

            Assert.Equal(2, result.Count);
            Assert.All(result, tag => Assert.Equal(currentUser.Id, tag.UserId));
            Assert.Equal(new[] { "Backend", "Urgent" }, result.Select(tag => tag.Name));
        }

        [Fact]
        public async Task GetTagByIdAsync_WithOwnedTag_ReturnsTag()
        {
            User user = await SeedTestUserAsync();
            Tag tag = await SeedTagAsync(user.Id, "Urgent");
            SetHttpContextUser(user.Id);

            Tag result = await _tagService.GetTagByIdAsync(tag.Id);

            Assert.Equal(tag.Id, result.Id);
            Assert.Equal("Urgent", result.Name);
            Assert.Equal(user.Id, result.UserId);
        }

        [Fact]
        public async Task GetTagByIdAsync_WithDifferentUsersTag_ThrowsNotFoundException()
        {
            User user = await SeedTestUserAsync("currentuser", "current@mail.com");
            User otherUser = await SeedTestUserAsync("otheruser", "other@mail.com");
            Tag tag = await SeedTagAsync(otherUser.Id, "Private");
            SetHttpContextUser(user.Id);

            await Assert.ThrowsAsync<NotFoundException>(() => _tagService.GetTagByIdAsync(tag.Id));
        }

        [Fact]
        public async Task CreateTagAsync_WithValidDto_CreatesTagForCurrentUser()
        {
            User user = await SeedTestUserAsync();
            SetHttpContextUser(user.Id);

            Tag result = await _tagService.CreateTagAsync(new TagWriteDto { Name = "  Urgent  " });

            Assert.NotEqual(0, result.Id);
            Assert.Equal("Urgent", result.Name);
            Assert.Equal(user.Id, result.UserId);

            await using SubTaskerEfCoreDbContext verifyContext = _postgresFixture.CreateDbContext();
            Tag? savedTag = await verifyContext.Tags.FindAsync(result.Id);

            Assert.NotNull(savedTag);
            Assert.Equal("Urgent", savedTag.Name);
            Assert.Equal(user.Id, savedTag.UserId);
        }

        [Fact]
        public async Task CreateTagAsync_WithWhitespaceName_ThrowsBadRequestException()
        {
            User user = await SeedTestUserAsync();
            SetHttpContextUser(user.Id);

            await Assert.ThrowsAsync<BadRequestException>(() => _tagService.CreateTagAsync(new TagWriteDto { Name = "   " }));
        }

        [Fact]
        public async Task CreateTagAsync_WithDuplicateNameForSameUser_ThrowsConflictException()
        {
            User user = await SeedTestUserAsync();
            await SeedTagAsync(user.Id, "Urgent");
            SetHttpContextUser(user.Id);

            await Assert.ThrowsAsync<ConflictException>(() => _tagService.CreateTagAsync(new TagWriteDto { Name = "Urgent" }));
        }

        [Fact]
        public async Task CreateTagAsync_WithDuplicateNameForDifferentUser_CreatesTag()
        {
            User user = await SeedTestUserAsync("currentuser", "current@mail.com");
            User otherUser = await SeedTestUserAsync("otheruser", "other@mail.com");
            await SeedTagAsync(otherUser.Id, "Urgent");
            SetHttpContextUser(user.Id);

            Tag result = await _tagService.CreateTagAsync(new TagWriteDto { Name = "Urgent" });

            Assert.Equal("Urgent", result.Name);
            Assert.Equal(user.Id, result.UserId);
        }

        [Fact]
        public async Task UpdateTagAsync_WithValidDto_UpdatesTag()
        {
            User user = await SeedTestUserAsync();
            Tag tag = await SeedTagAsync(user.Id, "Old Name");
            SetHttpContextUser(user.Id);

            Tag result = await _tagService.UpdateTagAsync(tag.Id, new TagWriteDto { Name = "  New Name  " });

            Assert.Equal(tag.Id, result.Id);
            Assert.Equal("New Name", result.Name);

            await using SubTaskerEfCoreDbContext verifyContext = _postgresFixture.CreateDbContext();
            Tag? updatedTag = await verifyContext.Tags.FindAsync(tag.Id);

            Assert.NotNull(updatedTag);
            Assert.Equal("New Name", updatedTag.Name);
        }

        [Fact]
        public async Task UpdateTagAsync_WithDuplicateNameForSameUser_ThrowsConflictException()
        {
            User user = await SeedTestUserAsync();
            Tag tag = await SeedTagAsync(user.Id, "Urgent");
            await SeedTagAsync(user.Id, "Backend");
            SetHttpContextUser(user.Id);

            await Assert.ThrowsAsync<ConflictException>(() => _tagService.UpdateTagAsync(tag.Id, new TagWriteDto { Name = "Backend" }));
        }

        [Fact]
        public async Task DeleteTagAsync_WithTagAssignedToTask_DeletesTagAndKeepsTask()
        {
            User user = await SeedTestUserAsync();
            Tag tag = await SeedTagAsync(user.Id, "Urgent");
            TaskItem taskItem = await SeedTaskItemAsync(user.Id, "Task with tag");

            taskItem.Tags.Add(tag);
            await _dbContext.SaveChangesAsync();

            SetHttpContextUser(user.Id);

            await _tagService.DeleteTagAsync(tag.Id);

            await using SubTaskerEfCoreDbContext verifyContext = _postgresFixture.CreateDbContext();
            Tag? deletedTag = await verifyContext.Tags.FindAsync(tag.Id);
            TaskItem? existingTask = await verifyContext.TaskItems
                .Include(t => t.Tags)
                .FirstOrDefaultAsync(t => t.Id == taskItem.Id);

            Assert.Null(deletedTag);
            Assert.NotNull(existingTask);
            Assert.Empty(existingTask.Tags);
        }

        private void SetHttpContextUser(int userId)
        {
            DefaultHttpContext httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                })
            );

            _httpContextAccessor.HttpContext = httpContext;
        }

        private async Task<User> SeedTestUserAsync(string username = "testuser", string email = "testuser@mail.com")
        {
            User user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = "somehash"
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            return user;
        }

        private async Task<Tag> SeedTagAsync(int userId, string name)
        {
            Tag tag = new Tag
            {
                Name = name,
                UserId = userId
            };

            _dbContext.Tags.Add(tag);
            await _dbContext.SaveChangesAsync();

            return tag;
        }

        private async Task<TaskItem> SeedTaskItemAsync(int userId, string title)
        {
            TaskItem taskItem = new TaskItem
            {
                Title = title,
                UserId = userId
            };

            _dbContext.TaskItems.Add(taskItem);
            await _dbContext.SaveChangesAsync();

            return taskItem;
        }
    }
}