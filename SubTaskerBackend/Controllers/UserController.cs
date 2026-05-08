using Microsoft.AspNetCore.Mvc;
using SubTaskerBackend.Mappers;
using SubTaskerBackend.Models;
using SubTaskerBackend.Interfaces;

namespace SubTaskerBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            User user = await _userService.GetCurrentUserAsync();

            return Ok(user.ToDto());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            User user = await _userService.GetUserByIdAsync(id);

            return Ok(user.ToDto());
        }
    }
}