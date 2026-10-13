using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FeriaAcademica.Infrastructure.Identity;

public static class AppClaims
{
    public const string SuperAdmin = "feria:superadmin";
    public const string SuperAdminPolicy = "SuperAdmin";
}

/// <summary>Adds the super admin claim to the session cookie, so the policy needs no database lookup per request.</summary>
internal sealed class AppClaimsPrincipalFactory(UserManager<ApplicationUser> users, IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser>(users, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        if (user.IsSuperAdmin) identity.AddClaim(new Claim(AppClaims.SuperAdmin, "true"));
        return identity;
    }
}
