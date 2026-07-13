namespace SubTaskerBackend.DTOs.TaskItems
{
    public class TaskItemUpdateDto
    {
        public string? Title { get; set; }

        public string? Description { get; set; }

        public Enums.TaskStatus? Status { get; set; }

        public Enums.PriorityLevel? Priority { get; set; }

        public DateTime? DueDate { get; set; }

        public int? CategoryId { get; set; }

        public List<int>? TagIds { get; set; }

        public int? ParentTaskId { get; set; }

        public List<int>? SubTaskIds { get; set; }
    }
}