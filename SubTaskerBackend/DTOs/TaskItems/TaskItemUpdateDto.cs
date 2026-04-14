namespace SubTaskerBackend.DTOs.TaskItems
{
    public class TaskItemUpdateDto
    {
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public Enums.TaskStatus Status { get; set; }

        public Enums.PriorityLevel Priority { get; set; }

        public DateTime? DueDate { get; set; }

        public int? CategoryId { get; set; }

        public List<int> TagIds { get; set; } = new List<int>();
    }
}