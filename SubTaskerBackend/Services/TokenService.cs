using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using SubTaskerBackend.Interfaces;
using SubTaskerBackend.Models;

namespace SubTaskerBackend.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
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
    }
}