using System.Security.Claims;
using FeriaAcademica.Infrastructure.Identity;

namespace FeriaAcademica.Api.Controllers;

internal static class UserClaims
{
    /// <summary>The signed-in user's id; only call it on endpoints that require authentication.</summary>
    public static Guid UserId(this ClaimsPrincipal user) => user.UserIdOrNull() ?? throw new UnauthorizedAccessException();

    public static bool IsSuperAdmin(this ClaimsPrincipal user) => user.HasClaim(AppClaims.SuperAdmin, "true");

    public static Guid? UserIdOrNull(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
