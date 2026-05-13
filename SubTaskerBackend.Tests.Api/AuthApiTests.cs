using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using SubTaskerBackend.DTOs.Users;
using SubTaskerBackend.Models;
using SubTaskerBackend.Tests.Api.Fixtures;

namespace SubTaskerBackend.Tests.Api
{
    public class AuthApiTests : IClassFixture<ApiTestFactory>, IAsyncLifetime
    {
        private readonly ApiTestFactory _factory;

        private readonly HttpClient _client;

        public AuthApiTests(ApiTestFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            await _factory.ResetDatabaseAsync();
        }

        public Task DisposeAsync()
        {
            _client.Dispose();
            return Task.CompletedTask;
        }

        [Fact]
        public async Task Register_WithValidData_Returns201Created()
        {
            var userCreateDto = new UserCreateDto
            {
                Username = "testuser",
                Email = "testuser@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", userCreateDto);
            var createdUser = await response.Content.ReadFromJsonAsync<UserReadDto>();

            Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(createdUser);
            Assert.Equal(userCreateDto.Username, createdUser.Username);
            Assert.Equal(userCreateDto.Email, createdUser.Email);
            Assert.True(createdUser.Id > 0);
        }

        [Theory]
        [InlineData("testuser", "otheruser@example.com", "Password123!", "Password123!")] // duplicate username
        [InlineData("otheruser", "testuser@example.com", "Password123!", "Password123!")] // duplicate email
        public async Task Register_WithInvalidData_Returns409Conflict(string username, string email, string password, string confirmPassword)
        {
            await SeedTestUserAsync("testuser", "testuser@example.com", "Password123!");

            var userCreateDto = new UserCreateDto
            {
                Username = username,
                Email = email,
                Password = password,
                ConfirmPassword = confirmPassword
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", userCreateDto);

            Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Register_WithDuplicateEmail_Returns409ConflictProblemDetails()
        {
            await SeedTestUserAsync("testuser", "testuser@example.com", "Password123!");

            var userCreateDto = new UserCreateDto
            {
                Username = "otheruser",
                Email = "testuser@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", userCreateDto);
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

            Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
            Assert.NotNull(problemDetails);
            Assert.Equal(409, problemDetails.Status);
            Assert.Equal("Authentication Error", problemDetails.Title);
            Assert.Equal("Email is already in use.", problemDetails.Detail);
        }

        [Theory]
        [InlineData("", "testuser@example.com", "Password123!", "Password123!")] // empty username
        [InlineData(null, "testuser@example.com", "Password123!", "Password123!")] // null username
        [InlineData("testuser", "invalid-email", "Password123!", "Password123!")] // invalid email format
        [InlineData("testuser", "", "Password123!", "Password123!")] // empty email
        [InlineData("testuser", null, "Password123!", "Password123!")] // null email
        [InlineData("testuser", "testuser@example.com", "", "Password123!")] // password missing
        [InlineData("testuser", "testuser@example.com", null, "Password123!")] // password null
        [InlineData("testuser", "testuser@example.com", "Password123!", "")] // confirm password missing
        [InlineData("testuser", "testuser@example.com", "Password123!", null)] // confirm password null
        [InlineData("testuser", "testuser@example.com", "", "")] // both passwords missing
        [InlineData("testuser", "testuser@example.com", null, null)] // both passwords null
        [InlineData("testuser", "testuser@example.com", "short", "short")] // password too short
        [InlineData("testuser", "testuser@example.com", "Password123!", "mismatch")] // password mismatch
        [InlineData("", "", "", "")] // all missing
        [InlineData(null, null, null, null)] // all null
        public async Task Register_WithInvalidDto_Returns400BadRequest(string? username, string? email, string? password, string? confirmPassword)
        {
            var userCreateDto = new UserCreateDto
            {
                Username = username!,
                Email = email!,
                Password = password!,
                ConfirmPassword = confirmPassword!
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", userCreateDto);

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Register_WithInvalidEmailFormat_Returns400BadRequestValidationProblemDetails()
        {
            var userCreateDto = new UserCreateDto
            {
                Username = "testuser",
                Email = "invalid-email",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", userCreateDto);
            var validationProblemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(validationProblemDetails);
            Assert.Equal(400, validationProblemDetails.Status);
            Assert.Contains(nameof(UserCreateDto.Email), validationProblemDetails.Errors.Keys);
        }

        [Fact]
        public async Task Login_WithValidCredentials_Returns200OkAndToken()
        {
            var loginDto = new UserLoginDto
            {
                Email = "testuser@example.com",
                Password = "Password123!"
            };

            await SeedTestUserAsync("testuser", loginDto.Email, loginDto.Password);

            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(loginResponse);
            Assert.False(string.IsNullOrEmpty(loginResponse.Token));
        }

        [Fact]
        public async Task Login_WithInvalidEmail_Returns401Unauthorized()
        {
            var loginDto = new UserLoginDto
            {
                Email = "invalidemail@example.com",
                Password = "Password123!"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithInvalidPassword_Returns401Unauthorized()
        {
            await SeedTestUserAsync("testuser", "testuser@example.com", "Password123!");

            var loginDto = new UserLoginDto
            {
                Email = "testuser@example.com",
                Password = "InvalidPassword123!"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.NotNull(problemDetails);
            Assert.Equal(401, problemDetails.Status);
            Assert.Equal("Authentication Error", problemDetails.Title);
            Assert.Equal("Invalid email or password.", problemDetails.Detail);
        }

        [Theory]
        [InlineData("", "Password123!")] // empty email
        [InlineData(null, "Password123!")] // null email
        [InlineData("invalid-email", "Password123!")] // invalid email format
        [InlineData("testuser@example.com", "")] // empty password
        [InlineData("testuser@example.com", null)] // null password
        [InlineData("testuser@example.com", "short")] // password too short
        [InlineData("", "")] // both missing
        [InlineData(null, null)] // both null
        public async Task Login_WithInvalidDto_Returns400BadRequest(string? email, string? password)
        {
            var loginDto = new UserLoginDto
            {
                Email = email!,
                Password = password!
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RegisterThenLogin_WithValidCredentials_Returns200OkAndToken()
        {
            var userCreateDto = new UserCreateDto
            {
                Username = "testuser",
                Email = "testuser@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", userCreateDto);
            var createdUser = await registerResponse.Content.ReadFromJsonAsync<UserReadDto>();

            var loginDto = new UserLoginDto
            {
                Email = userCreateDto.Email,
                Password = userCreateDto.Password
            };

            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();

            Assert.Equal(System.Net.HttpStatusCode.Created, registerResponse.StatusCode);
            Assert.NotNull(createdUser);
            Assert.Equal(userCreateDto.Username, createdUser.Username);
            Assert.Equal(userCreateDto.Email, createdUser.Email);
            Assert.True(createdUser.Id > 0);

            Assert.Equal(System.Net.HttpStatusCode.OK, loginResponse.StatusCode);
            Assert.NotNull(loginResult);
            Assert.False(string.IsNullOrEmpty(loginResult.Token));
        }

        private async Task SeedTestUserAsync(string username, string email, string password)
        {
            var dbContext = _factory.CreateDbContext();
            await using var _ = dbContext;

            User user = new User
            {
                Username = username,
                Email = email,
            };

            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);

            dbContext.Users.Add(user);

            await dbContext.SaveChangesAsync();
        }
    }
}