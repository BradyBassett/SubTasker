namespace SubTaskerBackend.Models
{
    public class Categories
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public int UserId { get; set; }

        public Users user { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}