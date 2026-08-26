using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SubTaskerBackend.Data;
using SubTaskerBackend.DTOs.Tags;
using SubTaskerBackend.Exceptions;
using SubTaskerBackend.Models;
using SubTaskerBackend.Services;
using SubTaskerBackend.Tests.Integration.Fixtures;
using SubTaskerBackend.Tests.Integration.Helpers;

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
            User currentUser = await TestDataHelper.SeedTestUserAsync(_dbContext, "currentuser", "current@mail.com");
            User otherUser = await TestDataHelper.SeedTestUserAsync(_dbContext, "otheruser", "other@mail.com");

            await TestDataHelper.SeedTagAsync(_dbContext, currentUser.Id, "Urgent");
            await TestDataHelper.SeedTagAsync(_dbContext, currentUser.Id, "Backend");
            await TestDataHelper.SeedTagAsync(_dbContext, otherUser.Id, "Frontend");

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, currentUser.Id);

            List<Tag> result = await _tagService.GetAllTagsAsync();

            Assert.Equal(2, result.Count);
            Assert.All(result, tag => Assert.Equal(currentUser.Id, tag.UserId));
            Assert.Equal(new[] { "Backend", "Urgent" }, result.Select(tag => tag.Name));
        }

        [Fact]
        public async Task GetTagByIdAsync_WithOwnedTag_ReturnsTag()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);
            Tag tag = await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Urgent");
            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            Tag result = await _tagService.GetTagByIdAsync(tag.Id);

            Assert.Equal(tag.Id, result.Id);
            Assert.Equal("Urgent", result.Name);
            Assert.Equal(user.Id, result.UserId);
        }

        [Fact]
        public async Task GetTagByIdAsync_WithDifferentUsersTag_ThrowsNotFoundException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext, "currentuser", "current@mail.com");
            User otherUser = await TestDataHelper.SeedTestUserAsync(_dbContext, "otheruser", "other@mail.com");
            Tag tag = await TestDataHelper.SeedTagAsync(_dbContext, otherUser.Id, "Private");
            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            await Assert.ThrowsAsync<NotFoundException>(() => _tagService.GetTagByIdAsync(tag.Id));
        }

        [Fact]
        public async Task CreateTagAsync_WithValidDto_CreatesTagForCurrentUser()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);
            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

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
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);
            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            await Assert.ThrowsAsync<BadRequestException>(() => _tagService.CreateTagAsync(new TagWriteDto { Name = "   " }));
        }

        [Fact]
        public async Task CreateTagAsync_WithDuplicateNameForSameUser_ThrowsConflictException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);
            await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Urgent");
            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            await Assert.ThrowsAsync<ConflictException>(() => _tagService.CreateTagAsync(new TagWriteDto { Name = "Urgent" }));
        }

        [Fact]
        public async Task CreateTagAsync_WithDuplicateNameForDifferentUser_CreatesTag()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext, "currentuser", "current@mail.com");
            User otherUser = await TestDataHelper.SeedTestUserAsync(_dbContext, "otheruser", "other@mail.com");
            await TestDataHelper.SeedTagAsync(_dbContext, otherUser.Id, "Urgent");
            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            Tag result = await _tagService.CreateTagAsync(new TagWriteDto { Name = "Urgent" });

            Assert.Equal("Urgent", result.Name);
            Assert.Equal(user.Id, result.UserId);
        }

        [Fact]
        public async Task UpdateTagAsync_WithValidDto_UpdatesTag()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);
            Tag tag = await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Old Name");
            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

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
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);
            Tag tag = await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Urgent");
            await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Backend");
            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            await Assert.ThrowsAsync<ConflictException>(() => _tagService.UpdateTagAsync(tag.Id, new TagWriteDto { Name = "Backend" }));
        }

        [Fact]
        public async Task DeleteTagAsync_WithTagAssignedToTask_DeletesTagAndKeepsTask()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);
            Tag tag = await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Urgent");
            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Task with tag");

            taskItem.Tags.Add(tag);
            await _dbContext.SaveChangesAsync();

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

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
    }
}