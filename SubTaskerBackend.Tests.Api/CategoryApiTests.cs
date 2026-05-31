using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubTaskerBackend.Data;
using SubTaskerBackend.DTOs.Categories;
using SubTaskerBackend.DTOs.Users;
using SubTaskerBackend.Models;
using SubTaskerBackend.Tests.Api.Fixtures;
using SubTaskerBackend.Tests.Api.Helpers;

namespace SubTaskerBackend.Tests.Api
{
    public class CategoryApiTests : IClassFixture<ApiTestFactory>, IAsyncLifetime
    {
        private readonly ApiTestFactory _factory;
        private readonly HttpClient _client;

        public CategoryApiTests(ApiTestFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            await _factory.ResetDatabaseAsync();
        }

        public Task DisposeAsync()
        {
            _client.Dispose();
            return Task.CompletedTask;
        }

        [Fact]
        public async Task GetAllCategories_WithoutAuth_Returns401Unauthorized()
        {
            HttpResponseMessage response = await _client.GetAsync("/api/category");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetAllCategories_WithAuth_ReturnsOnlyCurrentUsersCategories()
        {
            User currentUser = await ApiTestDataHelper.SeedTestUserAsync("user1", "user1@mail.com", "Password123!", _factory);
            User otherUser = await ApiTestDataHelper.SeedTestUserAsync("user2", "user2@mail.com", "Password123!", _factory);

            await SeedCategoryAsync(currentUser.Id, "Work");
            await SeedCategoryAsync(currentUser.Id, "Admin");
            await SeedCategoryAsync(otherUser.Id, "Personal");

            await AuthenticateClientAsync("user1@mail.com", "Password123!");

            HttpResponseMessage response = await _client.GetAsync("/api/category");
            List<CategoryReadDto>? categories = await response.Content.ReadFromJsonAsync<List<CategoryReadDto>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(categories);
            Assert.Equal(2, categories.Count);
            Assert.All(categories, category => Assert.Equal(currentUser.Id, category.UserId));
            Assert.Equal(new[] { "Admin", "Work" }, categories.Select(c => c.Name));
        }

        [Fact]
        public async Task GetCategoryById_WithOwnedCategory_Returns200Ok()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync("user1", "user1@mail.com", "Password123!", _factory);
            Category category = await SeedCategoryAsync(user.Id, "Work");

            await AuthenticateClientAsync("user1@mail.com", "Password123!");

            HttpResponseMessage response = await _client.GetAsync($"/api/category/{category.Id}");
            CategoryReadDto? dto = await response.Content.ReadFromJsonAsync<CategoryReadDto>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(dto);
            Assert.Equal(category.Id, dto.Id);
            Assert.Equal("Work", dto.Name);
            Assert.Equal(user.Id, dto.UserId);
        }

        [Fact]
        public async Task GetCategoryById_WithDifferentUsersCategory_Returns404NotFound()
        {
            User owner = await ApiTestDataHelper.SeedTestUserAsync("owner", "owner@mail.com", "Password123!", _factory);
            await ApiTestDataHelper.SeedTestUserAsync("requester", "requester@mail.com", "Password123!", _factory);
            Category category = await SeedCategoryAsync(owner.Id, "Private");

            await AuthenticateClientAsync("requester@mail.com", "Password123!");

            HttpResponseMessage response = await _client.GetAsync($"/api/category/{category.Id}");
            ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotNull(problemDetails);
            Assert.Equal(404, problemDetails.Status);
            Assert.Equal("Category not found.", problemDetails.Detail);
        }

