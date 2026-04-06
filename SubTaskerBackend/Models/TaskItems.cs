using SubTaskerBackend.Enums;
using TaskStatus = SubTaskerBackend.Enums.TaskStatus;

namespace SubTaskerBackend.Models
{
    public class TaskItems
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }

        public TaskStatus Status { get; set; } = TaskStatus.notStarted;

        public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;

        public DateTime? DueDate { get; set; }

        public int? CategoryId { get; set; }

        public Categories? Category { get; set; }

        public ICollection<Tags> Tags { get; set; } = new List<Tags>();

        public int? ParentTaskId { get; set; }

        public TaskItems? ParentTask { get; set; }

        public ICollection<TaskItems> SubTasks { get; set; } = new List<TaskItems>();

        public int UserId { get; set; }

        public Users User { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}