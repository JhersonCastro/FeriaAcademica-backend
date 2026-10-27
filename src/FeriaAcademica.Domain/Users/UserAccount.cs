using FeriaAcademica.Domain.Common;

namespace FeriaAcademica.Domain.Users;

public enum UserStatus { Pending, Active, Inactive }

/// <summary>
/// Lifecycle rules of a user account. The account itself is the Identity user (Infrastructure), which the Domain cannot reference,
/// so these rules take its current state and decide the next one.
/// </summary>
public static class UserAccount
{
    public static void EnsureCanSignIn(UserStatus status)
    {
        switch (status)
        {
            case UserStatus.Active:
                return;
            case UserStatus.Pending:
                throw new DomainException("The account is not active yet: set your password with the link sent to your email.");
            default:
                throw new DomainException("The account is deactivated.");
        }
    }

    public static UserStatus Deactivate(UserStatus current, Guid userId, Guid actorId)
    {
        if (userId == actorId) throw new DomainException("You cannot deactivate your own account.");
        if (current == UserStatus.Inactive) throw new DomainException("The account is already deactivated.");
        return UserStatus.Inactive;
    }

    /// <summary>A reactivated account that never set a password goes back to Pending, not Active.</summary>
    public static UserStatus Activate(UserStatus current, bool hasPassword)
    {
        if (current != UserStatus.Inactive) throw new DomainException("Only a deactivated account can be activated.");
        return hasPassword ? UserStatus.Active : UserStatus.Pending;
    }

    /// <summary>Setting a password (first time or after recovery) activates a pending account; a deactivated one cannot use it to come back.</summary>
    public static UserStatus AfterPasswordSet(UserStatus current)
    {
        if (current == UserStatus.Inactive) throw new DomainException("The account is deactivated.");
        return UserStatus.Active;
    }

    public static bool CanRecoverPassword(UserStatus status) => status != UserStatus.Inactive;
}
