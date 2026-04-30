using Microsoft.AspNetCore.Mvc;
using SubTaskerBackend.DTOs.Users;
using SubTaskerBackend.Mappers;
using SubTaskerBackend.Models;
using SubTaskerBackend.Services;

namespace SubTaskerBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserCreateDto userCreateDto)
        {
            User user = await _authService.RegisterAsync(userCreateDto);

            return Created("", user.ToDto());
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto loginDto)
        {
            throw new NotImplementedException();
        }
    }
}