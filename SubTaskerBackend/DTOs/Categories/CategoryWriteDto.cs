using System.ComponentModel.DataAnnotations;

namespace SubTaskerBackend.DTOs.Categories
{
    public class CategoryWriteDto
    {
        [Required]
        public string Name { get; set; } = null!;
    }
}