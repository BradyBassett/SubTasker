

using System.Net;
using System.Net.Http.Json;
using SubTaskerBackend.DTOs.TaskItems;
using SubTaskerBackend.Models;
using SubTaskerBackend.Tests.Api.Fixtures;
using SubTaskerBackend.Tests.Api.Helpers;

namespace SubTaskerBackend.Tests.Api
{
    public class TaskItemApiTests : IClassFixture<ApiTestFactory>, IAsyncLifetime
    {
        private readonly ApiTestFactory _factory;

        private readonly HttpClient _client;

        public TaskItemApiTests(ApiTestFactory factory)
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
        public async Task GetAllTaskItems_WithNoTasks_Returns200OkAndEmptyList()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "email@mail.com", "password123");
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "email@mail.com", "password123");

            HttpResponseMessage response = await _client.GetAsync("/api/taskitem");
            List<TaskItemReadDto>? taskItems = await response.Content.ReadFromJsonAsync<List<TaskItemReadDto>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(taskItems);
            Assert.Empty(taskItems);
        }

        [Fact]
        public async Task GetAllTaskItems_WithMultipleUsers_ReturnsOnlyCurrentUsersTasks()
        {
            User currentUser = await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            User otherUser = await ApiTestDataHelper.SeedTestUserAsync(_factory, "user2", "user2@mail.com", "Password123!");

            await ApiTestDataHelper.SeedTaskItemAsync(_factory, currentUser.Id, "Task 1");
            await ApiTestDataHelper.SeedTaskItemAsync(_factory, currentUser.Id, "Task 2");
            await ApiTestDataHelper.SeedTaskItemAsync(_factory, otherUser.Id, "Task 3");

            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");

            HttpResponseMessage response = await _client.GetAsync("/api/taskitem");
            List<TaskItemReadDto>? taskItems = await response.Content.ReadFromJsonAsync<List<TaskItemReadDto>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(taskItems);
            Assert.Equal(2, taskItems.Count);
            Assert.All(taskItems, task => Assert.Equal(currentUser.Id, task.UserId));
        }

        [Fact]
        public async Task GetAllTaskItems_WithTasksHavingDueDates_ReturnsTasksOrderedByDueDate()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");

            DateTime dueDate = DateTime.UtcNow;
            await ApiTestDataHelper.SeedTaskItemAsync(_factory, user.Id, "Task 1", dueDate.AddDays(2));
            await ApiTestDataHelper.SeedTaskItemAsync(_factory, user.Id, "Task 2", dueDate.AddDays(1));
            await ApiTestDataHelper.SeedTaskItemAsync(_factory, user.Id, "Task 3", dueDate.AddDays(3));

            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");

            HttpResponseMessage response = await _client.GetAsync("/api/taskitem");
            List<TaskItemReadDto>? taskItems = await response.Content.ReadFromJsonAsync<List<TaskItemReadDto>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(taskItems);
            Assert.Equal(3, taskItems.Count);
            Assert.Equal(new[] { "Task 2", "Task 1", "Task 3" }, taskItems.Select(t => t.Title));
        }

        [Fact]
        public async Task GetAllTaskItems_WithTasks_ReturnsExpectedTaskDtoFields()
        {
        }

        [Fact]
        public async Task GetAllTaskItems_WithTaggedTasks_ReturnsExpectedTags()
        {
        }

        [Fact]
        public async Task GetAllTaskItems_WithParentTasks_ReturnsExpectedSubTasks()
        {
        }

        [Fact]
        public async Task GetAllTaskItems_WithoutAuth_Returns401Unauthorized()
        {
        }

    }
}