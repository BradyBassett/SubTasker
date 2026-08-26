using SubTaskerBackend.Models;
using SubTaskerBackend.Mappers;

namespace SubTaskerBackend.Tests.Unit.Mappers;

public class TaskItemMapperTests
{
    [Fact]
    public void ToDto_WithValidTaskItem_ShouldMapAllProperties()
    {
        var taskItem = new TaskItem
        {
            Id = 1,
            Title = "Test Task",
            Description = "This is a test task.",
            Status = Enums.TaskStatus.notStarted,
            Priority = Enums.PriorityLevel.Medium,
            DueDate = DateTime.UtcNow.AddDays(7),
            CategoryId = 1,
            Tags = new List<Tag>
            {
                new Tag { Id = 1, Name = "Tag1", UserId = 1111111111, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Tag { Id = 2, Name = "Tag2", UserId = 1111111111, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            },
            ParentTaskId = null,
            SubTasks = new List<TaskItem>(),
            UserId = 1111111111,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = taskItem.ToDto();

        Assert.Equal(taskItem.Id, result.Id);
        Assert.Equal(taskItem.Title, result.Title);
        Assert.Equal(taskItem.Description, result.Description);
        Assert.Equal(taskItem.Status, result.Status);
        Assert.Equal(taskItem.Priority, result.Priority);
        Assert.Equal(taskItem.DueDate, result.DueDate);
        Assert.Equal(taskItem.CategoryId, result.CategoryId);
        Assert.Equal(taskItem.Tags.Select(tag => tag.Id).ToList(), result.TagIds);
        Assert.Equal(taskItem.ParentTaskId, result.ParentTaskId);
        Assert.Equal(taskItem.SubTasks.Count, result.SubTaskIds.Count);
        Assert.Equal(taskItem.UserId, result.UserId);
        Assert.Equal(taskItem.CreatedAt, result.CreatedAt);
        Assert.Equal(taskItem.UpdatedAt, result.UpdatedAt);
    }

    [Fact]
    public void ToDto_WithSubTasks_ShouldMapSubTaskIds()
    {
        var taskItem = new TaskItem
        {
            Id = 1,
            Title = "Test Task",
            Description = "This is a test task.",
            Status = Enums.TaskStatus.notStarted,
            Priority = Enums.PriorityLevel.Medium,
            DueDate = DateTime.UtcNow.AddDays(7),
            CategoryId = 1,
            Tags = new List<Tag>
            {
                new Tag { Id = 1, Name = "Tag1", UserId = 1111111111, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Tag { Id = 2, Name = "Tag2", UserId = 1111111111, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            },
            ParentTaskId = null,
            SubTasks = new List<TaskItem>
            {
                new TaskItem
                {
                    Id = 2,
                    Title = "Sub Task 1",
                    Description = "This is a sub task.",
                    Status = Enums.TaskStatus.inProgress,
                    Priority = Enums.PriorityLevel.Medium,
                    DueDate = DateTime.UtcNow.AddDays(3),
                    CategoryId = 1,
                    Tags = new List<Tag>
                    {
                        new Tag { Id = 3, Name = "Tag3", UserId = 1111111111, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
                    },
                    ParentTaskId = 1,
                    SubTasks = new List<TaskItem>(),
                    UserId = 1111111111,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new TaskItem
                {
                    Id = 3,
                    Title = "Sub Task 2",
                    Description = "This is another sub task.",
                    Status = Enums.TaskStatus.completed,
                    Priority = Enums.PriorityLevel.Low,
                    DueDate = DateTime.UtcNow.AddDays(3),
                    CategoryId = 1,
                    Tags = new List<Tag>
                    {
                        new Tag { Id = 4, Name = "Tag4", UserId = 1111111111, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
                    },
                    ParentTaskId = 1,
                    SubTasks = new List<TaskItem>(),
                    UserId = 1111111111,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            },
            UserId = 1111111111,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = taskItem.ToDto();

        Assert.Equal(taskItem.Id, result.Id);
        Assert.Equal(taskItem.Title, result.Title);
        Assert.Equal(taskItem.Description, result.Description);
        Assert.Equal(taskItem.Status, result.Status);
        Assert.Equal(taskItem.Priority, result.Priority);
        Assert.Equal(taskItem.DueDate, result.DueDate);
        Assert.Equal(taskItem.CategoryId, result.CategoryId);
        Assert.Equal(taskItem.Tags.Select(tag => tag.Id).ToList(), result.TagIds);
        Assert.Equal(taskItem.ParentTaskId, result.ParentTaskId);
        Assert.Equal(taskItem.SubTasks.Count, result.SubTaskIds.Count);
        Assert.Equal(taskItem.UserId, result.UserId);
        Assert.Equal(taskItem.CreatedAt, result.CreatedAt);
        Assert.Equal(taskItem.UpdatedAt, result.UpdatedAt);
    }

    [Fact]
    public void ToDtoList_WithMultipleTaskItems_ShouldMapAllTaskItems()
    {
        var taskItems = new List<TaskItem>
        {
            new TaskItem
            {
                Id = 1,
                Title = "Task 1",
                Description = "Description 1",
                Status = Enums.TaskStatus.notStarted,
                Priority = Enums.PriorityLevel.Low,
                DueDate = DateTime.UtcNow.AddDays(7),
                CategoryId = 1,
                Tags = new List<Tag>
                {
                    new Tag { Id = 1, Name = "Tag1", UserId = 1111111111, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new Tag { Id = 2, Name = "Tag2", UserId = 1111111111, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
                },
                ParentTaskId = null,
                SubTasks = new List<TaskItem>(),
                UserId = 1111111111,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TaskItem
            {
                Id = 2,
                Title = "Task 2",
                Description = "Description 2",
                Status = Enums.TaskStatus.inProgress,
                Priority = Enums.PriorityLevel.Medium,
                DueDate = DateTime.UtcNow.AddDays(7),
                CategoryId = 1,
                Tags = new List<Tag>
                {
                    new Tag { Id = 1, Name = "Tag1", UserId = 222222222, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new Tag { Id = 2, Name = "Tag2", UserId = 222222222, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
                },
                ParentTaskId = null,
                SubTasks = new List<TaskItem>(),
                UserId = 222222222,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TaskItem
            {
                Id = 3,
                Title = "Task 3",
                Description = "Description 3",
                Status = Enums.TaskStatus.completed,
                Priority = Enums.PriorityLevel.High,
                DueDate = DateTime.UtcNow.AddDays(7),
                CategoryId = 1,
                Tags = new List<Tag>
                {
                    new Tag { Id = 1, Name = "Tag1", UserId = 333333333, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new Tag { Id = 2, Name = "Tag2", UserId = 333333333, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
                },
                ParentTaskId = null,
                SubTasks = new List<TaskItem>(),
                UserId = 333333333,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        var result = taskItems.ToDtoList();

        Assert.Equal(taskItems.Count, result.Count);
        for (int i = 0; i < taskItems.Count; i++)
        {
            Assert.Equal(taskItems[i].Id, result.ElementAt(i).Id);
            Assert.Equal(taskItems[i].Title, result.ElementAt(i).Title);
            Assert.Equal(taskItems[i].Description, result.ElementAt(i).Description);
            Assert.Equal(taskItems[i].Status, result.ElementAt(i).Status);
            Assert.Equal(taskItems[i].Priority, result.ElementAt(i).Priority);
            Assert.Equal(taskItems[i].DueDate, result.ElementAt(i).DueDate);
            Assert.Equal(taskItems[i].CategoryId, result.ElementAt(i).CategoryId);
            Assert.Equal(taskItems[i].Tags.Select(tag => tag.Id).ToList(), result.ElementAt(i).TagIds);
            Assert.Equal(taskItems[i].ParentTaskId, result.ElementAt(i).ParentTaskId);
            Assert.Equal(taskItems[i].SubTasks.Count, result.ElementAt(i).SubTaskIds.Count);
            Assert.Equal(taskItems[i].UserId, result.ElementAt(i).UserId);
            Assert.Equal(taskItems[i].CreatedAt, result.ElementAt(i).CreatedAt);
            Assert.Equal(taskItems[i].UpdatedAt, result.ElementAt(i).UpdatedAt);
        }
    }
}
