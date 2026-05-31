using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SubTaskerBackend.Data;
using SubTaskerBackend.DTOs.Tags;
using SubTaskerBackend.DTOs.Users;
using SubTaskerBackend.Models;
using SubTaskerBackend.Tests.Api.Fixtures;
using SubTaskerBackend.Tests.Api.Helpers;

namespace SubTaskerBackend.Tests.Api
{
    public class TagApiTests : IClassFixture<ApiTestFactory>, IAsyncLifetime
    {
        private readonly ApiTestFactory _factory;
        private readonly HttpClient _client;

        public TagApiTests(ApiTestFactory factory)
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
        public async Task GetAllTags_WithoutAuth_Returns401Unauthorized()
        {
            HttpResponseMessage response = await _client.GetAsync("/api/tag");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetAllTags_WithAuth_ReturnsOnlyCurrentUsersTags()
        {
            User currentUser = await ApiTestDataHelper.SeedTestUserAsync("user1", "user1@mail.com", "Password123!", _factory);
            User otherUser = await ApiTestDataHelper.SeedTestUserAsync("user2", "user2@mail.com", "Password123!", _factory);

            await SeedTagAsync(currentUser.Id, "Urgent");
            await SeedTagAsync(currentUser.Id, "Backend");
            await SeedTagAsync(otherUser.Id, "Frontend");

            await AuthenticateClientAsync("user1@mail.com", "Password123!");

            HttpResponseMessage response = await _client.GetAsync("/api/tag");
            List<TagReadDto>? tags = await response.Content.ReadFromJsonAsync<List<TagReadDto>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(tags);
            Assert.Equal(2, tags.Count);
            Assert.All(tags, tag => Assert.Equal(currentUser.Id, tag.UserId));
            Assert.Equal(new[] { "Backend", "Urgent" }, tags.Select(t => t.Name));
        }

        [Fact]
        public async Task GetTagById_WithOwnedTag_Returns200Ok()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync("user1", "user1@mail.com", "Password123!", _factory);
            Tag tag = await SeedTagAsync(user.Id, "Urgent");

            await AuthenticateClientAsync("user1@mail.com", "Password123!");

            HttpResponseMessage response = await _client.GetAsync($"/api/tag/{tag.Id}");
            TagReadDto? dto = await response.Content.ReadFromJsonAsync<TagReadDto>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(dto);
            Assert.Equal(tag.Id, dto.Id);
            Assert.Equal("Urgent", dto.Name);
            Assert.Equal(user.Id, dto.UserId);
        }

        [Fact]
        public async Task GetTagById_WithDifferentUsersTag_Returns404NotFound()
        {
            User owner = await ApiTestDataHelper.SeedTestUserAsync("owner", "owner@mail.com", "Password123!", _factory);
            await ApiTestDataHelper.SeedTestUserAsync("requester", "requester@mail.com", "Password123!", _factory);
            Tag tag = await SeedTagAsync(owner.Id, "Private");

            await AuthenticateClientAsync("requester@mail.com", "Password123!");

            HttpResponseMessage response = await _client.GetAsync($"/api/tag/{tag.Id}");
            ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotNull(problemDetails);
            Assert.Equal(404, problemDetails.Status);
            Assert.Equal("Tag not found.", problemDetails.Detail);
        }

        [Fact]
        public async Task CreateTag_WithValidDto_Returns201Created()
        {
            await ApiTestDataHelper.SeedTestUserAsync("user1", "user1@mail.com", "Password123!", _factory);
            await AuthenticateClientAsync("user1@mail.com", "Password123!");

            TagWriteDto dto = new TagWriteDto { Name = "  Urgent  " };

            HttpResponseMessage response = await _client.PostAsJsonAsync("/api/tag", dto);
            TagReadDto? created = await response.Content.ReadFromJsonAsync<TagReadDto>();

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(created);
            Assert.True(created.Id > 0);
            Assert.Equal("Urgent", created.Name);
            Assert.NotNull(response.Headers.Location);
            Assert.Equal($"/api/Tag/{created.Id}", response.Headers.Location!.AbsolutePath);
        }

        [Fact]
        public async Task CreateTag_WithDuplicateNameForSameUser_Returns409Conflict()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync("user1", "user1@mail.com", "Password123!", _factory);
            await SeedTagAsync(user.Id, "Urgent");

            await AuthenticateClientAsync("user1@mail.com", "Password123!");

            HttpResponseMessage response = await _client.PostAsJsonAsync("/api/tag", new TagWriteDto { Name = "Urgent" });
            ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.NotNull(problemDetails);
            Assert.Equal(409, problemDetails.Status);
            Assert.Equal("A tag with the same name already exists.", problemDetails.Detail);
        }

        [Fact]
        public async Task CreateTag_WithDuplicateNameForDifferentUser_Returns201Created()
        {
            User owner = await ApiTestDataHelper.SeedTestUserAsync("owner", "owner@mail.com", "Password123!", _factory);
            await ApiTestDataHelper.SeedTestUserAsync("requester", "requester@mail.com", "Password123!", _factory);
            await SeedTagAsync(owner.Id, "Urgent");

            await AuthenticateClientAsync("requester@mail.com", "Password123!");

            HttpResponseMessage response = await _client.PostAsJsonAsync("/api/tag", new TagWriteDto { Name = "Urgent" });
            TagReadDto? created = await response.Content.ReadFromJsonAsync<TagReadDto>();

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(created);
            Assert.Equal("Urgent", created.Name);
            Assert.NotEqual(owner.Id, created.UserId);
        }

        [Fact]
        public async Task UpdateTag_WithValidDto_Returns200Ok()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync("user1", "user1@mail.com", "Password123!", _factory);
            Tag tag = await SeedTagAsync(user.Id, "Old Name");

            await AuthenticateClientAsync("user1@mail.com", "Password123!");

            HttpResponseMessage response = await _client.PatchAsJsonAsync($"/api/tag/{tag.Id}", new TagWriteDto { Name = "  New Name  " });
            TagReadDto? updated = await response.Content.ReadFromJsonAsync<TagReadDto>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(updated);
            Assert.Equal(tag.Id, updated.Id);
            Assert.Equal("New Name", updated.Name);
        }

        [Fact]
        public async Task DeleteTag_WithOwnedTag_Returns204NoContent()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync("user1", "user1@mail.com", "Password123!", _factory);
            Tag tag = await SeedTagAsync(user.Id, "Urgent");

            await AuthenticateClientAsync("user1@mail.com", "Password123!");

            HttpResponseMessage response = await _client.DeleteAsync($"/api/tag/{tag.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            await using SubTaskerEfCoreDbContext verifyDb = _factory.CreateDbContext();
            Tag? deleted = await verifyDb.Tags.FindAsync(tag.Id);
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

        private async Task<Tag> SeedTagAsync(int userId, string name)
        {
            await using SubTaskerEfCoreDbContext dbContext = _factory.CreateDbContext();

            Tag tag = new Tag
            {
                Name = name,
                UserId = userId
            };

            dbContext.Tags.Add(tag);
            await dbContext.SaveChangesAsync();

            return tag;
        }
    }
}