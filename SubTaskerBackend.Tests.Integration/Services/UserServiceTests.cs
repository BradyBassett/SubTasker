using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SubTaskerBackend.Data;
using SubTaskerBackend.Exceptions;
using SubTaskerBackend.Models;
using SubTaskerBackend.Services;
using SubTaskerBackend.Tests.Integration.Fixtures;

namespace SubTaskerBackend.Tests.Integration.Services
{
    public class UserServiceTests : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly PostgresFixture _postgresFixture;

        private SubTaskerEfCoreDbContext _dbContext = null!;
        private IHttpContextAccessor _httpContextAccessor = null!;
        private UserService _userService = null!;

        public UserServiceTests(PostgresFixture postgresFixture)
        {
            _postgresFixture = postgresFixture;
        }

        public async Task InitializeAsync()
        {
            await _postgresFixture.ResetDatabaseAsync();

            _dbContext = _postgresFixture.CreateDbContext();
            _httpContextAccessor = new HttpContextAccessor();
            _userService = new UserService(_dbContext, _httpContextAccessor);
        }

        public async Task DisposeAsync()
        {
            if (_dbContext is not null)
            {
                await _dbContext.DisposeAsync();
            }
        }

        [Fact]
        public async Task GetCurrentUserAsync_WithValidUser_ReturnsUser()
        {
            User seededUser = await SeedTestUserAsync();
            SetHttpContextUser(seededUser.Id);

            User result = await _userService.GetCurrentUserAsync();

            Assert.NotNull(result);
            Assert.Equal(seededUser.Id, result.Id);
            Assert.Equal(seededUser.Username, result.Username);
            Assert.Equal(seededUser.Email, result.Email);
        }

        [Fact]
        public async Task GetCurrentUserAsync_WithMissingUserIdClaim_ThrowsUnauthorizedException()
        {
            _httpContextAccessor.HttpContext = new DefaultHttpContext();

            await Assert.ThrowsAsync<UnauthorizedException>(() => _userService.GetCurrentUserAsync());
        }

        [Fact]
        public async Task GetCurrentUserAsync_WithInvalidUserIdClaim_ThrowsUnauthorizedException()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "invalid")
                })
            );

            _httpContextAccessor.HttpContext = httpContext;

            await Assert.ThrowsAsync<UnauthorizedException>(() => _userService.GetCurrentUserAsync());
        }

        [Fact]
        public async Task GetCurrentUserAsync_WithNonExistentUserIdClaim_ThrowsNotFoundException()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "9999")
                })
            );

            _httpContextAccessor.HttpContext = httpContext;

            await Assert.ThrowsAsync<NotFoundException>(() => _userService.GetCurrentUserAsync());
        }

        [Fact]
        public async Task GetUserByIdAsync_WithExistingId_ReturnsUser()
        {
            User seededUser = await SeedTestUserAsync();

            User result = await _userService.GetUserByIdAsync(seededUser.Id);

            Assert.NotNull(result);
            Assert.Equal(seededUser.Id, result.Id);
            Assert.Equal(seededUser.Username, result.Username);
            Assert.Equal(seededUser.Email, result.Email);
        }

        [Fact]
        public async Task GetUserByIdAsync_WithMissingId_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _userService.GetUserByIdAsync(9999));
        }

        private void SetHttpContextUser(int userId)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                })
            );

            _httpContextAccessor.HttpContext = httpContext;
        }

        private async Task<User> SeedTestUserAsync()
        {
            var user = new User
            {
                Username = "testuser",
                Email = "testuser@mail.com",
                PasswordHash = "somehash"
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            return user;
        }
    }
}