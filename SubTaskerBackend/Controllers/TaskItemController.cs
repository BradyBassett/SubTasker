using Microsoft.AspNetCore.Mvc;
using SubTaskerBackend.DTOs.TaskItems;
using SubTaskerBackend.Interfaces;
using SubTaskerBackend.Mappers;
using SubTaskerBackend.Models;

namespace SubTaskerBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaskItemController : ControllerBase
    {
        private readonly ITaskItemService _taskItemService;

        public TaskItemController(ITaskItemService taskItemService)
        {
            _taskItemService = taskItemService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTaskItems()
        {
            List<TaskItem> taskItems = await _taskItemService.GetAllTaskItems();

            return Ok(taskItems.ToDtoList());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTaskItemById(int id)
        {
            TaskItem taskItem = await _taskItemService.GetTaskItemById(id);

            return Ok(taskItem.ToDto());
        }

        [HttpGet("{id}/tags")]
        public async Task<IActionResult> GetTagsByTaskItemId(int id)
        {
            List<Tag> tags = await _taskItemService.GetTagsByTaskItemId(id);

            return Ok(tags.ToDtoList());
        }

        [HttpPost]
        public async Task<IActionResult> CreateTaskItem([FromBody] TaskItemWriteDto taskItem)
        {
            TaskItem createdTaskItem = await _taskItemService.CreateTaskItem(taskItem);

            return CreatedAtAction(nameof(GetTaskItemById), new { id = createdTaskItem.Id }, createdTaskItem.ToDto());
        }

        [HttpPost("{taskItemId}/tags/{tagId}")]
        public async Task<IActionResult> AddTagToTaskItem(int taskItemId, int tagId)
        {
            await _taskItemService.AddTagToTaskItem(taskItemId, tagId);

            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateTaskItem(int id, [FromBody] TaskItemWriteDto taskItem)
        {
            TaskItem updatedTaskItem = await _taskItemService.UpdateTaskItem(id, taskItem);

            return Ok(updatedTaskItem.ToDto());
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTaskItem(int id)
        {
            await _taskItemService.DeleteTaskItem(id);

            return NoContent();
        }

        [HttpDelete("{taskItemId}/tags/{tagId}")]
        public async Task<IActionResult> RemoveTagFromTaskItem(int taskItemId, int tagId)
        {
            await _taskItemService.RemoveTagFromTaskItem(taskItemId, tagId);

            return NoContent();
        }
    }
}