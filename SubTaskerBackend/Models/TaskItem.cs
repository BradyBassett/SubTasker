namespace SubTaskerBackend.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }

        public Enums.TaskStatus Status { get; set; } = Enums.TaskStatus.notStarted;

        public Enums.PriorityLevel Priority { get; set; } = Enums.PriorityLevel.Medium;

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