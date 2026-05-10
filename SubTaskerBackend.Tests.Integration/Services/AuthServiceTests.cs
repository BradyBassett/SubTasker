using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using SubTaskerBackend.Data;
using SubTaskerBackend.DTOs.Users;
using SubTaskerBackend.Exceptions;
using SubTaskerBackend.Models;
using SubTaskerBackend.Services;
using SubTaskerBackend.Tests.Integration.Fixtures;

namespace SubTaskerBackend.Tests.Integration.Services;

public class AuthServiceTests : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly IConfiguration _configuration;
    private readonly PostgresFixture _postgresFixture;

    private SubTaskerEfCoreDbContext _dbContext = null!;
    private PasswordHasher<User> _passwordHasher = null!;
    private AuthService _authService = null!;

    public AuthServiceTests(PostgresFixture postgresFixture)
    {
        Dictionary<string, string?> configValues = new()
        {
            ["Auth:Issuer"] = "https://test-issuer",
            ["Auth:Audience"] = "https://test-audience",
            ["Auth:SigningKey"] = "supersecrettestkey123456789012345678"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        _postgresFixture = postgresFixture;
    }

    public async Task InitializeAsync()
    {
        await _postgresFixture.ResetDatabaseAsync();

        _dbContext = _postgresFixture.CreateDbContext();
        _passwordHasher = new PasswordHasher<User>();
        _authService = new AuthService(_configuration, _dbContext, _passwordHasher);
    }

    public async Task DisposeAsync()
    {
        if (_dbContext is not null)
        {
            await _dbContext.DisposeAsync();
        }
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_CreatesUser()
    {
        var userCreateDto = new UserCreateDto
        {
            Username = "testuser",
            Email = "testuser@mail.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!",
        };

        var result = await _authService.RegisterAsync(userCreateDto);

        Assert.NotNull(result);
        Assert.Equal(userCreateDto.Username, result.Username);
        Assert.Equal(userCreateDto.Email, result.Email);

        Assert.False(string.IsNullOrEmpty(result.PasswordHash));
        Assert.NotEqual(userCreateDto.Password, result.PasswordHash);

        await using var verifyContext = _postgresFixture.CreateDbContext();
        var createdUser = await verifyContext.Users.FindAsync(result.Id);

        Assert.NotNull(createdUser);
        Assert.Equal(result.Id, createdUser.Id);
        Assert.Equal(result.Username, createdUser.Username);
        Assert.Equal(result.Email, createdUser.Email);

        var hashVerification = _passwordHasher.VerifyHashedPassword(createdUser, createdUser.PasswordHash, userCreateDto.Password);
        Assert.True(
            hashVerification == PasswordVerificationResult.Success ||
            hashVerification == PasswordVerificationResult.SuccessRehashNeeded);
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidConfirmPassword_ThrowsConflictException()
    {
        var userCreateDto = new UserCreateDto
        {
            Username = "testuser",
            Email = "testuser@mail.com",
            Password = "TestPassword123!",
            ConfirmPassword = "DifferentPassword123!",
        };

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _authService.RegisterAsync(userCreateDto));
        Assert.Equal("Passwords do not match.", ex.Message);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ThrowsConflictException()
    {
        await SeedTestUserAsync();

        var userCreateDto = new UserCreateDto
        {
            Username = "newuser",
            Email = "testuser@mail.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!",
        };

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _authService.RegisterAsync(userCreateDto));
        Assert.Equal("Email is already in use.", ex.Message);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingUsername_ThrowsConflictException()
    {
        await SeedTestUserAsync();

        var userCreateDto = new UserCreateDto
        {
            Username = "testuser",
            Email = "newemail@mail.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!",
        };

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _authService.RegisterAsync(userCreateDto));
        Assert.Equal("Username is already in use.", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        var userLoginDto = new UserLoginDto
        {
            Email = "testuser@mail.com",
            Password = "TestPassword123!"
        };

        await SeedTestUserAsync();

        var token = await _authService.LoginAsync(userLoginDto);

        Assert.False(string.IsNullOrEmpty(token));

        AssertJwtContainsUserClaims(token, userLoginDto.Email, "testuser");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ThrowsUnauthorizedException()
    {
        var userLoginDto = new UserLoginDto
        {
            Email = "invalidemail@mail.com",
            Password = "TestPassword123!"
        };

        await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(userLoginDto));
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsUnauthorizedException()
    {
        var userLoginDto = new UserLoginDto
        {
            Email = "testuser@mail.com",
            Password = "InvalidPassword123!"
        };

        await SeedTestUserAsync();

        await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(userLoginDto));
    }

    

    private async Task SeedTestUserAsync()
    {
        var userCreateDto = new UserCreateDto
        {
            Username = "testuser",
            Email = "testuser@mail.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!",
        };

        await _authService.RegisterAsync(userCreateDto);
    }

    private void AssertJwtContainsUserClaims(string token, string expectedEmail, string expectedUsername)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwt = tokenHandler.ReadJwtToken(token);

        Assert.NotNull(jwt);
        Assert.Equal(expectedEmail, jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(expectedUsername, jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
        Assert.False(string.IsNullOrEmpty(jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value));
    }
}