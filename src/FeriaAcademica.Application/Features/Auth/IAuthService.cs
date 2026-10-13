using FeriaAcademica.Application.Features.Users;

namespace FeriaAcademica.Application.Features.Auth;

public interface IAuthService
{
    /// <summary>Checks the password without signing in (counts failures towards the lockout). Throws a generic error that does not reveal whether the email exists.</summary>
    Task<UserAccountInfo> CheckPasswordAsync(string email, string password, CancellationToken ct);

    Task SignInAsync(Guid userId, bool remember, CancellationToken ct);
    Task SignOutAsync();
}
