using FeriaAcademica.Domain.Users;

namespace FeriaAcademica.Application.Features.Users;

public sealed record UserAccountInfo(Guid Id, string Email, string FullName, UserStatus Status, bool IsSuperAdmin, bool HasPassword);

public sealed record UserProfileInput(string FullName, string? PhoneNumber, DateOnly? BirthDate, string? GuardianName, string? GuardianEmail);

/// <summary>Writes on the Identity user. Handlers decide the status with <see cref="UserAccount"/> and store it through here.</summary>
public interface IUserAccounts
{
    Task<UserAccountInfo?> FindAsync(Guid id, CancellationToken ct);
    Task<UserAccountInfo?> FindByEmailAsync(string email, CancellationToken ct);

    /// <summary>Creates the account; without a password it can only be activated through a set-password link. Throws if the email is taken.</summary>
    Task<Guid> CreateAsync(string email, UserProfileInput profile, string? password, UserStatus status, CancellationToken ct);

    Task UpdateProfileAsync(Guid id, UserProfileInput profile, CancellationToken ct);

    /// <summary>Throws if another account already uses the email.</summary>
    Task ChangeEmailAsync(Guid id, string email, CancellationToken ct);

    /// <summary>Deactivating also invalidates the sessions the user has open.</summary>
    Task SetStatusAsync(Guid id, UserStatus status, CancellationToken ct);

    Task<string> GeneratePasswordTokenAsync(Guid id, CancellationToken ct);

    /// <summary>Throws if the token is invalid or expired, or the password does not meet the policy.</summary>
    Task ResetPasswordAsync(Guid id, string token, string newPassword, CancellationToken ct);
}
