using SubTaskerBackend.DTOs.TaskItems;
using SubTaskerBackend.Models;

namespace SubTaskerBackend.Interfaces
{
    public interface ITaskItemService
    {
        Task<List<TaskItem>> GetAllTaskItems();

        Task<TaskItem> GetTaskItemById(int id);

        Task<List<Tag>> GetTagsByTaskItemId(int taskItemId);

        Task<TaskItem> CreateTaskItem(TaskItemWriteDto taskItemDto);

        Task AddTagToTaskItem(int taskItemId, int tagId);

        Task<TaskItem> UpdateTaskItem(int id, TaskItemUpdateDto taskItemDto);

        Task DeleteTaskItem(int id);

        Task RemoveTagFromTaskItem(int taskItemId, int tagId);
    }
}