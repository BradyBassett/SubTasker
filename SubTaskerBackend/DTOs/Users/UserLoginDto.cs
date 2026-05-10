using System.ComponentModel.DataAnnotations;

namespace SubTaskerBackend.DTOs.Users
{
    public class UserLoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = null!;
    }
}