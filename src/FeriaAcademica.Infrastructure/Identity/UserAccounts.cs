using FeriaAcademica.Application.Features.Users;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace FeriaAcademica.Infrastructure.Identity;

internal sealed class UserAccounts(UserManager<ApplicationUser> users) : IUserAccounts
{
    public async Task<UserAccountInfo?> FindAsync(Guid id, CancellationToken ct) =>
        await users.FindByIdAsync(id.ToString()) is { } user ? ToInfo(user) : null;

    public async Task<UserAccountInfo?> FindByEmailAsync(string email, CancellationToken ct) =>
        await users.FindByEmailAsync(email) is { } user ? ToInfo(user) : null;

    public async Task<Guid> CreateAsync(string email, UserProfileInput profile, string? password, UserStatus status, CancellationToken ct)
    {
        await EnsureEmailIsFree(email, exceptId: null);
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, Status = status };
        Apply(user, profile);
        Ensure(password is null ? await users.CreateAsync(user) : await users.CreateAsync(user, password));
        return user.Id;
    }

    public async Task UpdateProfileAsync(Guid id, UserProfileInput profile, CancellationToken ct)
    {
        var user = await Get(id);
        Apply(user, profile);
        Ensure(await users.UpdateAsync(user));
    }

    public async Task ChangeEmailAsync(Guid id, string email, CancellationToken ct)
    {
        var user = await Get(id);
        await EnsureEmailIsFree(email, exceptId: id);
        user.Email = email;
        user.UserName = email;
        user.EmailConfirmed = true;
        Ensure(await users.UpdateAsync(user)); // also normalizes both
    }

    public async Task SetStatusAsync(Guid id, UserStatus status, CancellationToken ct)
    {
        var user = await Get(id);
        user.Status = status;
        // A new security stamp makes the cookie validator reject the sessions the user already has open.
        Ensure(status == UserStatus.Inactive ? await users.UpdateSecurityStampAsync(user) : await users.UpdateAsync(user));
    }

    public async Task<string> GeneratePasswordTokenAsync(Guid id, CancellationToken ct) =>
        await users.GeneratePasswordResetTokenAsync(await Get(id));

    public async Task ResetPasswordAsync(Guid id, string token, string newPassword, CancellationToken ct)
    {
        var user = await Get(id);
        var result = await users.ResetPasswordAsync(user, token, newPassword);
        if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken))) throw new DomainException("The link is invalid or has expired.");
        Ensure(result);
        await users.SetLockoutEndDateAsync(user, null); // a fresh password ends a lockout from earlier failed attempts
        await users.ResetAccessFailedCountAsync(user);
    }

    private async Task<ApplicationUser> Get(Guid id) =>
        await users.FindByIdAsync(id.ToString()) ?? throw new DomainException("User not found.");

    private async Task EnsureEmailIsFree(string email, Guid? exceptId)
    {
        var owner = await users.FindByEmailAsync(email);
        if (owner is not null && owner.Id != exceptId) throw new DomainException("A user with this email already exists.");
    }

    private static void Apply(ApplicationUser user, UserProfileInput profile)
    {
        user.FullName = profile.FullName;
        user.PhoneNumber = profile.PhoneNumber;
        user.BirthDate = profile.BirthDate;
        user.GuardianName = profile.GuardianName;
        user.GuardianEmail = profile.GuardianEmail;
    }

    private static UserAccountInfo ToInfo(ApplicationUser u) =>
        new(u.Id, u.Email!, u.FullName, u.Status, u.IsSuperAdmin, u.PasswordHash is not null);

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded) throw new DomainException(string.Join(" ", result.Errors.Select(e => e.Description)));
    }
}
