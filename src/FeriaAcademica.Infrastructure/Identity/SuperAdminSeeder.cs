using FeriaAcademica.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FeriaAcademica.Infrastructure.Identity;

/// <summary>
/// Creates the first super admin from configuration ("SuperAdmin:Email", "SuperAdmin:Password", optional "SuperAdmin:FullName"),
/// kept in user-secrets, never in appsettings. Does nothing once any super admin exists.
/// </summary>
public static class SuperAdminSeeder
{
    public static async Task SeedSuperAdminAsync(this IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(SuperAdminSeeder));

        if (await users.Users.AnyAsync(u => u.IsSuperAdmin)) return;

        var email = config["SuperAdmin:Email"];
        var password = config["SuperAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            logger.LogWarning("There is no super admin. Set SuperAdmin:Email and SuperAdmin:Password in user-secrets to create one on startup.");
            return;
        }

        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, FullName = config["SuperAdmin:FullName"] ?? "Super admin" };
            Ensure(await users.CreateAsync(user, password));
        }

        user.IsSuperAdmin = true;
        user.Status = UserStatus.Active;
        Ensure(await users.UpdateAsync(user));
        logger.LogInformation("Super admin {Email} is ready.", email);
    }

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException("Could not create the super admin: " + string.Join(" ", result.Errors.Select(e => e.Description)));
    }
}
