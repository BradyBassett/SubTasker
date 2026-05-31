using SubTaskerBackend.Models;
using SubTaskerBackend.Services;
using Microsoft.Extensions.Configuration;

namespace SubTaskerBackend.Tests.Unit.Services
{
    public class TokenServiceTests
    {
        private static User CreateTestUser()
        {
            return new User
            {
                Id = 1,
                Username = "testuser",
                Email = "testuser@email.com",
                PasswordHash = "hashedpassword"
            };
        }

        private static IConfiguration CreateConfiguration(
            string? issuer = "https://test-issuer.com",
            string? audience = "https://test-audience.com",
            string? signingKey = "supersecrettestkey123456789012345678")
        {
            var configValues = new Dictionary<string, string?>
            {
                ["Auth:Issuer"] = issuer,
                ["Auth:Audience"] = audience,
                ["Auth:SigningKey"] = signingKey
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();
        }

        [Fact]
        public void CreateToken_WithValidUser_ShouldReturnToken()
        {
            var user = CreateTestUser();
            var config = CreateConfiguration();
            var tokenService = new TokenService(config);

            string token = tokenService.CreateToken(user);

            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            Assert.Equal("https://test-issuer.com", jwtToken.Issuer);
            Assert.Equal("https://test-audience.com", jwtToken.Audiences.FirstOrDefault());
            Assert.Contains(jwtToken.Claims, c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
            Assert.Contains(jwtToken.Claims, c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email && c.Value == user.Email);
            Assert.Contains(jwtToken.Claims, c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.UniqueName && c.Value == user.Username);
            Assert.True(jwtToken.ValidTo > DateTime.UtcNow.AddMinutes(29) && jwtToken.ValidTo <= DateTime.UtcNow.AddMinutes(31));
        }

        [Fact]
        public void CreateToken_WithMissingIssuer_ShouldThrowInvalidOperationException()
        {
            var user = CreateTestUser();
            var config = CreateConfiguration(issuer: null);
            var tokenService = new TokenService(config);

            var ex = Assert.Throws<InvalidOperationException>(() => tokenService.CreateToken(user));

            Assert.Equal("Auth:Issuer is not configured.", ex.Message);
        }

        [Fact]
        public void CreateToken_WithMissingAudience_ShouldThrowInvalidOperationException()
        {
            var user = CreateTestUser();
            var config = CreateConfiguration(audience: null);
            var tokenService = new TokenService(config);

            var ex = Assert.Throws<InvalidOperationException>(() => tokenService.CreateToken(user));

            Assert.Equal("Auth:Audience is not configured.", ex.Message);
        }

        [Fact]
        public void CreateToken_WithMissingSigningKey_ShouldThrowInvalidOperationException()
        {
            var user = CreateTestUser();
            var config = CreateConfiguration(signingKey: null);
            var tokenService = new TokenService(config);

            var ex = Assert.Throws<InvalidOperationException>(() => tokenService.CreateToken(user));

            Assert.Equal("Auth:SigningKey is not configured.", ex.Message);
        }
    }
}