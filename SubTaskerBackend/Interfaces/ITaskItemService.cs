using SubTaskerBackend.DTOs.TaskItems;
using SubTaskerBackend.Models;

namespace SubTaskerBackend.Interfaces
{
    public interface ITaskItemService
    {
        Task<List<TaskItem>> GetAllTaskItemsAsync();

        Task<TaskItem> GetTaskItemByIdAsync(int id);

        Task<List<Tag>> GetTagsByTaskItemIdAsync(int taskItemId);

        Task<TaskItem> CreateTaskItemAsync(TaskItemWriteDto taskItemDto);

        Task AddTagToTaskItemAsync(int taskItemId, int tagId);

        Task<TaskItem> UpdateTaskItemAsync(int id, TaskItemUpdateDto taskItemDto);

        Task DeleteTaskItemAsync(int id);

        Task RemoveTagFromTaskItemAsync(int taskItemId, int tagId);
    }
}