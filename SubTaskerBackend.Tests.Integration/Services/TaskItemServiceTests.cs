using Microsoft.AspNetCore.Http;
using SubTaskerBackend.Data;
using SubTaskerBackend.DTOs.TaskItems;
using SubTaskerBackend.Exceptions;
using SubTaskerBackend.Models;
using SubTaskerBackend.Services;
using SubTaskerBackend.Tests.Integration.Fixtures;
using SubTaskerBackend.Tests.Integration.Helpers;
using SubTaskerBackend.Enums;

namespace SubTaskerBackend.Tests.Integration.Services
{
	public class TaskItemServiceTests : IClassFixture<PostgresFixture>, IAsyncLifetime
	{
        private readonly PostgresFixture _postgresFixture;

        private SubTaskerEfCoreDbContext _dbContext = null!;
        private IHttpContextAccessor _httpContextAccessor = null!;
        private TaskItemService _taskItemService = null!;

        public TaskItemServiceTests(PostgresFixture postgresFixture)
        {
            _postgresFixture = postgresFixture;
        }

        public async Task InitializeAsync()
        {
            await _postgresFixture.ResetDatabaseAsync();

            _dbContext = _postgresFixture.CreateDbContext();
            _httpContextAccessor = new HttpContextAccessor();
            _taskItemService = new TaskItemService(_dbContext, _httpContextAccessor);
        }

        public async Task DisposeAsync()
        {
            if (_dbContext is not null)
            {
                await _dbContext.DisposeAsync();
            }
        }

        // GetAllTaskItems and associated tests
        [Fact]
        public async Task GetAllTaskItems_WithMixedUsers_ReturnsOnlyCurrentUsersTaskItemsOrderedByDueDate()
        {
            User currentUser = await TestDataHelper.SeedTestUserAsync(_dbContext, "currentuser", "current@mail.com");
            User otherUser = await TestDataHelper.SeedTestUserAsync(_dbContext, "otheruser", "other@mail.com");

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, currentUser.Id);

            await TestDataHelper.SeedTaskItemAsync(_dbContext, currentUser.Id, "Task 1");
            await TestDataHelper.SeedTaskItemAsync(_dbContext, currentUser.Id, "Task 2");
            await TestDataHelper.SeedTaskItemAsync(_dbContext, otherUser.Id, "Task 3");

            List<TaskItem> result = await _taskItemService.GetAllTaskItemsAsync();

            Assert.Equal(2, result.Count);
            Assert.All(result, taskItem => Assert.Equal(currentUser.Id, taskItem.UserId));
            Assert.Equal(new[] { "Task 1", "Task 2" }, result.Select(taskItem => taskItem.Title));
        }

        [Fact]
        public async Task GetAllTaskItems_WithNoTaskItems_ReturnsEmptyList()
        {
            User currentUser = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, currentUser.Id);

            List<TaskItem> result = await _taskItemService.GetAllTaskItemsAsync();
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllTaskItems_WithTaskItemsHavingTagsAndSubTasks_ReturnsTaskItemsWithLoadedRelations()
        {
            User currentUser = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, currentUser.Id);

            TaskItem parentTask = await TestDataHelper.SeedTaskItemAsync(_dbContext, currentUser.Id, "Parent Task");
            TaskItem subTask = await TestDataHelper.SeedSubTaskItemAsync(_dbContext, currentUser.Id, "Sub Task", parentTask.Id);
            Tag tag = await TestDataHelper.SeedTagAsync(_dbContext, currentUser.Id, "Urgent");

            parentTask.Tags.Add(tag);
            await _dbContext.SaveChangesAsync();

            List<TaskItem> result = await _taskItemService.GetAllTaskItemsAsync();

