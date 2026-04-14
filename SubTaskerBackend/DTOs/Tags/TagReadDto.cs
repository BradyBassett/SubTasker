namespace SubTaskerBackend.DTOs.Tags
{
    public class TagReadDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public int UserId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public IReadOnlyCollection<int> TaskIds { get; set; } = new List<int>();
    }
}