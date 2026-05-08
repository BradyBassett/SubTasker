using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SubTaskerBackend.Data;
using SubTaskerBackend.DTOs.Users;
using SubTaskerBackend.Exceptions;
using SubTaskerBackend.Interfaces;
using SubTaskerBackend.Models;

namespace SubTaskerBackend.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly SubTaskerEfCoreDbContext _dbContext;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthService(IConfiguration configuration, SubTaskerEfCoreDbContext dbContext, IPasswordHasher<User> passwordHasher)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
        }

        public string CreateToken(User user)
        {
			// not strictly necessary to validate these here since the app won't start without them, but it doesn't hurt to be defensive
			string issuer = _configuration["Auth:Issuer"] ?? throw new InvalidOperationException("Auth:Issuer is not configured.");
            string audience = _configuration["Auth:Audience"] ?? throw new InvalidOperationException("Auth:Audience is not configured.");
            string signingKey = _configuration["Auth:SigningKey"] ?? throw new InvalidOperationException("Auth:SigningKey is not configured.");

            // A claim is a piece of information about the user that we want to include in the token.
            List<Claim> claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            };

            SymmetricSecurityKey key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(signingKey));
            SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: credentials
            );

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

            return tokenHandler.WriteToken(token);
        }

        // Register User
        public async Task<User> RegisterAsync(UserCreateDto userCreateDto)
        {
            if (userCreateDto.Password != userCreateDto.ConfirmPassword)
            {
                throw new ConflictException("Passwords do not match.");
            }

            if (await _dbContext.Users.AnyAsync(u => u.Email == userCreateDto.Email))
            {
                throw new ConflictException("Email is already in use.");
            }

            if (await _dbContext.Users.AnyAsync(u => u.Username == userCreateDto.Username))
            {
                throw new ConflictException("Username is already in use.");
            }

            User user = new User
            {
                Username = userCreateDto.Username,
                Email = userCreateDto.Email,
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, userCreateDto.Password);

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            return user;
        }

        // Login User
        public async Task<string> LoginAsync(UserLoginDto loginDto)
        {
            User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
            if (user == null)
            {
                throw new UnauthorizedException("Invalid email or password.");
            }

            PasswordVerificationResult result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                throw new UnauthorizedException("Invalid email or password.");
            }

            if(result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, loginDto.Password);
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync();
            }

            return CreateToken(user);
        }
    }
}