using SubTaskerBackend.Data;
using SubTaskerBackend.DTOs.TaskItems;
using SubTaskerBackend.Interfaces;
using SubTaskerBackend.Models;
using SubTaskerBackend.Utilities;
using Microsoft.EntityFrameworkCore;
using SubTaskerBackend.Exceptions;

namespace SubTaskerBackend.Services
{
    public class TaskItemService : ITaskItemService
    {
        private readonly SubTaskerEfCoreDbContext _dbContext;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public TaskItemService(SubTaskerEfCoreDbContext dbContext, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<TaskItem>> GetAllTaskItemsAsync()
        {
            int userId = ClaimHelper.GetUserIdFromClaims(_httpContextAccessor);

            List<TaskItem> taskItems = await _dbContext.TaskItems
                .Where(t => t.UserId == userId)
                .Include(t => t.SubTasks)
                .Include(t => t.Tags)
                .OrderBy(t => t.DueDate)
                .ToListAsync();

            return taskItems;
        }

        public async Task<List<Tag>> GetTagsByTaskItemIdAsync(int taskItemId)
        {
            int userId = ClaimHelper.GetUserIdFromClaims(_httpContextAccessor);

            TaskItem? taskItem = await _dbContext.TaskItems
                .Include(t => t.Tags)
                .FirstOrDefaultAsync(t => t.Id == taskItemId && t.UserId == userId);

            if (taskItem == null)
            {
                throw new NotFoundException("Task item not found.");
            }

            return taskItem.Tags.ToList();
        }

        public async Task<TaskItem> GetTaskItemByIdAsync(int id)
        {
            int userId = ClaimHelper.GetUserIdFromClaims(_httpContextAccessor);

            TaskItem? taskItem = await _dbContext.TaskItems
                .Include(t => t.SubTasks)
                .Include(t => t.Tags)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (taskItem == null)
            {
                throw new NotFoundException("Task item not found.");
            }

            return taskItem;
        }

        public async Task<TaskItem> CreateTaskItemAsync(TaskItemWriteDto taskItemDto)
        {
            if (string.IsNullOrWhiteSpace(taskItemDto.Title))
            {
                throw new BadRequestException("Task item title is required.");
            }

            int userId = ClaimHelper.GetUserIdFromClaims(_httpContextAccessor);

            if (taskItemDto.CategoryId.HasValue)
            {
                var category = await _dbContext.Categories
                    .FirstOrDefaultAsync(c => c.Id == taskItemDto.CategoryId.Value && c.UserId == userId);

                if (category == null)
                {
                    throw new NotFoundException("Invalid category ID.");
                }
            }

            if (taskItemDto.ParentTaskId.HasValue)
            {
                var parentTask = await _dbContext.TaskItems
                    .FirstOrDefaultAsync(t => t.Id == taskItemDto.ParentTaskId.Value && t.UserId == userId);

                if (parentTask == null)
                {
                    throw new NotFoundException("Invalid parent task ID.");
                }
            }

            ICollection<Tag> tags = new List<Tag>();
            if (taskItemDto.TagIds.Any())
            {
                tags = await _dbContext.Tags
                    .Where(t => taskItemDto.TagIds.Contains(t.Id) && t.UserId == userId)
                    .ToListAsync();

                if (tags.Count != taskItemDto.TagIds.Count)
                {
                    throw new NotFoundException("One or more tag IDs are invalid.");
                }

                if (taskItemDto.TagIds.Count != taskItemDto.TagIds.Distinct().Count())
                {
                    throw new BadRequestException("Duplicate tag IDs are not allowed.");
                }
            }

            TaskItem taskItem = new TaskItem
            {
                Title = taskItemDto.Title.Trim(),
                Description = taskItemDto.Description,
                Status = taskItemDto.Status ?? Enums.TaskStatus.notStarted,
                Priority = taskItemDto.Priority ?? Enums.PriorityLevel.Medium,
                DueDate = taskItemDto.DueDate,
                CategoryId = taskItemDto.CategoryId,
                Tags = tags,
                ParentTaskId = taskItemDto.ParentTaskId,
                UserId = userId
            };

            await _dbContext.TaskItems.AddAsync(taskItem);
            await _dbContext.SaveChangesAsync();

            return taskItem;
        }

        public async Task AddTagToTaskItemAsync(int taskItemId, int tagId)
        {
            int userId = ClaimHelper.GetUserIdFromClaims(_httpContextAccessor);

            TaskItem? taskItem = await _dbContext.TaskItems
                .Include(t => t.Tags)
                .FirstOrDefaultAsync(t => t.Id == taskItemId && t.UserId == userId);

            if (taskItem == null)
            {
                throw new NotFoundException("Invalid task item ID.");
            }

            Tag? tag = await _dbContext.Tags.FirstOrDefaultAsync(t => t.Id == tagId && t.UserId == userId);

            if (tag == null)
            {
                throw new NotFoundException("Invalid tag ID.");
            }

            if (!taskItem.Tags.Contains(tag))
            {
                taskItem.Tags.Add(tag);
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                throw new BadRequestException("Tag is already associated with the task item.");
            }
        }

        public async Task<TaskItem> UpdateTaskItemAsync(int id, TaskItemUpdateDto taskItemDto)
        {
            int userId = ClaimHelper.GetUserIdFromClaims(_httpContextAccessor);

            TaskItem? taskItem = await _dbContext.TaskItems
                .Include(t => t.Tags)
                .Include(t => t.SubTasks)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (taskItem == null)
            {
                throw new NotFoundException("Invalid task item ID.");
            }

            if (taskItemDto.CategoryId.HasValue)
            {
                bool categoryExists = await _dbContext.Categories.AnyAsync(c => c.Id == taskItemDto.CategoryId.Value && c.UserId == userId);
                if (!categoryExists)
                {
                    throw new NotFoundException("Invalid category ID.");
                }
            }

            if (taskItemDto.ParentTaskId.HasValue)
            {
                bool parentTaskExists = await _dbContext.TaskItems.AnyAsync(t => t.Id == taskItemDto.ParentTaskId.Value && t.UserId == userId);
                if (!parentTaskExists)
                {
                    throw new NotFoundException("Invalid parent task ID.");
                }

                if (taskItemDto.ParentTaskId.Value == id)
                {
                    throw new BadRequestException("A task cannot be its own parent.");
                }
            }

            taskItem.Title = taskItemDto.Title?.Trim() ?? taskItem.Title;
            taskItem.Description = taskItemDto.Description ?? taskItem.Description;
            taskItem.Status = taskItemDto.Status ?? taskItem.Status;
            taskItem.Priority = taskItemDto.Priority ?? taskItem.Priority;
            taskItem.DueDate = taskItemDto.DueDate ?? taskItem.DueDate;
            taskItem.CategoryId = taskItemDto.CategoryId ?? taskItem.CategoryId;
            taskItem.ParentTaskId = taskItemDto.ParentTaskId ?? taskItem.ParentTaskId;

            if (taskItemDto.TagIds != null)
            {
                if (taskItemDto.TagIds.Count != taskItemDto.TagIds.Distinct().Count())
                {
                    throw new BadRequestException("Duplicate tag IDs are not allowed.");
                }

                List<Tag> tags = await _dbContext.Tags
                    .Where(t => taskItemDto.TagIds.Contains(t.Id) && t.UserId == userId)
                    .ToListAsync();

                if (tags.Count != taskItemDto.TagIds.Count)
                {
                    throw new NotFoundException("One or more tag IDs are invalid.");
                }

                taskItem.Tags.Clear();
                foreach (Tag tag in tags)
                {
                    taskItem.Tags.Add(tag);
                }
            }

            if (taskItemDto.SubTaskIds != null)
            {
                if (taskItemDto.SubTaskIds.Count != taskItemDto.SubTaskIds.Distinct().Count())
                {
                    throw new BadRequestException("Duplicate sub-task IDs are not allowed.");
                }

                if (taskItemDto.SubTaskIds.Contains(id))
                {
                    throw new BadRequestException("A task cannot be a sub-task of itself.");
                }

                List<TaskItem> subTasks = await _dbContext.TaskItems
                    .Where(t => taskItemDto.SubTaskIds.Contains(t.Id) && t.UserId == userId)
                    .ToListAsync();

                if (subTasks.Count != taskItemDto.SubTaskIds.Count)
                {
                    throw new NotFoundException("One or more sub-task IDs are invalid.");
                }

                taskItem.SubTasks.Clear();
                foreach (TaskItem subTask in subTasks)
                {
                    taskItem.SubTasks.Add(subTask);
                }
            }

            await _dbContext.SaveChangesAsync();

            return taskItem;
        }

        public async Task DeleteTaskItemAsync(int id)
        {
            int userId = ClaimHelper.GetUserIdFromClaims(_httpContextAccessor);

            TaskItem? taskItem = await _dbContext.TaskItems.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (taskItem == null)
            {
                throw new NotFoundException("Invalid task item ID.");
            }

            _dbContext.TaskItems.Remove(taskItem);
            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveTagFromTaskItemAsync(int taskItemId, int tagId)
        {
            int userId = ClaimHelper.GetUserIdFromClaims(_httpContextAccessor);

            TaskItem? taskItem = await _dbContext.TaskItems
                .Include(t => t.Tags)
                .FirstOrDefaultAsync(t => t.Id == taskItemId && t.UserId == userId);

            if (taskItem == null)
            {
                throw new NotFoundException("Invalid task item ID.");
            }

            Tag? tag = await _dbContext.Tags.FirstOrDefaultAsync(t => t.Id == tagId && t.UserId == userId);

            if (tag == null)
            {
                throw new NotFoundException("Invalid tag ID.");
            }

            if (taskItem.Tags.Contains(tag))
            {
                taskItem.Tags.Remove(tag);
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                throw new BadRequestException("The task item does not contain the specified tag.");
            }
        }
    }
}