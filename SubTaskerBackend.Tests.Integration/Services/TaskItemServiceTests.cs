using Microsoft.AspNetCore.Http;
using SubTaskerBackend.Data;
using SubTaskerBackend.DTOs.TaskItems;
using SubTaskerBackend.Exceptions;
using SubTaskerBackend.Models;
using SubTaskerBackend.Services;
using SubTaskerBackend.Tests.Integration.Fixtures;
using SubTaskerBackend.Tests.Integration.Helpers;
using SubTaskerBackend.Enums;
using Microsoft.EntityFrameworkCore;

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

        [Fact]
        public async Task CreateTaskItem_WithDuplicateTagIds_ThrowsBadRequestException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            Tag tag = await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Duplicate Tag");

            TaskItemWriteDto taskItemWriteDto = new TaskItemWriteDto
            {
                Title = "Task with Duplicate Tags",
                TagIds = new List<int> { tag.Id, tag.Id }
            };

            await Assert.ThrowsAsync<BadRequestException>(async () =>
            {
                await _taskItemService.CreateTaskItemAsync(taskItemWriteDto);
            });
        }

        [Fact]
        public async Task CreateTaskItem_WithMixedUserTagIds_ThrowsNotFoundException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);
            User differentUser = await TestDataHelper.SeedTestUserAsync(
                _dbContext,
                "differentuser",
                "differentuser@example.com");

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            Tag userTag = await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Owned Tag");
            Tag differentUserTag = await TestDataHelper.SeedTagAsync(
                _dbContext,
                differentUser.Id,
                "Different User Tag");

            TaskItemWriteDto taskItemWriteDto = new TaskItemWriteDto
            {
                Title = "Task with Mixed User Tags",
                TagIds = new List<int> { userTag.Id, differentUserTag.Id }
            };

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _taskItemService.CreateTaskItemAsync(taskItemWriteDto);
            });
        }

        [Fact]
        public async Task CreateTaskItem_WithExplicitStatusAndPriority_UsesProvidedValues()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItemWriteDto taskItemWriteDto = new TaskItemWriteDto
            {
                Title = "Explicit Status and Priority",
                Status = Enums.TaskStatus.completed,
                Priority = PriorityLevel.Critical
            };

            TaskItem createdTaskItem = await _taskItemService.CreateTaskItemAsync(taskItemWriteDto);

            Assert.Equal(Enums.TaskStatus.completed, createdTaskItem.Status);
            Assert.Equal(PriorityLevel.Critical, createdTaskItem.Priority);
        }

        [Fact]
        public async Task CreateTaskItem_WithoutStatusOrPriority_UsesDefaultValues()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItemWriteDto taskItemWriteDto = new TaskItemWriteDto
            {
                Title = "Default Status and Priority"
            };

            TaskItem createdTaskItem = await _taskItemService.CreateTaskItemAsync(taskItemWriteDto);

            Assert.Equal(Enums.TaskStatus.notStarted, createdTaskItem.Status);
            Assert.Equal(PriorityLevel.Medium, createdTaskItem.Priority);
        }

        // AddTagToTaskItem and associated tests
        [Fact]
        public async Task AddTagToTaskItem_WithValidTaskAndTag_AddsTagToTaskItem()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Test Task");
            Tag tag = await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Test Tag");

            await _taskItemService.AddTagToTaskItemAsync(taskItem.Id, tag.Id);

            TaskItem updatedTaskItem = await _dbContext.TaskItems
                .Include(t => t.Tags)
                .SingleAsync(t => t.Id == taskItem.Id);

            Assert.Contains(updatedTaskItem.Tags, t => t.Id == tag.Id);
        }

        [Fact]
        public async Task AddTagToTaskItem_WithDifferentUsersTaskItem_ThrowsNotFoundException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);
            User otherUser = await TestDataHelper.SeedTestUserAsync(_dbContext, "OtherUser", "otheruser@example.com");

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, otherUser.Id, "Test Task");
            Tag tag = await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Test Tag");

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _taskItemService.AddTagToTaskItemAsync(taskItem.Id, tag.Id);
            });
        }

        [Fact]
        public async Task AddTagToTaskItem_WithMissingTaskItem_ThrowsNotFoundException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            Tag tag = await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Test Tag");

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _taskItemService.AddTagToTaskItemAsync(9999, tag.Id);
            });
        }

        [Fact]
        public async Task AddTagToTaskItem_WithInvalidTagId_ThrowsNotFoundException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Test Task");

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _taskItemService.AddTagToTaskItemAsync(taskItem.Id, 9999);
            });
        }

        [Fact]
        public async Task AddTagToTaskItem_WithAlreadyAssociatedTag_ThrowsBadRequestException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Test Task");
            Tag tag = await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Test Tag");

            await _taskItemService.AddTagToTaskItemAsync(taskItem.Id, tag.Id);

            await Assert.ThrowsAsync<BadRequestException>(async () =>
            {
                await _taskItemService.AddTagToTaskItemAsync(taskItem.Id, tag.Id);
            });
        }

        // UpdateTaskItem and associated tests
        [Fact]
        public async Task UpdateTaskItem_WithValidDto_UpdatesTaskItem()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Original Task");
            Category category = await TestDataHelper.SeedCategoryAsync(_dbContext, user.Id, "Updated Category");
            Tag tag = await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Updated Tag");
            DateTime dueDate = DateTime.UtcNow.AddDays(2);

            TaskItemUpdateDto taskItemUpdateDto = new TaskItemUpdateDto
            {
                Title = "Updated Task",
                Description = "Updated Description",
                Status = Enums.TaskStatus.inProgress,
                Priority = PriorityLevel.High,
                DueDate = dueDate,
                CategoryId = category.Id,
                TagIds = new List<int> { tag.Id }
            };

            TaskItem updatedTaskItem = await _taskItemService.UpdateTaskItemAsync(taskItem.Id, taskItemUpdateDto);

            Assert.Equal("Updated Task", updatedTaskItem.Title);
            Assert.Equal("Updated Description", updatedTaskItem.Description);
            Assert.Equal(Enums.TaskStatus.inProgress, updatedTaskItem.Status);
            Assert.Equal(PriorityLevel.High, updatedTaskItem.Priority);
            Assert.Equal(dueDate, updatedTaskItem.DueDate);
            Assert.Equal(category.Id, updatedTaskItem.CategoryId);
            Assert.Contains(updatedTaskItem.Tags, currentTag => currentTag.Id == tag.Id);
        }

        [Fact]
        public async Task UpdateTaskItem_WithPartialDto_UpdatesOnlyProvidedFields()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            DateTime originalDueDate = DateTime.UtcNow.AddDays(1);
            TaskItem taskItem = new TaskItem
            {
                Title = "Original Task",
                Description = "Original Description",
                Status = Enums.TaskStatus.completed,
                Priority = PriorityLevel.High,
                DueDate = originalDueDate,
                UserId = user.Id
            };
            _dbContext.TaskItems.Add(taskItem);
            await _dbContext.SaveChangesAsync();

            TaskItemUpdateDto taskItemUpdateDto = new TaskItemUpdateDto
            {
                Title = "Updated Title"
            };

            TaskItem updatedTaskItem = await _taskItemService.UpdateTaskItemAsync(taskItem.Id, taskItemUpdateDto);

            Assert.Equal("Updated Title", updatedTaskItem.Title);
            Assert.Equal("Original Description", updatedTaskItem.Description);
            Assert.Equal(Enums.TaskStatus.completed, updatedTaskItem.Status);
            Assert.Equal(PriorityLevel.High, updatedTaskItem.Priority);
            Assert.Equal(originalDueDate, updatedTaskItem.DueDate);
        }

        [Fact]
        public async Task UpdateTaskItem_WithInvalidTaskItemId_ThrowsNotFoundException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _taskItemService.UpdateTaskItemAsync(9999, new TaskItemUpdateDto { Title = "Updated Task" });
            });
        }

        [Fact]
        public async Task UpdateTaskItem_WithInvalidCategoryId_ThrowsNotFoundException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Test Task");

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _taskItemService.UpdateTaskItemAsync(
                    taskItem.Id,
                    new TaskItemUpdateDto { CategoryId = 9999 });
            });
        }

        [Fact]
        public async Task UpdateTaskItem_WithInvalidParentTaskId_ThrowsNotFoundException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Test Task");

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _taskItemService.UpdateTaskItemAsync(
                    taskItem.Id,
                    new TaskItemUpdateDto { ParentTaskId = 9999 });
            });
        }

        [Fact]
        public async Task UpdateTaskItem_WithSelfParentTaskId_ThrowsBadRequestException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Test Task");

            await Assert.ThrowsAsync<BadRequestException>(async () =>
            {
                await _taskItemService.UpdateTaskItemAsync(
                    taskItem.Id,
                    new TaskItemUpdateDto { ParentTaskId = taskItem.Id });
            });
        }

        [Fact]
        public async Task UpdateTaskItem_WithDuplicateTagIds_ThrowsBadRequestException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Test Task");
            Tag tag = await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Test Tag");

            await Assert.ThrowsAsync<BadRequestException>(async () =>
            {
                await _taskItemService.UpdateTaskItemAsync(
                    taskItem.Id,
                    new TaskItemUpdateDto { TagIds = new List<int> { tag.Id, tag.Id } });
            });
        }

        [Fact]
        public async Task UpdateTaskItem_WithInvalidTagIds_ThrowsNotFoundException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Test Task");

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _taskItemService.UpdateTaskItemAsync(
                    taskItem.Id,
                    new TaskItemUpdateDto { TagIds = new List<int> { 9999 } });
            });
        }

        [Fact]
        public async Task UpdateTaskItem_WithDuplicateSubTaskIds_ThrowsBadRequestException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Parent Task");
            TaskItem subTask = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Sub Task");

            await Assert.ThrowsAsync<BadRequestException>(async () =>
            {
                await _taskItemService.UpdateTaskItemAsync(
                    taskItem.Id,
                    new TaskItemUpdateDto { SubTaskIds = new List<int> { subTask.Id, subTask.Id } });
            });
        }

        [Fact]
        public async Task UpdateTaskItem_WithSelfInSubTaskIds_ThrowsBadRequestException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Test Task");

            await Assert.ThrowsAsync<BadRequestException>(async () =>
            {
                await _taskItemService.UpdateTaskItemAsync(
                    taskItem.Id,
                    new TaskItemUpdateDto { SubTaskIds = new List<int> { taskItem.Id } });
            });
        }

        [Fact]
        public async Task UpdateTaskItem_WithInvalidSubTaskIds_ThrowsNotFoundException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Parent Task");

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _taskItemService.UpdateTaskItemAsync(
                    taskItem.Id,
                    new TaskItemUpdateDto { SubTaskIds = new List<int> { 9999 } });
            });
        }

        [Fact]
        public async Task UpdateTaskItem_WithTagIds_ReplacesExistingTags()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Test Task");
            Tag originalTag = await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Original Tag");
            Tag replacementTag = await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Replacement Tag");
            taskItem.Tags.Add(originalTag);
            await _dbContext.SaveChangesAsync();

            await _taskItemService.UpdateTaskItemAsync(
                taskItem.Id,
                new TaskItemUpdateDto { TagIds = new List<int> { replacementTag.Id } });

            TaskItem updatedTaskItem = await _dbContext.TaskItems
                .Include(currentTask => currentTask.Tags)
                .SingleAsync(currentTask => currentTask.Id == taskItem.Id);

            Assert.DoesNotContain(updatedTaskItem.Tags, currentTag => currentTag.Id == originalTag.Id);
            Assert.Contains(updatedTaskItem.Tags, currentTag => currentTag.Id == replacementTag.Id);
        }

        [Fact]
        public async Task UpdateTaskItem_WithSubTaskIds_ReplacesExistingSubTasks()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Parent Task");
            TaskItem originalSubTask = await TestDataHelper.SeedSubTaskItemAsync(
                _dbContext,
                user.Id,
                "Original Sub Task",
                taskItem.Id);
            TaskItem replacementSubTask = await TestDataHelper.SeedTaskItemAsync(
                _dbContext,
                user.Id,
                "Replacement Sub Task");

            await _taskItemService.UpdateTaskItemAsync(
                taskItem.Id,
                new TaskItemUpdateDto { SubTaskIds = new List<int> { replacementSubTask.Id } });

            TaskItem updatedTaskItem = await _dbContext.TaskItems
                .Include(currentTask => currentTask.SubTasks)
                .SingleAsync(currentTask => currentTask.Id == taskItem.Id);

            Assert.DoesNotContain(updatedTaskItem.SubTasks, currentSubTask => currentSubTask.Id == originalSubTask.Id);
            Assert.Contains(updatedTaskItem.SubTasks, currentSubTask => currentSubTask.Id == replacementSubTask.Id);
        }

        [Fact]
        public async Task UpdateTaskItem_WithNullTagIdsAndSubTaskIds_KeepsExistingRelations()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Original Task");
            Tag tag = await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Existing Tag");
            TaskItem subTask = await TestDataHelper.SeedSubTaskItemAsync(
                _dbContext,
                user.Id,
                "Existing Sub Task",
                taskItem.Id);
            taskItem.Tags.Add(tag);
            await _dbContext.SaveChangesAsync();

            TaskItem updatedTaskItem = await _taskItemService.UpdateTaskItemAsync(
                taskItem.Id,
                new TaskItemUpdateDto { Title = "Updated Task" });

            Assert.Equal("Updated Task", updatedTaskItem.Title);
            Assert.Contains(updatedTaskItem.Tags, currentTag => currentTag.Id == tag.Id);
            Assert.Contains(updatedTaskItem.SubTasks, currentSubTask => currentSubTask.Id == subTask.Id);
        }

        // DeleteTaskItem and associated tests
        [Fact]
        public async Task DeleteTaskItem_WithOwnedTaskItem_DeletesTaskItem()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Task to Delete");

            await _taskItemService.DeleteTaskItemAsync(taskItem.Id);

            TaskItem? deletedTaskItem = await _dbContext.TaskItems
                .SingleOrDefaultAsync(currentTask => currentTask.Id == taskItem.Id);

            Assert.Null(deletedTaskItem);
        }

        [Fact]
        public async Task DeleteTaskItem_WithInvalidTaskItemId_ThrowsNotFoundException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _taskItemService.DeleteTaskItemAsync(9999);
            });
        }

        [Fact]
        public async Task DeleteTaskItem_WithDifferentUsersTaskItem_ThrowsNotFoundException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(
                _dbContext,
                "currentuser",
                "currentuser@example.com");
            User otherUser = await TestDataHelper.SeedTestUserAsync(
                _dbContext,
                "otheruser",
                "otheruser@example.com");

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, otherUser.Id, "Other User Task");

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _taskItemService.DeleteTaskItemAsync(taskItem.Id);
            });
        }

        [Fact]
        public async Task DeleteTaskItem_WithSubTasksOrTags_DeletesTaskItemAndCleansUpRelations()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Task to Delete");
            TaskItem subTask = await TestDataHelper.SeedSubTaskItemAsync(
                _dbContext,
                user.Id,
                "Subtask to Preserve",
                taskItem.Id);
            Tag tag = await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Tag to Preserve");
            taskItem.Tags.Add(tag);
            await _dbContext.SaveChangesAsync();

            await _taskItemService.DeleteTaskItemAsync(taskItem.Id);

            TaskItem? deletedTaskItem = await _dbContext.TaskItems
                .Include(currentTask => currentTask.Tags)
                .SingleOrDefaultAsync(currentTask => currentTask.Id == taskItem.Id);
            TaskItem? preservedSubTask = await _dbContext.TaskItems
                .SingleOrDefaultAsync(currentTask => currentTask.Id == subTask.Id);
            Tag? preservedTag = await _dbContext.Tags
                .Include(currentTag => currentTag.Tasks)
                .SingleOrDefaultAsync(currentTag => currentTag.Id == tag.Id);

            Assert.Null(deletedTaskItem);
            Assert.NotNull(preservedSubTask);
            Assert.Null(preservedSubTask.ParentTaskId);
            Assert.NotNull(preservedTag);
            Assert.DoesNotContain(preservedTag.Tasks, currentTask => currentTask.Id == taskItem.Id);
        }

        // RemoveTagFromTaskItem and associated tests
        [Fact]
        public async Task RemoveTagFromTaskItem_WithAssociatedTag_RemovesTagFromTaskItem()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Test Task");
            Tag tag = await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Test Tag");
            taskItem.Tags.Add(tag);
            await _dbContext.SaveChangesAsync();

            await _taskItemService.RemoveTagFromTaskItemAsync(taskItem.Id, tag.Id);

            TaskItem updatedTaskItem = await _dbContext.TaskItems
                .Include(currentTask => currentTask.Tags)
                .SingleAsync(currentTask => currentTask.Id == taskItem.Id);

            Assert.DoesNotContain(updatedTaskItem.Tags, currentTag => currentTag.Id == tag.Id);
        }

        [Fact]
        public async Task RemoveTagFromTaskItem_WithTaskItemThatDoesNotHaveTag_ThrowsBadRequestException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Test Task");
            Tag tag = await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Unassociated Tag");

            await Assert.ThrowsAsync<BadRequestException>(async () =>
            {
                await _taskItemService.RemoveTagFromTaskItemAsync(taskItem.Id, tag.Id);
            });
        }

        [Fact]
        public async Task RemoveTagFromTaskItem_WithInvalidTaskItemId_ThrowsNotFoundException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            Tag tag = await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Test Tag");

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _taskItemService.RemoveTagFromTaskItemAsync(9999, tag.Id);
            });
        }

        [Fact]
        public async Task RemoveTagFromTaskItem_WithInvalidTagId_ThrowsNotFoundException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(_dbContext);

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Test Task");

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _taskItemService.RemoveTagFromTaskItemAsync(taskItem.Id, 9999);
            });
        }

        [Fact]
        public async Task RemoveTagFromTaskItem_WithDifferentUsersTaskItem_ThrowsNotFoundException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(
                _dbContext,
                "currentuser",
                "currentuser@example.com");
            User otherUser = await TestDataHelper.SeedTestUserAsync(
                _dbContext,
                "otheruser",
                "otheruser@example.com");

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, otherUser.Id, "Other User Task");
            Tag tag = await TestDataHelper.SeedTagAsync(_dbContext, user.Id, "Test Tag");

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _taskItemService.RemoveTagFromTaskItemAsync(taskItem.Id, tag.Id);
            });
        }

        [Fact]
        public async Task RemoveTagFromTaskItem_WithDifferentUsersTag_ThrowsNotFoundException()
        {
            User user = await TestDataHelper.SeedTestUserAsync(
                _dbContext,
                "currentuser",
                "currentuser@example.com");
            User otherUser = await TestDataHelper.SeedTestUserAsync(
                _dbContext,
                "otheruser",
                "otheruser@example.com");

            TestDataHelper.SetHttpContextUser(_httpContextAccessor, user.Id);

            TaskItem taskItem = await TestDataHelper.SeedTaskItemAsync(_dbContext, user.Id, "Test Task");
            Tag tag = await TestDataHelper.SeedTagAsync(_dbContext, otherUser.Id, "Other User Tag");

            await Assert.ThrowsAsync<NotFoundException>(async () =>
            {
                await _taskItemService.RemoveTagFromTaskItemAsync(taskItem.Id, tag.Id);
            });
        }
    }
}