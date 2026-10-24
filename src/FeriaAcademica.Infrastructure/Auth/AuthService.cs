using FeriaAcademica.Application.Features.Auth;
using FeriaAcademica.Application.Features.Users;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace FeriaAcademica.Infrastructure.Auth;

internal sealed class AuthService(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn) : IAuthService
{
    // Same message for unknown email, wrong password and lockout: it must not reveal which accounts exist.
    private const string InvalidCredentials = "Invalid email or password, or the account is temporarily locked.";

    public async Task<UserAccountInfo> CheckPasswordAsync(string email, string password, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(email) ?? throw new DomainException(InvalidCredentials);
        var result = await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (!result.Succeeded) throw new DomainException(InvalidCredentials);
        return new UserAccountInfo(user.Id, user.Email!, user.FullName, user.Status, user.IsSuperAdmin, HasPassword: true);
    }

    public async Task SignInAsync(Guid userId, bool remember, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.ToString()) ?? throw new DomainException(InvalidCredentials);
        await signIn.SignInAsync(user, isPersistent: remember);
    }

    public Task SignOutAsync() => signIn.SignOutAsync();
}
