using SubTaskerBackend.DTOs.TaskItems;
using SubTaskerBackend.Models;

namespace SubTaskerBackend.Mappers
{
	public static class TaskItemMapper
	{
		public static TaskItemReadDto ToDto(this TaskItem taskItem)
		{
			return new TaskItemReadDto
			{
				Id = taskItem.Id,
				Title = taskItem.Title,
				Description = taskItem.Description,
				Status = taskItem.Status,
				Priority = taskItem.Priority,
				DueDate = taskItem.DueDate,
				CategoryId = taskItem.CategoryId,
				TagIds = taskItem.Tags.Select(tag => tag.Id).ToList(),
				ParentTaskId = taskItem.ParentTaskId,
				SubTasks = taskItem.SubTasks.ToDtoList(),
				UserId = taskItem.UserId,
				CreatedAt = taskItem.CreatedAt,
				UpdatedAt = taskItem.UpdatedAt
			};
		}

		public static IReadOnlyCollection<TaskItemReadDto> ToDtoList(this IEnumerable<TaskItem> taskItems)
		{
			return taskItems.Select(taskItem => taskItem.ToDto()).ToList();
		}
	}
}