using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SubTaskerBackend.Data;
using SubTaskerBackend.DTOs.Users;
using SubTaskerBackend.Exceptions;
using SubTaskerBackend.Interfaces;
using SubTaskerBackend.Models;

namespace SubTaskerBackend.Services
{
    public class AuthService : IAuthService
    {
        private readonly SubTaskerEfCoreDbContext _dbContext;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ITokenService _tokenService;

        public AuthService(SubTaskerEfCoreDbContext dbContext, IPasswordHasher<User> passwordHasher, ITokenService tokenService)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
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

            return _tokenService.CreateToken(user);
        }
    }
}