namespace FeriaAcademica.Application.Features.Auth;

public interface IAuthService
{
    Task<UserDto> RegisterAsync(string email, string password, string fullName, CancellationToken ct);
    Task<UserDto> SignInAsync(string email, string password, bool remember, CancellationToken ct);
    Task SignOutAsync();
}
