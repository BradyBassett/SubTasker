namespace SubTaskerBackend.DTOs.TaskItems
{
    public class TaskItemWriteDto
    {
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public Enums.PriorityLevel Priority { get; set; }

        public DateTime? DueDate { get; set; }

        public int? CategoryId { get; set; }

        public List<int> TagIds { get; set; } = new List<int>();

        public int? ParentTaskId { get; set; }
    }
}