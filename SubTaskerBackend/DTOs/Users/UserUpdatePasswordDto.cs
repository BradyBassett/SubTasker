namespace SubTaskerBackend.DTOs.Users
{
    public class UserUpdatePasswordDto
    {

        public string? CurrentPassword { get; set; }

        public string? NewPassword { get; set; }

        public string? ConfirmNewPassword { get; set; }
    }
}