using System.Net;
using System.Net.Http.Json;
using SubTaskerBackend.DTOs.TaskItems;
using SubTaskerBackend.DTOs.Tags;
using SubTaskerBackend.Models;
using SubTaskerBackend.Tests.Api.Fixtures;
using SubTaskerBackend.Tests.Api.Helpers;
using Microsoft.Extensions.DependencyInjection;

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
            List<TaskItemReadDto>? taskItems = await response.Content.ReadFromJsonAsync<List<TaskItemReadDto>>(ApiTestDataHelper.JsonOptions);

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
            List<TaskItemReadDto>? taskItems = await response.Content.ReadFromJsonAsync<List<TaskItemReadDto>>(ApiTestDataHelper.JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(taskItems);
            Assert.Equal(3, taskItems.Count);
            Assert.Equal(new[] { "Task 2", "Task 1", "Task 3" }, taskItems.Select(t => t.Title));
        }

        [Fact]
        public async Task GetAllTaskItems_WithTasks_ReturnsExpectedTaskDtoFields()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            DateTime dueDate = DateTime.UtcNow.AddDays(1);
            Category category = await ApiTestDataHelper.SeedCategoryAsync(_factory, user.Id, "TestCategory");
            TaskItem task = await ApiTestDataHelper.SeedTaskItemAsync(
                _factory,
                user.Id,
                "Task",
                dueDate,
                description: "Task description",
                status: Enums.TaskStatus.inProgress,
                priority: Enums.PriorityLevel.High,
                categoryId: category.Id
            );

            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");

            HttpResponseMessage response = await _client.GetAsync("/api/taskitem");
            List<TaskItemReadDto>? taskItems = await response.Content.ReadFromJsonAsync<List<TaskItemReadDto>>(ApiTestDataHelper.JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(taskItems);
            TaskItemReadDto taskDto = Assert.Single(taskItems);
            Assert.Equal(task.Id, taskDto.Id);
            Assert.Equal("Task", taskDto.Title);
            Assert.Equal(user.Id, taskDto.UserId);
            Assert.Equal("Task description", taskDto.Description);
            Assert.Equal(Enums.TaskStatus.inProgress, taskDto.Status);
            Assert.Equal(Enums.PriorityLevel.High, taskDto.Priority);
            Assert.NotNull(taskDto.DueDate);
            Assert.Equal(dueDate, taskDto.DueDate.Value, TimeSpan.FromMicroseconds(1));
            Assert.Equal(category.Id, taskDto.CategoryId);
        }

        [Fact]
        public async Task GetAllTaskItems_WithTasks_ReturnsExpectedRelationships()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            Category category = await ApiTestDataHelper.SeedCategoryAsync(_factory, user.Id, "TestCategory");
            Tag tag1 = await ApiTestDataHelper.SeedTagAsync(_factory, user.Id, "TestTag1");
            Tag tag2 = await ApiTestDataHelper.SeedTagAsync(_factory, user.Id, "TestTag2");
            Tag tag3 = await ApiTestDataHelper.SeedTagAsync(_factory, user.Id, "TestTag3");
            TaskItem parentTask = await ApiTestDataHelper.SeedTaskItemAsync(
                _factory,
                user.Id,
                "ParentTask",
                categoryId: category.Id,
                tagIds: new List<int> { tag1.Id, tag2.Id }
            );
            TaskItem subTask = await ApiTestDataHelper.SeedTaskItemAsync(
                _factory,
                user.Id,
                "SubTask",
                categoryId: category.Id,
                parentTaskId: parentTask.Id,
                tagIds: new List<int> { tag2.Id, tag3.Id }
            );
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");

            HttpResponseMessage response = await _client.GetAsync("/api/taskitem");
            List<TaskItemReadDto>? taskItems = await response.Content.ReadFromJsonAsync<List<TaskItemReadDto>>(ApiTestDataHelper.JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(taskItems);
            Assert.Equal(2, taskItems.Count);
            TaskItemReadDto parentTaskDto = Assert.Single(taskItems, task => task.Title == "ParentTask");
            TaskItemReadDto subTaskDto = Assert.Single(taskItems, task => task.Title == "SubTask");
            Assert.Equal(category.Id, parentTaskDto.CategoryId);
            Assert.Equal(category.Id, subTaskDto.CategoryId);
            Assert.Null(parentTaskDto.ParentTaskId);
            Assert.Equal(parentTask.Id, subTaskDto.ParentTaskId);
            Assert.Contains(subTask.Id, parentTaskDto.SubTaskIds);
            Assert.Equal(new List<int> { tag1.Id, tag2.Id }, parentTaskDto.TagIds);
            Assert.Equal(new List<int> { tag2.Id, tag3.Id }, subTaskDto.TagIds);
        }

        [Fact]
        public async Task GetAllTaskItems_WithoutAuth_Returns401Unauthorized()
        {
            HttpResponseMessage response = await _client.GetAsync("/api/taskitem");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetTaskItemById_WithOwnedTask_Returns200OkWithExpectedDtoAndRelationships()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            DateTime dueDate = DateTime.UtcNow.AddDays(1);
            Category category = await ApiTestDataHelper.SeedCategoryAsync(_factory, user.Id, "TestCategory");
            Tag tag1 = await ApiTestDataHelper.SeedTagAsync(_factory, user.Id, "TestTag1");
            Tag tag2 = await ApiTestDataHelper.SeedTagAsync(_factory, user.Id, "TestTag2");

            TaskItem task = await ApiTestDataHelper.SeedTaskItemAsync(
                _factory,
                user.Id,
                "Task",
                dueDate,
                description: "Task description",
                status: Enums.TaskStatus.inProgress,
                priority: Enums.PriorityLevel.High,
                categoryId: category.Id,
                tagIds: new List<int> { tag1.Id, tag2.Id });

            TaskItem subTask = await ApiTestDataHelper.SeedTaskItemAsync(
                _factory,
                user.Id,
                "SubTask",
                parentTaskId: task.Id);

            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");

            HttpResponseMessage response = await _client.GetAsync($"/api/taskitem/{task.Id}");
            TaskItemReadDto? taskDto = await response.Content.ReadFromJsonAsync<TaskItemReadDto>(ApiTestDataHelper.JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(taskDto);
            Assert.Equal(task.Id, taskDto.Id);
            Assert.Equal(task.Title, taskDto.Title);
            Assert.Equal(user.Id, taskDto.UserId);
            Assert.Equal(task.Description, taskDto.Description);
            Assert.Equal(task.Status, taskDto.Status);
            Assert.Equal(task.Priority, taskDto.Priority);
            Assert.NotNull(taskDto.DueDate);
            Assert.Equal(dueDate, taskDto.DueDate.Value, TimeSpan.FromMicroseconds(1));
            Assert.Equal(category.Id, taskDto.CategoryId);
            Assert.Null(taskDto.ParentTaskId);
            Assert.Contains(tag1.Id, taskDto.TagIds);
            Assert.Contains(tag2.Id, taskDto.TagIds);
            Assert.Contains(subTask.Id, taskDto.SubTaskIds);
        }

        [Fact]
        public async Task GetTaskItemById_WithoutAuth_Returns401Unauthorized()
        {
            HttpResponseMessage response = await _client.GetAsync("/api/taskitem/1");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetTaskItemById_WithNonexistentTaskId_Returns404NotFound()
        {
            await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");

            HttpResponseMessage response = await _client.GetAsync("/api/taskitem/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetTaskItemById_WithAnotherUsersTaskId_Returns404NotFound()
        {
            User owner = await ApiTestDataHelper.SeedTestUserAsync(_factory, "owner", "owner@mail.com", "Password123!");
            await ApiTestDataHelper.SeedTestUserAsync(_factory, "requester", "requester@mail.com", "Password123!");
            TaskItem task = await ApiTestDataHelper.SeedTaskItemAsync(_factory, owner.Id, "Private task");

            await ApiTestDataHelper.AuthenticateClientAsync(_client, "requester@mail.com", "Password123!");

            HttpResponseMessage response = await _client.GetAsync($"/api/taskitem/{task.Id}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetTagsByTaskItemId_WithTaskHavingNoTags_Returns200OkAndEmptyList()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            TaskItem task = await ApiTestDataHelper.SeedTaskItemAsync(_factory, user.Id, "Task");

            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");

            HttpResponseMessage response = await _client.GetAsync($"/api/taskitem/{task.Id}/tags");
            List<TagReadDto>? tags = await response.Content.ReadFromJsonAsync<List<TagReadDto>>(ApiTestDataHelper.JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(tags);
            Assert.Empty(tags);
        }

        [Fact]
        public async Task GetTagsByTaskItemId_WithTags_ReturnsExpectedTagDtos()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            Tag tag1 = await ApiTestDataHelper.SeedTagAsync(_factory, user.Id, "Urgent");
            Tag tag2 = await ApiTestDataHelper.SeedTagAsync(_factory, user.Id, "Backend");
            TaskItem task = await ApiTestDataHelper.SeedTaskItemAsync(
                _factory,
                user.Id,
                "Task",
                tagIds: new List<int> { tag1.Id, tag2.Id });

            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");

            HttpResponseMessage response = await _client.GetAsync($"/api/taskitem/{task.Id}/tags");
            List<TagReadDto>? tags = await response.Content.ReadFromJsonAsync<List<TagReadDto>>(ApiTestDataHelper.JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(tags);
            Assert.Equal(2, tags.Count);

            TagReadDto returnedTag1 = Assert.Single(tags, tag => tag.Id == tag1.Id);
            TagReadDto returnedTag2 = Assert.Single(tags, tag => tag.Id == tag2.Id);
            Assert.Equal(tag1.Name, returnedTag1.Name);
            Assert.Equal(tag2.Name, returnedTag2.Name);
            Assert.Equal(user.Id, returnedTag1.UserId);
            Assert.Equal(user.Id, returnedTag2.UserId);
            Assert.Contains(task.Id, returnedTag1.TaskIds);
            Assert.Contains(task.Id, returnedTag2.TaskIds);
        }

        [Fact]
        public async Task GetTagsByTaskItemId_WithoutAuth_Returns401Unauthorized()
        {
            HttpResponseMessage response = await _client.GetAsync("/api/taskitem/1/tags");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetTagsByTaskItemId_WithNonexistentTaskId_Returns404NotFound()
        {
            await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");

            HttpResponseMessage response = await _client.GetAsync("/api/taskitem/999999/tags");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetTagsByTaskItemId_WithAnotherUsersTaskId_Returns404NotFound()
        {
            User owner = await ApiTestDataHelper.SeedTestUserAsync(_factory, "owner", "owner@mail.com", "Password123!");
            await ApiTestDataHelper.SeedTestUserAsync(_factory, "requester", "requester@mail.com", "Password123!");
            TaskItem task = await ApiTestDataHelper.SeedTaskItemAsync(_factory, owner.Id, "Private task");

            await ApiTestDataHelper.AuthenticateClientAsync(_client, "requester@mail.com", "Password123!");

            HttpResponseMessage response = await _client.GetAsync($"/api/taskitem/{task.Id}/tags");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateTaskItem_WithMinimumValidData_Returns201Created()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");

            TaskItemWriteDto createDto = new TaskItemWriteDto
            {
                Title = "Test Task"
            };

            HttpResponseMessage response = await _client.PostAsJsonAsync("/api/taskitem", createDto);
            TaskItemReadDto? createdTask = await response.Content.ReadFromJsonAsync<TaskItemReadDto>(ApiTestDataHelper.JsonOptions);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(createdTask);
            Assert.Equal(createDto.Title, createdTask.Title);
            Assert.Equal(user.Id, createdTask.UserId);
            Assert.Equal(Enums.TaskStatus.notStarted, createdTask.Status);
            Assert.Equal(Enums.PriorityLevel.Medium, createdTask.Priority);
            Assert.NotNull(response.Headers.Location);
            Assert.Equal($"/api/TaskItem/{createdTask.Id}", response.Headers.Location!.AbsolutePath);
        }

        [Fact]
        public async Task CreateTaskItem_WithMaximumValidData_Returns201CreatedAndPersistsAllFields()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");
            Category category = await ApiTestDataHelper.SeedCategoryAsync(_factory, user.Id, "Test Category");
            Tag tag1 = await ApiTestDataHelper.SeedTagAsync(_factory, user.Id, "Test Tag1");
            Tag tag2 = await ApiTestDataHelper.SeedTagAsync(_factory, user.Id, "Test Tag2");
            Tag tag3 = await ApiTestDataHelper.SeedTagAsync(_factory, user.Id, "Test Tag3");
            TaskItem parentTask = await ApiTestDataHelper.SeedTaskItemAsync(_factory, user.Id, "Parent Task");

            TaskItemWriteDto createDto = new TaskItemWriteDto
            {
                Title = "Test Task",
                Description = "This is a test task with maximum valid data",
                Status = Enums.TaskStatus.inProgress,
                Priority = Enums.PriorityLevel.High,
                DueDate = DateTime.UtcNow.AddDays(7),
                CategoryId = category.Id,
                TagIds = new List<int> { tag1.Id, tag2.Id, tag3.Id },
                ParentTaskId = parentTask.Id
            };

            HttpResponseMessage response = await _client.PostAsJsonAsync("/api/taskitem", createDto);
            TaskItemReadDto? createdTask = await response.Content.ReadFromJsonAsync<TaskItemReadDto>(ApiTestDataHelper.JsonOptions);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(createdTask);
            Assert.Equal(createDto.Title, createdTask.Title);
            Assert.Equal(createDto.Description, createdTask.Description);
            Assert.Equal(user.Id, createdTask.UserId);
            Assert.Equal(createDto.Status, createdTask.Status);
            Assert.Equal(createDto.Priority, createdTask.Priority);
            Assert.Equal(createDto.DueDate, createdTask.DueDate);
            Assert.Equal(createDto.CategoryId, createdTask.CategoryId);
            Assert.Equal(createDto.TagIds, createdTask.TagIds);
            Assert.Equal(createDto.ParentTaskId, createdTask.ParentTaskId);
            Assert.NotNull(response.Headers.Location);
            Assert.Equal($"/api/TaskItem/{createdTask.Id}", response.Headers.Location!.AbsolutePath);
        }

        [Fact]
        public async Task CreateTaskItem_WithParentTask_ParentContainsChildInSubTaskIds()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");
            TaskItem parentTask = await ApiTestDataHelper.SeedTaskItemAsync(_factory, user.Id, "Parent Task");

            TaskItemWriteDto createDto = new TaskItemWriteDto
            {
                Title = "Child Task",
                ParentTaskId = parentTask.Id
            };

            HttpResponseMessage response = await _client.PostAsJsonAsync("/api/taskitem", createDto);
            TaskItemReadDto? createdTask = await response.Content.ReadFromJsonAsync<TaskItemReadDto>(ApiTestDataHelper.JsonOptions);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(createdTask);
            Assert.Equal(parentTask.Id, createdTask.ParentTaskId);

            HttpResponseMessage parentResponse = await _client.GetAsync($"/api/taskitem/{parentTask.Id}");
            TaskItemReadDto? parentTaskDto = await parentResponse.Content.ReadFromJsonAsync<TaskItemReadDto>(ApiTestDataHelper.JsonOptions);

            Assert.Equal(HttpStatusCode.OK, parentResponse.StatusCode);
            Assert.NotNull(parentTaskDto);
            Assert.Contains(createdTask.Id, parentTaskDto.SubTaskIds);
        }

        [Fact]
        public async Task CreateTaskItem_WithoutAuth_Returns401Unauthorized()
        {
            TaskItemWriteDto createDto = new TaskItemWriteDto
            {
                Title = "Test Task"
            };

            HttpResponseMessage response = await _client.PostAsJsonAsync("/api/taskitem", createDto);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateTaskItem_WithCallerSuppliedUserId_IgnoresItAndUsesAuthenticatedUser()
        {
            User currentUser = await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            User otherUser = await ApiTestDataHelper.SeedTestUserAsync(_factory, "user2", "user2@mail.com", "Password123!");
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");

            object createDto = new
            {
                Title = "Test Task",
                UserId = otherUser.Id
            };

            HttpResponseMessage response = await _client.PostAsJsonAsync("/api/taskitem", createDto);
            TaskItemReadDto? createdTask = await response.Content.ReadFromJsonAsync<TaskItemReadDto>(ApiTestDataHelper.JsonOptions);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(createdTask);
            Assert.Equal(currentUser.Id, createdTask.UserId);
        }

        [Fact]
        public async Task CreateTaskItem_WithoutTitle_Returns400BadRequest()
        {
            await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");

            TaskItemWriteDto createDto = new TaskItemWriteDto
            {
                Description = "Missing title"
            };

            HttpResponseMessage response = await _client.PostAsJsonAsync("/api/taskitem", createDto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("   ")]
        [InlineData("\t")]
        public async Task CreateTaskItem_WithWhitespaceOnlyTitle_Returns400BadRequest(string title)
        {
            await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");

            TaskItemWriteDto createDto = new TaskItemWriteDto
            {
                Title = title
            };

            HttpResponseMessage response = await _client.PostAsJsonAsync("/api/taskitem", createDto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData("category", "missing")]
        [InlineData("category", "owner")]
        [InlineData("parent", "missing")]
        [InlineData("parent", "owner")]
        [InlineData("tag", "missing")]
        [InlineData("tag", "owner")]
        public async Task CreateTaskItem_WithInvalidRelationshipIds_Returns404NotFound(string relationshipType, string idSource)
        {
            User owner = await ApiTestDataHelper.SeedTestUserAsync(_factory, "owner", "owner@mail.com", "Password123!");
            await ApiTestDataHelper.SeedTestUserAsync(_factory, "requester", "requester@mail.com", "Password123!");
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "requester@mail.com", "Password123!");

            Category? ownerCategory = null;
            TaskItem? ownerTask = null;
            Tag? ownerTag = null;

            if (relationshipType == "category")
            {
                ownerCategory = await ApiTestDataHelper.SeedCategoryAsync(_factory, owner.Id, "Owner Category");
            }
            else if (relationshipType == "parent")
            {
                ownerTask = await ApiTestDataHelper.SeedTaskItemAsync(_factory, owner.Id, "Owner Task");
            }
            else if (relationshipType == "tag")
            {
                ownerTag = await ApiTestDataHelper.SeedTagAsync(_factory, owner.Id, "Owner Tag");
            }

            TaskItemWriteDto createDto = new TaskItemWriteDto
            {
                Title = "Test Task"
            };

            switch (relationshipType)
            {
                case "category":
                    createDto.CategoryId = idSource == "missing" ? 999999 : ownerCategory!.Id;
                    break;
                case "parent":
                    createDto.ParentTaskId = idSource == "missing" ? 999999 : ownerTask!.Id;
                    break;
                case "tag":
                    createDto.TagIds = new List<int> { idSource == "missing" ? 999999 : ownerTag!.Id };
                    break;
            }

            HttpResponseMessage response = await _client.PostAsJsonAsync("/api/taskitem", createDto);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateTaskItem_WithDuplicateTagIds_Returns400BadRequest()
        {
            await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");
            Tag tag1 = await ApiTestDataHelper.SeedTagAsync(_factory, 1, "Tag 1");
            Tag tag2 = await ApiTestDataHelper.SeedTagAsync(_factory, 1, "Tag 2");

            TaskItemWriteDto createDto = new TaskItemWriteDto
            {
                Title = "Test Task",
                TagIds = new List<int> { tag1.Id, tag2.Id, tag1.Id }
            };

            HttpResponseMessage response = await _client.PostAsJsonAsync("/api/taskitem", createDto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddTagToTaskItem_WithOwnedTaskAndTag_Returns204AndTagAppearsInGetResponse()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            TaskItem task = await ApiTestDataHelper.SeedTaskItemAsync(_factory, user.Id, "Task");
            Tag tag = await ApiTestDataHelper.SeedTagAsync(_factory, user.Id, "Tag");
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");

            HttpResponseMessage addResponse = await _client.PostAsync($"/api/taskitem/{task.Id}/tags/{tag.Id}", null);

            Assert.Equal(HttpStatusCode.NoContent, addResponse.StatusCode);

            HttpResponseMessage getResponse = await _client.GetAsync($"/api/taskitem/{task.Id}/tags");
            List<TagReadDto>? tags = await getResponse.Content.ReadFromJsonAsync<List<TagReadDto>>(ApiTestDataHelper.JsonOptions);

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.NotNull(tags);
            TagReadDto addedTag = Assert.Single(tags, returnedTag => returnedTag.Id == tag.Id);
            Assert.Equal(tag.Name, addedTag.Name);
            Assert.Contains(task.Id, addedTag.TaskIds);
        }

        [Fact]
        public async Task AddTagToTaskItem_WithoutAuth_Returns401Unauthorized()
        {
            HttpResponseMessage response = await _client.PostAsync("/api/taskitem/1/tags/1", null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("task", "missing")]
        [InlineData("task", "owner")]
        [InlineData("tag", "missing")]
        [InlineData("tag", "owner")]
        public async Task AddTagToTaskItem_WithInvalidTaskOrTagId_Returns404NotFound(string resourceType, string idSource)
        {
            User owner = await ApiTestDataHelper.SeedTestUserAsync(_factory, "owner", "owner@mail.com", "Password123!");
            await ApiTestDataHelper.SeedTestUserAsync(_factory, "requester", "requester@mail.com", "requester@mail.com");
            TaskItem? ownerTask = null;
            Tag? ownerTag = null;

            if (resourceType == "task")
            {
                ownerTask = await ApiTestDataHelper.SeedTaskItemAsync(_factory, owner.Id, "Owner Task");
            }
            else
            {
                ownerTag = await ApiTestDataHelper.SeedTagAsync(_factory, owner.Id, "Owner Tag");
            }

            await ApiTestDataHelper.AuthenticateClientAsync(_client, "requester@mail.com", "requester@mail.com");

            int taskId = resourceType == "task" && idSource == "owner" ? ownerTask!.Id : 999999;
            int tagId = resourceType == "tag" && idSource == "owner" ? ownerTag!.Id : 999999;

            HttpResponseMessage response = await _client.PostAsync($"/api/taskitem/{taskId}/tags/{tagId}", null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AddTagToTaskItem_WithAlreadyAssociatedTag_Returns400BadRequest()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            Tag tag = await ApiTestDataHelper.SeedTagAsync(_factory, user.Id, "Tag");
            TaskItem task = await ApiTestDataHelper.SeedTaskItemAsync(
                _factory,
                user.Id,
                "Task",
                tagIds: new List<int> { tag.Id });
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");

            HttpResponseMessage response = await _client.PostAsync($"/api/taskitem/{task.Id}/tags/{tag.Id}", null);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateTaskItem_WithAllFields_Returns200OkWithUpdatedValues()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            Category oldCategory = await ApiTestDataHelper.SeedCategoryAsync(_factory, user.Id, "Old Category");
            Category newCategory = await ApiTestDataHelper.SeedCategoryAsync(_factory, user.Id, "New Category");
            Tag oldTag = await ApiTestDataHelper.SeedTagAsync(_factory, user.Id, "Old Tag");
            Tag newTag1 = await ApiTestDataHelper.SeedTagAsync(_factory, user.Id, "New Tag 1");
            Tag newTag2 = await ApiTestDataHelper.SeedTagAsync(_factory, user.Id, "New Tag 2");
            TaskItem parentTask = await ApiTestDataHelper.SeedTaskItemAsync(_factory, user.Id, "Parent Task");
            TaskItem oldSubTask = await ApiTestDataHelper.SeedTaskItemAsync(_factory, user.Id, "Old Subtask");
            TaskItem newSubTask1 = await ApiTestDataHelper.SeedTaskItemAsync(_factory, user.Id, "New Subtask 1");
            TaskItem newSubTask2 = await ApiTestDataHelper.SeedTaskItemAsync(_factory, user.Id, "New Subtask 2");
            TaskItem task = await ApiTestDataHelper.SeedTaskItemAsync(
                _factory,
                user.Id,
                "Old Title",
                DateTime.UtcNow.AddDays(1),
                description: "Old description",
                status: Enums.TaskStatus.notStarted,
                priority: Enums.PriorityLevel.Low,
                categoryId: oldCategory.Id,
                tagIds: new List<int> { oldTag.Id });
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");

            TaskItemUpdateDto updateDto = new TaskItemUpdateDto
            {
                Title = "Updated Title",
                Description = "Updated description",
                Status = Enums.TaskStatus.completed,
                Priority = Enums.PriorityLevel.High,
                DueDate = DateTime.UtcNow.AddDays(7),
                CategoryId = newCategory.Id,
                TagIds = new List<int> { newTag1.Id, newTag2.Id },
                ParentTaskId = parentTask.Id,
                SubTaskIds = new List<int> { newSubTask1.Id, newSubTask2.Id }
            };

            HttpResponseMessage response = await _client.PatchAsJsonAsync($"/api/taskitem/{task.Id}", updateDto);
            TaskItemReadDto? updatedTask = await response.Content.ReadFromJsonAsync<TaskItemReadDto>(ApiTestDataHelper.JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(updatedTask);
            Assert.Equal(task.Id, updatedTask.Id);
            Assert.Equal(updateDto.Title, updatedTask.Title);
            Assert.Equal(updateDto.Description, updatedTask.Description);
            Assert.Equal(updateDto.Status, updatedTask.Status);
            Assert.Equal(updateDto.Priority, updatedTask.Priority);
            Assert.Equal(updateDto.DueDate, updatedTask.DueDate);
            Assert.Equal(updateDto.CategoryId, updatedTask.CategoryId);
            Assert.Equal(updateDto.TagIds, updatedTask.TagIds);
            Assert.Equal(updateDto.ParentTaskId, updatedTask.ParentTaskId);
            Assert.Equal(updateDto.SubTaskIds, updatedTask.SubTaskIds);
            Assert.DoesNotContain(oldTag.Id, updatedTask.TagIds);
            Assert.DoesNotContain(oldSubTask.Id, updatedTask.SubTaskIds);
        }

        [Fact]
        public async Task UpdateTaskItem_WithPartialUpdate_PreservesUnspecifiedFields()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            Category category = await ApiTestDataHelper.SeedCategoryAsync(_factory, user.Id, "Category");
            Tag tag = await ApiTestDataHelper.SeedTagAsync(_factory, user.Id, "Tag");
            TaskItem parentTask = await ApiTestDataHelper.SeedTaskItemAsync(_factory, user.Id, "Parent Task");
            DateTime dueDate = DateTime.UtcNow.AddDays(3);
            TaskItem task = await ApiTestDataHelper.SeedTaskItemAsync(
                _factory,
                user.Id,
                "Original Title",
                dueDate,
                description: "Original description",
                status: Enums.TaskStatus.inProgress,
                priority: Enums.PriorityLevel.High,
                categoryId: category.Id,
                parentTaskId: parentTask.Id,
                tagIds: new List<int> { tag.Id });
            TaskItem subTask = await ApiTestDataHelper.SeedTaskItemAsync(
                _factory,
                user.Id,
                "Subtask",
                parentTaskId: task.Id);
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");

            Dictionary<string, string> updateDto = new Dictionary<string, string>
            {
                ["Title"] = "Updated Title"
            };

            HttpResponseMessage response = await _client.PatchAsJsonAsync($"/api/taskitem/{task.Id}", updateDto);
            TaskItemReadDto? updatedTask = await response.Content.ReadFromJsonAsync<TaskItemReadDto>(ApiTestDataHelper.JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(updatedTask);
            Assert.Equal(updateDto["Title"], updatedTask.Title);
            Assert.Equal(task.Description, updatedTask.Description);
            Assert.Equal(task.Status, updatedTask.Status);
            Assert.Equal(task.Priority, updatedTask.Priority);
            Assert.NotNull(updatedTask.DueDate);
            Assert.Equal(dueDate, updatedTask.DueDate.Value, TimeSpan.FromMicroseconds(1));
            Assert.Equal(category.Id, updatedTask.CategoryId);
            Assert.Equal(tag.Id, Assert.Single(updatedTask.TagIds));
            Assert.Equal(parentTask.Id, updatedTask.ParentTaskId);
            Assert.Contains(subTask.Id, updatedTask.SubTaskIds);
        }

        [Fact]
        public async Task UpdateTaskItem_WithoutAuth_Returns401Unauthorized()
        {
            HttpResponseMessage response = await _client.PatchAsJsonAsync("/api/taskitem/1", new { Title = "Updated Title" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("missing")]
        [InlineData("owner")]
        public async Task UpdateTaskItem_WithInvalidTaskId_Returns404NotFound(string idSource)
        {
            User owner = await ApiTestDataHelper.SeedTestUserAsync(_factory, "owner", "owner@mail.com", "Password123!");
            await ApiTestDataHelper.SeedTestUserAsync(_factory, "requester", "requester@mail.com", "requester@mail.com");
            TaskItem? ownerTask = idSource == "owner"
                ? await ApiTestDataHelper.SeedTaskItemAsync(_factory, owner.Id, "Owner Task")
                : null;
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "requester@mail.com", "requester@mail.com");

            int taskId = ownerTask?.Id ?? 999999;
            HttpResponseMessage response = await _client.PatchAsJsonAsync($"/api/taskitem/{taskId}", new { Title = "Updated Title" });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("category", "missing")]
        [InlineData("category", "owner")]
        [InlineData("parent", "missing")]
        [InlineData("parent", "owner")]
        [InlineData("tag", "missing")]
        [InlineData("tag", "owner")]
        [InlineData("subtask", "missing")]
        [InlineData("subtask", "owner")]
        public async Task UpdateTaskItem_WithInvalidRelationshipIds_Returns404NotFound(string relationshipType, string idSource)
        {
            User owner = await ApiTestDataHelper.SeedTestUserAsync(_factory, "owner", "owner@mail.com", "Password123!");
            User requester = await ApiTestDataHelper.SeedTestUserAsync(_factory, "requester", "requester@mail.com", "requester@mail.com");
            Category? ownerCategory = null;
            TaskItem? ownerTask = null;
            Tag? ownerTag = null;
            TaskItem? ownerSubTask = null;

            if (relationshipType == "category")
            {
                ownerCategory = await ApiTestDataHelper.SeedCategoryAsync(_factory, owner.Id, "Owner Category");
            }
            else if (relationshipType == "parent")
            {
                ownerTask = await ApiTestDataHelper.SeedTaskItemAsync(_factory, owner.Id, "Owner Parent");
            }
            else if (relationshipType == "tag")
            {
                ownerTag = await ApiTestDataHelper.SeedTagAsync(_factory, owner.Id, "Owner Tag");
            }
            else
            {
                ownerSubTask = await ApiTestDataHelper.SeedTaskItemAsync(_factory, owner.Id, "Owner Subtask");
            }

            TaskItem task = await ApiTestDataHelper.SeedTaskItemAsync(_factory, requester.Id, "Task");
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "requester@mail.com", "requester@mail.com");

            TaskItemUpdateDto updateDto = new TaskItemUpdateDto
            {
                CategoryId = relationshipType == "category" ? idSource == "owner" ? ownerCategory!.Id : 999999 : (int?)null,
                ParentTaskId = relationshipType == "parent" ? idSource == "owner" ? ownerTask!.Id : 999999 : (int?)null,
                TagIds = relationshipType == "tag" ? new List<int> { idSource == "owner" ? ownerTag!.Id : 999999 } : null,
                SubTaskIds = relationshipType == "subtask" ? new List<int> { idSource == "owner" ? ownerSubTask!.Id : 999999 } : null
            };

            HttpResponseMessage response = await _client.PatchAsJsonAsync($"/api/taskitem/{task.Id}", updateDto);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("parent")]
        [InlineData("subtask")]
        public async Task UpdateTaskItem_WithSelfReference_Returns400BadRequest(string relationshipType)
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            TaskItem task = await ApiTestDataHelper.SeedTaskItemAsync(_factory, user.Id, "Task");
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");

            TaskItemUpdateDto updateDto = new TaskItemUpdateDto
            {
                ParentTaskId = relationshipType == "parent" ? task.Id : (int?)null,
                SubTaskIds = relationshipType == "subtask" ? new List<int> { task.Id } : null
            };

            HttpResponseMessage response = await _client.PatchAsJsonAsync($"/api/taskitem/{task.Id}", updateDto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData("tag")]
        [InlineData("subtask")]
        public async Task UpdateTaskItem_WithDuplicateRelationshipIds_Returns400BadRequest(string relationshipType)
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            Tag? tag = relationshipType == "tag"
                ? await ApiTestDataHelper.SeedTagAsync(_factory, user.Id, "Tag")
                : null;
            TaskItem? subTask = relationshipType == "subtask"
                ? await ApiTestDataHelper.SeedTaskItemAsync(_factory, user.Id, "Subtask")
                : null;
            TaskItem task = await ApiTestDataHelper.SeedTaskItemAsync(_factory, user.Id, "Task");
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");

            TaskItemUpdateDto updateDto = new TaskItemUpdateDto
            {
                TagIds = relationshipType == "tag" ? new List<int> { tag!.Id, tag.Id } : null,
                SubTaskIds = relationshipType == "subtask" ? new List<int> { subTask!.Id, subTask.Id } : null
            };

            HttpResponseMessage response = await _client.PatchAsJsonAsync($"/api/taskitem/{task.Id}", updateDto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("   ")]
        [InlineData("\t")]
        public async Task UpdateTaskItem_WithWhitespaceOnlyTitle_Returns400BadRequest(string title)
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            TaskItem task = await ApiTestDataHelper.SeedTaskItemAsync(_factory, user.Id, "Original Title");
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");

            TaskItemUpdateDto updateDto = new TaskItemUpdateDto
            {
                Title = title
            };

            HttpResponseMessage response = await _client.PatchAsJsonAsync($"/api/taskitem/{task.Id}", updateDto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData("category")]
        [InlineData("parent")]
        [InlineData("dueDate")]
        [InlineData("description")]
        public async Task UpdateTaskItem_WithExplicitNull_ClearsField(string fieldName)
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            Category category = await ApiTestDataHelper.SeedCategoryAsync(_factory, user.Id, "Category");
            TaskItem parentTask = await ApiTestDataHelper.SeedTaskItemAsync(_factory, user.Id, "Parent Task");
            DateTime dueDate = DateTime.UtcNow.AddDays(3);
            TaskItem task = await ApiTestDataHelper.SeedTaskItemAsync(
                _factory,
                user.Id,
                "Task",
                dueDate,
                description: "Description",
                categoryId: category.Id,
                parentTaskId: parentTask.Id);
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");

            Dictionary<string, object?> updateDto = new Dictionary<string, object?>
            {
                [fieldName switch
                {
                    "category" => "CategoryId",
                    "parent" => "ParentTaskId",
                    "dueDate" => "DueDate",
                    _ => "Description"
                }] = null
            };

            HttpResponseMessage response = await _client.PatchAsJsonAsync($"/api/taskitem/{task.Id}", updateDto);
            TaskItemReadDto? updatedTask = await response.Content.ReadFromJsonAsync<TaskItemReadDto>(ApiTestDataHelper.JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(updatedTask);

            switch (fieldName)
            {
                case "category":
                    Assert.Null(updatedTask.CategoryId);
                    break;
                case "parent":
                    Assert.Null(updatedTask.ParentTaskId);
                    break;
                case "dueDate":
                    Assert.Null(updatedTask.DueDate);
                    break;
                case "description":
                    Assert.Null(updatedTask.Description);
                    break;
            }
        }

        [Fact]
        public async Task DeleteTaskItem_WithOwnedTask_Returns204AndSubsequentGetReturns404()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            TaskItem task = await ApiTestDataHelper.SeedTaskItemAsync(_factory, user.Id, "Task");
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");

            HttpResponseMessage deleteResponse = await _client.DeleteAsync($"/api/taskitem/{task.Id}");

            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            HttpResponseMessage getResponse = await _client.GetAsync($"/api/taskitem/{task.Id}");

            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteTaskItem_WithoutAuth_Returns401Unauthorized()
        {
            HttpResponseMessage response = await _client.DeleteAsync("/api/taskitem/1");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("missing")]
        [InlineData("owner")]
        public async Task DeleteTaskItem_WithInvalidTaskId_Returns404NotFound(string idSource)
        {
            User owner = await ApiTestDataHelper.SeedTestUserAsync(_factory, "owner", "owner@mail.com", "Password123!");
            await ApiTestDataHelper.SeedTestUserAsync(_factory, "requester", "requester@mail.com", "requester@mail.com");
            TaskItem? ownerTask = idSource == "owner"
                ? await ApiTestDataHelper.SeedTaskItemAsync(_factory, owner.Id, "Owner Task")
                : null;
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "requester@mail.com", "requester@mail.com");

            int taskId = ownerTask?.Id ?? 999999;
            HttpResponseMessage response = await _client.DeleteAsync($"/api/taskitem/{taskId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task RemoveTagFromTaskItem_WithAssociatedOwnedTag_Returns204AndTagIsAbsentFromGetResponse()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            Tag tag = await ApiTestDataHelper.SeedTagAsync(_factory, user.Id, "Tag");
            TaskItem task = await ApiTestDataHelper.SeedTaskItemAsync(
                _factory,
                user.Id,
                "Task",
                tagIds: new List<int> { tag.Id });
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");

            HttpResponseMessage removeResponse = await _client.DeleteAsync($"/api/taskitem/{task.Id}/tags/{tag.Id}");

            Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);

            HttpResponseMessage getResponse = await _client.GetAsync($"/api/taskitem/{task.Id}/tags");
            List<TagReadDto>? tags = await getResponse.Content.ReadFromJsonAsync<List<TagReadDto>>(ApiTestDataHelper.JsonOptions);

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.NotNull(tags);
            Assert.DoesNotContain(tags, returnedTag => returnedTag.Id == tag.Id);
        }

        [Fact]
        public async Task RemoveTagFromTaskItem_WithoutAuth_Returns401Unauthorized()
        {
            HttpResponseMessage response = await _client.DeleteAsync("/api/taskitem/1/tags/1");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("task", "missing")]
        [InlineData("task", "owner")]
        [InlineData("tag", "missing")]
        [InlineData("tag", "owner")]
        public async Task RemoveTagFromTaskItem_WithInvalidTaskOrTagId_Returns404NotFound(string resourceType, string idSource)
        {
            User owner = await ApiTestDataHelper.SeedTestUserAsync(_factory, "owner", "owner@mail.com", "Password123!");
            await ApiTestDataHelper.SeedTestUserAsync(_factory, "requester", "requester@mail.com", "requester@mail.com");
            TaskItem? ownerTask = null;
            Tag? ownerTag = null;

            if (resourceType == "task")
            {
                ownerTask = await ApiTestDataHelper.SeedTaskItemAsync(_factory, owner.Id, "Owner Task");
            }
            else
            {
                ownerTag = await ApiTestDataHelper.SeedTagAsync(_factory, owner.Id, "Owner Tag");
            }

            await ApiTestDataHelper.AuthenticateClientAsync(_client, "requester@mail.com", "requester@mail.com");

            int taskId = resourceType == "task" && idSource == "owner" ? ownerTask!.Id : 999999;
            int tagId = resourceType == "tag" && idSource == "owner" ? ownerTag!.Id : 999999;
            HttpResponseMessage response = await _client.DeleteAsync($"/api/taskitem/{taskId}/tags/{tagId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task RemoveTagFromTaskItem_WithUnassociatedTag_Returns400BadRequest()
        {
            User user = await ApiTestDataHelper.SeedTestUserAsync(_factory, "user1", "user1@mail.com", "Password123!");
            TaskItem task = await ApiTestDataHelper.SeedTaskItemAsync(_factory, user.Id, "Task");
            Tag tag = await ApiTestDataHelper.SeedTagAsync(_factory, user.Id, "Tag");
            await ApiTestDataHelper.AuthenticateClientAsync(_client, "user1@mail.com", "Password123!");

            HttpResponseMessage response = await _client.DeleteAsync($"/api/taskitem/{task.Id}/tags/{tag.Id}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}