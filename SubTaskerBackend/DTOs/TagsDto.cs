namespace SubTaskerBackend.DTOs
{
    public class TagsDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public int UserId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public List<int> TaskIds { get; set; } = new List<int>();
    }
}