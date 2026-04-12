using SubTaskerBackend.DTOs;
using SubTaskerBackend.Models;

namespace SubTaskerBackend.Mappers
{
	public static class TaskItemsMapper
	{
		public static TaskItemsDto ToDto(this TaskItems taskItem)
		{
			return new TaskItemsDto
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

		public static IReadOnlyCollection<TaskItemsDto> ToDtoList(this IEnumerable<TaskItems> taskItems)
		{
			return taskItems.Select(taskItem => taskItem.ToDto()).ToList();
		}
	}
}