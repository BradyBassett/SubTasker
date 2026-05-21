using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SubTaskerBackend.Exceptions;

namespace SubTaskerBackend.Utilities
{
    public static class ClaimHelper
    {
        public static int GetUserIdFromClaims(IHttpContextAccessor httpContextAccessor)
        {
            ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;

            string? userIdClaim =
                user?.FindFirstValue(ClaimTypes.NameIdentifier) ??
                user?.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                user?.FindFirstValue("userId");

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedException("User ID claim is missing or invalid.");
            }

            return userId;
        }
    }
}