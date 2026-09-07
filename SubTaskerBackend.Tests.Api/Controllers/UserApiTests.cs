using System.Net;
using System.Net.Http.Json;
using SubTaskerBackend.DTOs.Users;
using SubTaskerBackend.Models;
using SubTaskerBackend.Tests.Api.Fixtures;
using SubTaskerBackend.Tests.Api.Helpers;

namespace SubTaskerBackend.Tests.Api
{
    public class UserApiTest : IClassFixture<ApiTestFactory>, IAsyncLifetime
    {
        private readonly ApiTestFactory _factory;

        private readonly HttpClient _client;

        public UserApiTest(ApiTestFactory factory)
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
        public async Task GetCurrentUser_WithAuthenticatedUser_Returns200OkAndCurrentUsersDto()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync(
                "user1",
                "user1@mail.com",
                "Password123!",
                _factory);

            await ApiTestDataHelper.AuthenticateClientAsync(
                _client,
                "user1@mail.com",
                "Password123!");

            HttpResponseMessage response = await _client.GetAsync("/api/user/me");
            UserReadDto? currentUser = await response.Content.ReadFromJsonAsync<UserReadDto>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(currentUser);
            Assert.Equal(user.Id, currentUser.Id);
            Assert.Equal(user.Username, currentUser.Username);
            Assert.Equal(user.Email, currentUser.Email);
        }

        [Fact]
        public async Task GetCurrentUser_WithoutAuth_Returns401Unauthorized()
        {
            HttpResponseMessage response = await _client.GetAsync("/api/user/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetUserById_WithCurrentUsersId_Returns200OkAndCurrentUsersDto()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync(
                "user1",
                "user1@mail.com",
                "Password123!",
                _factory);

            await ApiTestDataHelper.AuthenticateClientAsync(
                _client,
                "user1@mail.com",
                "Password123!");

            HttpResponseMessage response = await _client.GetAsync($"/api/user/{user.Id}");
            UserReadDto? currentUser = await response.Content.ReadFromJsonAsync<UserReadDto>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(currentUser);
            Assert.Equal(user.Id, currentUser.Id);
            Assert.Equal(user.Username, currentUser.Username);
            Assert.Equal(user.Email, currentUser.Email);
        }

        [Fact]
        public async Task GetUserById_WithoutAuth_Returns401Unauthorized()
        {
            HttpResponseMessage response = await _client.GetAsync($"/api/user/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetUserById_WithAnotherUsersId_Returns404NotFound()
        {
            User user1 = await ApiTestDataHelper.SeedTestUserAsync(
                "user1",
                "user1@mail.com",
                "Password123!",
                _factory);

            User user2 = await ApiTestDataHelper.SeedTestUserAsync(
                "user2",
                "user2@mail.com",
                "Password123!",
                _factory);

            await ApiTestDataHelper.AuthenticateClientAsync(
                _client,
                "user1@mail.com",
                "Password123!");

            HttpResponseMessage response = await _client.GetAsync($"/api/user/{user2.Id}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetCurrentUser_WhenAuthenticatedUserWasDeleted_Returns404NotFound()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync(
                "user1",
                "user1@mail.com",
                "Password123!",
                _factory);

            await ApiTestDataHelper.AuthenticateClientAsync(
                _client,
                "user1@mail.com",
                "Password123!");

            await ApiTestDataHelper.DeleteTestUserAsync(user.Id, _factory);

            HttpResponseMessage response = await _client.GetAsync($"/api/user/{user.Id}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}