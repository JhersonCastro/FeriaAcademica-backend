using FeriaAcademica.Application.Features.Auth;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace FeriaAcademica.Infrastructure.Auth;

internal sealed class AuthService(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn) : IAuthService
{
    public async Task<UserDto> RegisterAsync(string email, string password, string fullName, CancellationToken ct)
    {
        var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName };
        Ensure(await users.CreateAsync(user, password));
        await signIn.SignInAsync(user, isPersistent: false);
        return ToDto(user);
    }

    public async Task<UserDto> SignInAsync(string email, string password, bool remember, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(email) ?? throw new DomainException("Invalid email or password.");
        var result = await signIn.PasswordSignInAsync(user, password, remember, lockoutOnFailure: true);
        if (result.IsLockedOut) throw new DomainException("Account temporarily locked after too many failed attempts.");
        if (!result.Succeeded) throw new DomainException("Invalid email or password.");
        return ToDto(user);
    }

    public Task SignOutAsync() => signIn.SignOutAsync();

    private static UserDto ToDto(ApplicationUser u) => new(u.Id.ToString(), u.Email!, u.FullName);

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded) throw new DomainException(string.Join(" ", result.Errors.Select(e => e.Description)));
    }
}
