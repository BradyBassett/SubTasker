using SubTaskerBackend.Enums;
using TaskStatus = SubTaskerBackend.Enums.TaskStatus;

namespace SubTaskerBackend.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }

        public TaskStatus Status { get; set; } = TaskStatus.notStarted;

        public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;

        public DateTime? DueDate { get; set; }

        public int? CategoryId { get; set; }

        public Category? Category { get; set; }

        public ICollection<Tag> Tags { get; set; } = new List<Tag>();

        public int? ParentTaskId { get; set; }

        public TaskItem? ParentTask { get; set; }

        public ICollection<TaskItem> SubTasks { get; set; } = new List<TaskItem>();

        public int UserId { get; set; }

        public User User { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}