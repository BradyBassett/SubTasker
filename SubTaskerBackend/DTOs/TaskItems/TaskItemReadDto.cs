namespace SubTaskerBackend.DTOs.TaskItems
{
    public class TaskItemReadDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public Enums.TaskStatus Status { get; set; }

        public Enums.PriorityLevel Priority { get; set; }

        public DateTime? DueDate { get; set; }

        public int? CategoryId { get; set; }

        public IReadOnlyCollection<int> TagIds { get; set; } = new List<int>();

        public int? ParentTaskId { get; set; }

        public IReadOnlyCollection<TaskItemReadDto> SubTasks { get; set; } = new List<TaskItemReadDto>();

        public int UserId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}