using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SubTaskerBackend.Exceptions;
using SubTaskerBackend.Utilities;

namespace SubTaskerBackend.Tests.Unit.Utilities
{
    public class ClaimHelperTests
    {
        private static IHttpContextAccessor BuildAccessorWithClaims(params Claim[] claims)
        {
            DefaultHttpContext httpContext = new DefaultHttpContext();
            ClaimsIdentity identity = new ClaimsIdentity(claims, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);

            return new HttpContextAccessor { HttpContext = httpContext };
        }

        [Fact]
        public void GetUserIdFromClaims_WithNameIdentifierClaim_ReturnsUserId()
        {
            IHttpContextAccessor accessor = BuildAccessorWithClaims(new Claim(ClaimTypes.NameIdentifier, "42"));

            int userId = ClaimHelper.GetUserIdFromClaims(accessor);

            Assert.Equal(42, userId);
        }

        [Fact]
        public void GetUserIdFromClaims_WithSubClaim_ReturnsUserId()
        {
            IHttpContextAccessor accessor = BuildAccessorWithClaims(new Claim(JwtRegisteredClaimNames.Sub, "84"));

            int userId = ClaimHelper.GetUserIdFromClaims(accessor);

            Assert.Equal(84, userId);
        }

        [Fact]
        public void GetUserIdFromClaims_WithUserIdClaim_ReturnsUserId()
        {
            IHttpContextAccessor accessor = BuildAccessorWithClaims(new Claim("userId", "126"));

            int userId = ClaimHelper.GetUserIdFromClaims(accessor);

            Assert.Equal(126, userId);
        }

        [Fact]
        public void GetUserIdFromClaims_WithNoSupportedClaims_ThrowsUnauthorizedException()
        {
            IHttpContextAccessor accessor = BuildAccessorWithClaims(new Claim(ClaimTypes.Email, "user@example.com"));

            UnauthorizedException ex = Assert.Throws<UnauthorizedException>(() => ClaimHelper.GetUserIdFromClaims(accessor));

            Assert.Equal("User ID claim is missing or invalid.", ex.Message);
        }

        [Fact]
        public void GetUserIdFromClaims_WithInvalidClaimValue_ThrowsUnauthorizedException()
        {
            IHttpContextAccessor accessor = BuildAccessorWithClaims(new Claim(ClaimTypes.NameIdentifier, "not-an-int"));

            UnauthorizedException ex = Assert.Throws<UnauthorizedException>(() => ClaimHelper.GetUserIdFromClaims(accessor));

            Assert.Equal("User ID claim is missing or invalid.", ex.Message);
        }

        [Fact]
        public void GetUserIdFromClaims_WithNullHttpContext_ThrowsUnauthorizedException()
        {
            IHttpContextAccessor accessor = new HttpContextAccessor();

            UnauthorizedException ex = Assert.Throws<UnauthorizedException>(() => ClaimHelper.GetUserIdFromClaims(accessor));

            Assert.Equal("User ID claim is missing or invalid.", ex.Message);
        }
    }
}