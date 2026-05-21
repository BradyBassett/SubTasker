using System.ComponentModel.DataAnnotations;

namespace SubTaskerBackend.DTOs.Tags
{
    public class TagWriteDto
    {
        [Required]
        public string Name { get; set; } = null!;
    }
}