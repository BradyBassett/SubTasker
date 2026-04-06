namespace SubTaskerBackend.DTOs
{
    public class TaskItemsDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public Enums.TaskStatus Status { get; set; }

        public Enums.PriorityLevel Priority { get; set; }

        public DateTime? DueDate { get; set; }

        public int? CategoryId { get; set; }

        public List<int> TagIds { get; set; } = new List<int>();

        public int? ParentTaskId { get; set; }

        public List<TaskItemsDto> SubTasks { get; set; } = new List<TaskItemsDto>();

        public int UserId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}