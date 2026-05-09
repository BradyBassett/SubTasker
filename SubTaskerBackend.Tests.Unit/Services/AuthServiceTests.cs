using Moq;
using SubTaskerBackend.Models;
using SubTaskerBackend.Services;
using Microsoft.Extensions.Configuration;

namespace SubTaskerBackend.Tests.Unit.Services
{
    public class AuthServiceTests
    {
        [Fact]
        public void CreateToken_WithValidUser_ShouldReturnToken()
        {
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "testuser@email.com",
                PasswordHash = "hashedpassword"
            };

            var config = new Mock<IConfiguration>();
            config.Setup(c => c["Auth:Issuer"]).Returns("https://test-issuer.com");
            config.Setup(c => c["Auth:Audience"]).Returns("https://test-audience.com");
            config.Setup(c => c["Auth:SigningKey"]).Returns("supersecrettestkey123456789012345678");

            var authService = new AuthService(config.Object, null!, null!);

            string token = authService.CreateToken(user);

            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            Assert.Equal("https://test-issuer.com", jwtToken.Issuer);
            Assert.Equal("https://test-audience.com", jwtToken.Audiences.FirstOrDefault());
            Assert.Contains(jwtToken.Claims, c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
            Assert.Contains(jwtToken.Claims, c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email && c.Value == user.Email);
            Assert.Contains(jwtToken.Claims, c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.UniqueName && c.Value == user.Username);
            Assert.True(jwtToken.ValidTo > DateTime.UtcNow.AddMinutes(29) && jwtToken.ValidTo <= DateTime.UtcNow.AddMinutes(31));
        }
    }
}