        [Fact]
        public async Task CreateCategory_WithValidDto_Returns201Created()
        {
            await ApiTestDataHelper.SeedTestUserAsync("user1", "user1@mail.com", "Password123!", _factory);
            await AuthenticateClientAsync("user1@mail.com", "Password123!");

            CategoryWriteDto dto = new CategoryWriteDto { Name = "  Work  " };

            HttpResponseMessage response = await _client.PostAsJsonAsync("/api/category", dto);
            CategoryReadDto? created = await response.Content.ReadFromJsonAsync<CategoryReadDto>();

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(created);
            Assert.True(created.Id > 0);
            Assert.Equal("Work", created.Name);
            Assert.NotNull(response.Headers.Location);
            Assert.Equal($"/api/Category/{created.Id}", response.Headers.Location!.AbsolutePath);
        }

        [Fact]
        public async Task CreateCategory_WithDuplicateNameForSameUser_Returns409Conflict()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync("user1", "user1@mail.com", "Password123!", _factory);
            await SeedCategoryAsync(user.Id, "Work");

            await AuthenticateClientAsync("user1@mail.com", "Password123!");

            HttpResponseMessage response = await _client.PostAsJsonAsync("/api/category", new CategoryWriteDto { Name = "Work" });
            ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.NotNull(problemDetails);
            Assert.Equal(409, problemDetails.Status);
            Assert.Equal("A category with the same name already exists.", problemDetails.Detail);
        }

        [Fact]
        public async Task CreateCategory_WithDuplicateNameForDifferentUser_Returns201Created()
        {
            User owner = await ApiTestDataHelper.SeedTestUserAsync("owner", "owner@mail.com", "Password123!", _factory);
            await ApiTestDataHelper.SeedTestUserAsync("requester", "requester@mail.com", "Password123!", _factory);
            await SeedCategoryAsync(owner.Id, "Work");

            await AuthenticateClientAsync("requester@mail.com", "Password123!");

            HttpResponseMessage response = await _client.PostAsJsonAsync("/api/category", new CategoryWriteDto { Name = "Work" });
            CategoryReadDto? created = await response.Content.ReadFromJsonAsync<CategoryReadDto>();

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(created);
            Assert.Equal("Work", created.Name);
            Assert.NotEqual(owner.Id, created.UserId);
        }

        [Fact]
        public async Task UpdateCategory_WithValidDto_Returns200Ok()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync("user1", "user1@mail.com", "Password123!", _factory);
            Category category = await SeedCategoryAsync(user.Id, "Old Name");

            await AuthenticateClientAsync("user1@mail.com", "Password123!");

            HttpResponseMessage response = await _client.PatchAsJsonAsync($"/api/category/{category.Id}", new CategoryWriteDto { Name = "  New Name  " });
            CategoryReadDto? updated = await response.Content.ReadFromJsonAsync<CategoryReadDto>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(updated);
            Assert.Equal(category.Id, updated.Id);
            Assert.Equal("New Name", updated.Name);
        }

        [Fact]
        public async Task DeleteCategory_WithOwnedCategory_Returns204NoContent()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync("user1", "user1@mail.com", "Password123!", _factory);
            Category category = await SeedCategoryAsync(user.Id, "Work");

            await AuthenticateClientAsync("user1@mail.com", "Password123!");

            HttpResponseMessage response = await _client.DeleteAsync($"/api/category/{category.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            await using SubTaskerEfCoreDbContext verifyDb = _factory.CreateDbContext();
            Category? deleted = await verifyDb.Categories.FindAsync(category.Id);
            Assert.Null(deleted);
        }

        private async Task AuthenticateClientAsync(string email, string password)
        {
            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new UserLoginDto
            {
                Email = email,
                Password = password
            });

            loginResponse.EnsureSuccessStatusCode();

            LoginResponseDto? loginDto = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
            Assert.NotNull(loginDto);
            Assert.False(string.IsNullOrWhiteSpace(loginDto.Token));

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginDto.Token);
        }

        private async Task<Category> SeedCategoryAsync(int userId, string name)
        {
            await using SubTaskerEfCoreDbContext dbContext = _factory.CreateDbContext();

            Category category = new Category
            {
                Name = name,
                UserId = userId
            };

            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();

            return category;
        }
    }
}