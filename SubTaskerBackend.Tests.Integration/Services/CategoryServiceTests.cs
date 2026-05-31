using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SubTaskerBackend.Data;
using SubTaskerBackend.DTOs.Categories;
using SubTaskerBackend.Exceptions;
using SubTaskerBackend.Models;
using SubTaskerBackend.Services;
using SubTaskerBackend.Tests.Integration.Fixtures;

namespace SubTaskerBackend.Tests.Integration.Services
{
    public class CategoryServiceTests : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly PostgresFixture _postgresFixture;

        private SubTaskerEfCoreDbContext _dbContext = null!;
        private IHttpContextAccessor _httpContextAccessor = null!;
        private CategoryService _categoryService = null!;

        public CategoryServiceTests(PostgresFixture postgresFixture)
        {
            _postgresFixture = postgresFixture;
        }

        public async Task InitializeAsync()
        {
            await _postgresFixture.ResetDatabaseAsync();

            _dbContext = _postgresFixture.CreateDbContext();
            _httpContextAccessor = new HttpContextAccessor();
            _categoryService = new CategoryService(_dbContext, _httpContextAccessor);
        }

        public async Task DisposeAsync()
        {
            if (_dbContext is not null)
            {
                await _dbContext.DisposeAsync();
            }
        }

        [Fact]
        public async Task GetAllCategoriesAsync_WithMixedUsers_ReturnsOnlyCurrentUsersCategoriesOrderedByName()
        {
            User currentUser = await SeedTestUserAsync("currentuser", "current@mail.com");
            User otherUser = await SeedTestUserAsync("otheruser", "other@mail.com");

            await SeedCategoryAsync(currentUser.Id, "Work");
            await SeedCategoryAsync(currentUser.Id, "Admin");
            await SeedCategoryAsync(otherUser.Id, "Personal");

            SetHttpContextUser(currentUser.Id);

            List<Category> result = await _categoryService.GetAllCategoriesAsync();

            Assert.Equal(2, result.Count);
            Assert.All(result, category => Assert.Equal(currentUser.Id, category.UserId));
            Assert.Equal(new[] { "Admin", "Work" }, result.Select(category => category.Name));
        }

        [Fact]
        public async Task GetCategoryByIdAsync_WithOwnedCategory_ReturnsCategory()
        {
            User user = await SeedTestUserAsync();
            Category category = await SeedCategoryAsync(user.Id, "Work");
            SetHttpContextUser(user.Id);

            Category result = await _categoryService.GetCategoryByIdAsync(category.Id);

            Assert.Equal(category.Id, result.Id);
            Assert.Equal("Work", result.Name);
            Assert.Equal(user.Id, result.UserId);
        }

        [Fact]
        public async Task GetCategoryByIdAsync_WithDifferentUsersCategory_ThrowsNotFoundException()
        {
            User user = await SeedTestUserAsync("currentuser", "current@mail.com");
            User otherUser = await SeedTestUserAsync("otheruser", "other@mail.com");
            Category category = await SeedCategoryAsync(otherUser.Id, "Private");
            SetHttpContextUser(user.Id);

            await Assert.ThrowsAsync<NotFoundException>(() => _categoryService.GetCategoryByIdAsync(category.Id));
        }

        [Fact]
        public async Task CreateCategoryAsync_WithValidDto_CreatesCategoryForCurrentUser()
        {
            User user = await SeedTestUserAsync();
            SetHttpContextUser(user.Id);

            Category result = await _categoryService.CreateCategoryAsync(new CategoryWriteDto { Name = "  Work  " });

            Assert.NotEqual(0, result.Id);
            Assert.Equal("Work", result.Name);
            Assert.Equal(user.Id, result.UserId);

            await using SubTaskerEfCoreDbContext verifyContext = _postgresFixture.CreateDbContext();
            Category? savedCategory = await verifyContext.Categories.FindAsync(result.Id);

            Assert.NotNull(savedCategory);
            Assert.Equal("Work", savedCategory.Name);
            Assert.Equal(user.Id, savedCategory.UserId);
        }

        [Fact]
        public async Task CreateCategoryAsync_WithWhitespaceName_ThrowsBadRequestException()
        {
            User user = await SeedTestUserAsync();
            SetHttpContextUser(user.Id);

            await Assert.ThrowsAsync<BadRequestException>(() => _categoryService.CreateCategoryAsync(new CategoryWriteDto { Name = "   " }));
        }

        [Fact]
        public async Task CreateCategoryAsync_WithDuplicateNameForSameUser_ThrowsConflictException()
        {
            User user = await SeedTestUserAsync();
            await SeedCategoryAsync(user.Id, "Work");
            SetHttpContextUser(user.Id);

            await Assert.ThrowsAsync<ConflictException>(() => _categoryService.CreateCategoryAsync(new CategoryWriteDto { Name = "Work" }));
        }

        [Fact]
        public async Task CreateCategoryAsync_WithDuplicateNameForDifferentUser_CreatesCategory()
        {
            User user = await SeedTestUserAsync("currentuser", "current@mail.com");
            User otherUser = await SeedTestUserAsync("otheruser", "other@mail.com");
            await SeedCategoryAsync(otherUser.Id, "Work");
            SetHttpContextUser(user.Id);

            Category result = await _categoryService.CreateCategoryAsync(new CategoryWriteDto { Name = "Work" });

            Assert.Equal("Work", result.Name);
            Assert.Equal(user.Id, result.UserId);
        }

        [Fact]
        public async Task UpdateCategoryAsync_WithValidDto_UpdatesCategory()
        {
            User user = await SeedTestUserAsync();
            Category category = await SeedCategoryAsync(user.Id, "Old Name");
            SetHttpContextUser(user.Id);

            Category result = await _categoryService.UpdateCategoryAsync(category.Id, new CategoryWriteDto { Name = "  New Name  " });

            Assert.Equal(category.Id, result.Id);
            Assert.Equal("New Name", result.Name);

            await using SubTaskerEfCoreDbContext verifyContext = _postgresFixture.CreateDbContext();
            Category? updatedCategory = await verifyContext.Categories.FindAsync(category.Id);

            Assert.NotNull(updatedCategory);
            Assert.Equal("New Name", updatedCategory.Name);
        }

        [Fact]
        public async Task UpdateCategoryAsync_WithDuplicateNameForSameUser_ThrowsConflictException()
        {
            User user = await SeedTestUserAsync();
            Category category = await SeedCategoryAsync(user.Id, "Work");
            await SeedCategoryAsync(user.Id, "Personal");
            SetHttpContextUser(user.Id);

            await Assert.ThrowsAsync<ConflictException>(() => _categoryService.UpdateCategoryAsync(category.Id, new CategoryWriteDto { Name = "Personal" }));
        }

        [Fact]
        public async Task DeleteCategoryAsync_WithOwnedCategory_DeletesCategory()
        {
            User user = await SeedTestUserAsync();
            Category category = await SeedCategoryAsync(user.Id, "Work");
            SetHttpContextUser(user.Id);

            await _categoryService.DeleteCategoryAsync(category.Id);

            await using SubTaskerEfCoreDbContext verifyContext = _postgresFixture.CreateDbContext();
            Category? deletedCategory = await verifyContext.Categories.FindAsync(category.Id);

            Assert.Null(deletedCategory);
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

        private async Task<Category> SeedCategoryAsync(int userId, string name)
        {
            Category category = new Category
            {
                Name = name,
                UserId = userId
            };

            _dbContext.Categories.Add(category);
            await _dbContext.SaveChangesAsync();

            return category;
        }
    }
}