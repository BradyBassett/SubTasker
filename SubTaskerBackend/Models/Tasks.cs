using SubTaskerBackend.Enums;
using TaskStatus = SubTaskerBackend.Enums.TaskStatus;

namespace SubTaskerBackend.Models
{
    public class Tasks
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

        public Tasks? ParentTask { get; set; }

        public ICollection<Tasks> SubTasks { get; set; } = new List<Tasks>();

        public int UserId { get; set; }

        public Users user { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}