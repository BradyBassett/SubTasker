using Microsoft.AspNetCore.Authorization;
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

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(UserCreateDto userCreateDto)
        {
            User user = await _authService.RegisterAsync(userCreateDto);

            return CreatedAtAction(
                nameof(UserController.GetUserById),
                "User",
                new { id = user.Id },
                user.ToDto()
            );
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto loginDto)
        {
            string token = await _authService.LoginAsync(loginDto);

            return Ok(new LoginResponseDto { Token = token });
        }
    }
}