            Assert.Equal(2, result.Count);
            TaskItem retrievedParentTask = result.First();
            Assert.Equal(parentTask.Id, retrievedParentTask.Id);
            Assert.Single(retrievedParentTask.SubTasks);
            Assert.Equal(subTask.Id, retrievedParentTask.SubTasks.First().Id);
            Assert.Single(retrievedParentTask.Tags);
            Assert.Equal(tag.Id, retrievedParentTask.Tags.First().Id);
        }

        // GetTagsByTaskItemId and associated tests
        [Fact]
        public async Task GetTagsByTaskItemId_WithOwnedTaskItem_ReturnsAssociatedTags()
        {
            User currentUser = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, currentUser.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, currentUser.Id, "Task with Tags");
            Tag tag = await TestDataHelper.SeedTagAsync(_dbContext, currentUser.Id, "Urgent");

            taskItem.Tags.Add(tag);
            await _dbContext.SaveChangesAsync();

            List<Tag> result = await _taskItemService.GetTagsByTaskItemIdAsync(taskItem.Id);
            Assert.Single(result);
            Assert.Equal(tag.Id, result.First().Id);
        }
        [Fact]
        public async Task TaskGetTagsByTaskItemId_WithTaskItemThatHasNoTags_ReturnsEmptyList()
        {
            User currentUser = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, currentUser.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, currentUser.Id, "Task with No Tags");

            List<Tag> result = await _taskItemService.GetTagsByTaskItemIdAsync(taskItem.Id);
            Assert.Empty(result);
        }
        [Fact]
        public async Task GetTagsByTaskItemId_WithDifferentUsersTaskItem_ThrowsNotFoundException()
        {
            User currentUser = await TestDataHelper.SeedTestUserAsync(_dbContext);
            User differentUser = await TestDataHelper.SeedTestUserAsync(_dbContext, "testuser2", "testuser2@example.com");

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, currentUser.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, differentUser.Id, "Task with Different User");

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _taskItemService.GetTagsByTaskItemIdAsync(taskItem.Id);
            });
        }
        [Fact]
        public async Task GetTagsByTaskItemId_WithMissingTaskItem_ThrowsNotFoundException()
        {
            User currentUser = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, currentUser.Id);

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _taskItemService.GetTagsByTaskItemIdAsync(9999); // Assuming 9999 is a non-existent TaskItemId
            });
        }

        // GetTaskItemById and associated tests
        [Fact]
        public async Task GetTaskItemById_WithOwnedTaskItem_ReturnsTaskItem()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Owned Task");

            TaskItem result = await _taskItemService.GetTaskItemByIdAsync(taskItem.Id);
            Assert.NotNull(result);
            Assert.Equal(taskItem.Id, result.Id);
        }

        [Fact]
        public async Task GetTaskItemById_WithTaskItemThatHasTagsAndSubTasks_ReturnsTaskItemWithRelations()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Task with Relations");
            Tag tag = await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Sample Tag");
            taskItem.Tags.Add(tag);
            TaskItem subTask = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Sub Task");
            taskItem.SubTasks.Add(subTask);

            TaskItem result = await _taskItemService.GetTaskItemByIdAsync(taskItem.Id);
            Assert.NotNull(result);
            Assert.Equal(taskItem.Id, result.Id);
            Assert.NotEmpty(result.Tags);
            Assert.NotEmpty(result.SubTasks);
            Assert.Contains(result.Tags, t => t.Id == tag.Id);
            Assert.Contains(result.SubTasks, st => st.Id == subTask.Id);
        }

        [Fact]
        public async Task GetTaskItemById_WithDifferentUsersTaskItem_ThrowsNotFoundException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            User differentUser = await TestDataHelper.SeedTestUserAsync(_dbContext, "testuser2", "testuser2@example.com");
            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, differentUser.Id, "Different User's Task");

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _taskItemService.GetTaskItemByIdAsync(taskItem.Id);
            });
        }

        [Fact]
        public async Task GetTaskItemById_WithMissingTaskItem_ThrowsNotFoundException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _taskItemService.GetTaskItemByIdAsync(9999); // Non-existent task item ID
            });
        }

        // CreateTaskItem and associated tests
        [Fact]
        public async Task CreateTaskItem_WithValidDto_CreatesTaskItemForCurrentUser()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItemWriteDto taskItemWriteDto = new TaskItemWriteDto
            {
                Title = "New Task",
                Description = "Task Description",
                Status = Enums.TaskStatus.notStarted,
                Priority = PriorityLevel.Medium,
                DueDate = DateTime.UtcNow,
                CategoryId = null,
                ParentTaskId = null,
                TagIds = new List<int>()
            };

            TaskItem createdTaskItem = await _taskItemService.CreateTaskItemAsync(taskItemWriteDto);

            Assert.NotNull(createdTaskItem);
            Assert.Equal(taskItemWriteDto.Title, createdTaskItem.Title);
            Assert.Equal(taskItemWriteDto.Description, createdTaskItem.Description);
            Assert.Equal(taskItemWriteDto.Status, createdTaskItem.Status);
            Assert.Equal(taskItemWriteDto.Priority, createdTaskItem.Priority);
            Assert.Equal(taskItemWriteDto.DueDate, createdTaskItem.DueDate);
            Assert.Equal(taskItemWriteDto.CategoryId, createdTaskItem.CategoryId);
            Assert.Equal(taskItemWriteDto.ParentTaskId, createdTaskItem.ParentTaskId);
            Assert.Empty(createdTaskItem.Tags);
            Assert.Equal(user.Id, createdTaskItem.UserId);
        }

        [Fact]
        public async Task CreateTaskItem_WithWhitespaceTitle_ThrowsBadRequestException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItemWriteDto taskItemWriteDto = new TaskItemWriteDto
            {
                Title = "     ",
                Description = "Task Description",
                Status = Enums.TaskStatus.notStarted,
                Priority = PriorityLevel.Medium,
                DueDate = DateTime.UtcNow,
                CategoryId = null,
                ParentTaskId = null,
                TagIds = new List<int>()
            };

            await Assert.ThrowsAsync<BadRequestException>(async () =>
            {
                await _taskItemService.CreateTaskItemAsync(taskItemWriteDto);
            });
        }

        [Fact]
        public async Task CreateTaskItem_WithValidCategoryId_AssignsCategory()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            Category category = await TestDataHelper.SeedCategoryAsync(_dbContext, user.Id, "Test Category");

            TaskItemWriteDto taskItemWriteDto = new TaskItemWriteDto
            {
                Title = "New Task",
                Description = "Task Description",
                Status = Enums.TaskStatus.notStarted,
                Priority = PriorityLevel.Medium,
                DueDate = DateTime.UtcNow,
                CategoryId = category.Id,
                ParentTaskId = null,
                TagIds = new List<int>()
            };

            TaskItem createdTaskItem = await _taskItemService.CreateTaskItemAsync(taskItemWriteDto);

            Assert.Equal(category.Id, createdTaskItem.CategoryId);
        }

        [Fact]
        public async Task CreateTaskItem_WithInvalidCategoryId_ThrowsNotFoundException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItemWriteDto taskItemWriteDto = new TaskItemWriteDto
            {
                Title = "New Task",
                Description = "Task Description",
                Status = Enums.TaskStatus.notStarted,
                Priority = PriorityLevel.Medium,
                DueDate = DateTime.UtcNow,
                CategoryId = 999, // Assuming 999 is an invalid category ID
                ParentTaskId = null,
                TagIds = new List<int>()
            };

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _taskItemService.CreateTaskItemAsync(taskItemWriteDto);
            });
        }

        [Fact]
        public async Task CreateTaskItem_WithValidParentTaskId_AssignsParentTask()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem parentTask = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Parent Task");

            TaskItemWriteDto taskItemWriteDto = new TaskItemWriteDto
            {
                Title = "New Task",
                Description = "Task Description",
                Status = Enums.TaskStatus.notStarted,
                Priority = PriorityLevel.Medium,
                DueDate = DateTime.UtcNow,
                CategoryId = null,
                ParentTaskId = parentTask.Id,
                TagIds = new List<int>()
            };

            TaskItem createdTaskItem = await _taskItemService.CreateTaskItemAsync(taskItemWriteDto);

            Assert.Equal(parentTask.Id, createdTaskItem.ParentTaskId);
        }

        [Fact]
        public async Task CreateTaskItem_WithInvalidParentTaskId_ThrowsNotFoundException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItemWriteDto taskItemWriteDto = new TaskItemWriteDto
            {
                Title = "New Task",
                Description = "Task Description",
                Status = Enums.TaskStatus.notStarted,
                Priority = PriorityLevel.Medium,
                DueDate = DateTime.UtcNow,
                CategoryId = null,
                ParentTaskId = 999, // Assuming 999 is an invalid parent task ID
                TagIds = new List<int>()
            };

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _taskItemService.CreateTaskItemAsync(taskItemWriteDto);
            });
        }

        [Fact]
        public async Task CreateTaskItem_WithValidTagIds_AssignsTags()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            Tag tag1 = await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Tag 1");
            Tag tag2 = await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Tag 2");
            Tag tag3 = await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Tag 3");


            TaskItemWriteDto taskItemWriteDto = new TaskItemWriteDto
            {
                Title = "New Task",
                Description = "Task Description",
                Status = Enums.TaskStatus.notStarted,
                Priority = PriorityLevel.Medium,
                DueDate = DateTime.UtcNow,
                CategoryId = null,
                ParentTaskId = null,
                TagIds = new List<int> { tag1.Id, tag2.Id, tag3.Id }
            };

            TaskItem createdTaskItem = await _taskItemService.CreateTaskItemAsync(taskItemWriteDto);

            Assert.Equal(3, createdTaskItem.Tags.Count);
            Assert.Contains(createdTaskItem.Tags, t => t.Id == tag1.Id);
            Assert.Contains(createdTaskItem.Tags, t => t.Id == tag2.Id);
            Assert.Contains(createdTaskItem.Tags, t => t.Id == tag3.Id);
        }

        [Fact]
        public async Task CreateTaskItem_WithInvalidTagIds_ThrowsNotFoundException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItemWriteDto taskItemWriteDto = new TaskItemWriteDto
            {
                Title = "New Task",
                Description = "Task Description",
                Status = Enums.TaskStatus.notStarted,
                Priority = PriorityLevel.Medium,
                DueDate = DateTime.UtcNow,
                CategoryId = null,
                ParentTaskId = null,
                TagIds = new List<int> { 999 } // Assuming 999 is an invalid tag ID
            };

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _taskItemService.CreateTaskItemAsync(taskItemWriteDto);
            });
        }

        /*
        CreateTaskItem_WithDuplicateTagIds_ThrowsBadRequestException
        CreateTaskItem_WithMixedUserTagIds_ThrowsNotFoundException
        CreateTaskItem_WithExplicitStatusAndPriority_UsesProvidedValues
        CreateTaskItem_WithoutStatusOrPriority_UsesDefaultValues
        */

        // AddTagToTaskItem and associated tests
        /*
        AddTagToTaskItem_WithValidTaskAndTag_AddsTagToTaskItem
        AddTagToTaskItem_WithDifferentUsersTaskItem_ThrowsNotFoundException
        AddTagToTaskItem_WithMissingTaskItem_ThrowsNotFoundException
        AddTagToTaskItem_WithInvalidTagId_ThrowsNotFoundException
        AddTagToTaskItem_WithAlreadyAssociatedTag_ThrowsBadRequestException
        */

        // UpdateTaskItem and associated tests
        /*
        UpdateTaskItem_WithValidDto_UpdatesTaskItem
        UpdateTaskItem_WithPartialDto_UpdatesOnlyProvidedFields
        UpdateTaskItem_WithInvalidTaskItemId_ThrowsNotFoundException
        UpdateTaskItem_WithInvalidCategoryId_ThrowsNotFoundException
        UpdateTaskItem_WithInvalidParentTaskId_ThrowsNotFoundException
        UpdateTaskItem_WithSelfParentTaskId_ThrowsBadRequestException
        UpdateTaskItem_WithDuplicateTagIds_ThrowsBadRequestException
        UpdateTaskItem_WithInvalidTagIds_ThrowsNotFoundException
        UpdateTaskItem_WithDuplicateSubTaskIds_ThrowsBadRequestException
        UpdateTaskItem_WithSelfInSubTaskIds_ThrowsBadRequestException
        UpdateTaskItem_WithInvalidSubTaskIds_ThrowsNotFoundException
        UpdateTaskItem_WithTagIds_ReplacesExistingTags
        UpdateTaskItem_WithSubTaskIds_ReplacesExistingSubTasks
        UpdateTaskItem_WithNullTagIdsAndSubTaskIds_KeepsExistingRelations
        */

        // DeleteTaskItem and associated tests
        /*
        DeleteTaskItem_WithOwnedTaskItem_DeletesTaskItem
        DeleteTaskItem_WithInvalidTaskItemId_ThrowsNotFoundException
        DeleteTaskItem_WithDifferentUsersTaskItem_ThrowsNotFoundException
        DeleteTaskItem_WithSubTasksOrTags_DeletesTaskItemAndCleansUpRelations
        */

        // RemoveTagFromTaskItem and associated tests
        /*
        RemoveTagFromTaskItem_WithAssociatedTag_RemovesTagFromTaskItem
        RemoveTagFromTaskItem_WithTaskItemThatDoesNotHaveTag_ThrowsBadRequestException
        RemoveTagFromTaskItem_WithInvalidTaskItemId_ThrowsNotFoundException
        RemoveTagFromTaskItem_WithInvalidTagId_ThrowsNotFoundException
        RemoveTagFromTaskItem_WithDifferentUsersTaskItem_ThrowsNotFoundException
        RemoveTagFromTaskItem_WithDifferentUsersTag_ThrowsNotFoundException
        */
    }
}