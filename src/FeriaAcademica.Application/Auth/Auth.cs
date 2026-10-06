using FeriaAcademica.Application.Abstractions;

namespace FeriaAcademica.Application.Auth;

public sealed record UserDto(string Id, string Email, string FullName);

public interface IAuthService
{
    Task<UserDto> RegisterAsync(string email, string password, string fullName, CancellationToken ct);
    Task<UserDto> SignInAsync(string email, string password, bool remember, CancellationToken ct);
    Task<UserDto> ExternalSignInAsync(CancellationToken ct);
    Task SignOutAsync();
}

public sealed record RegisterUserCommand(string Email, string Password, string FullName) : ICommand<UserDto>;
public sealed record SignInCommand(string Email, string Password, bool Remember) : ICommand<UserDto>;
public sealed record ExternalSignInCommand : ICommand<UserDto>;
public sealed record SignOutCommand : ICommand<bool>;

internal sealed class AuthHandlers(IAuthService auth) :
    ICommandHandler<RegisterUserCommand, UserDto>,
    ICommandHandler<SignInCommand, UserDto>,
    ICommandHandler<ExternalSignInCommand, UserDto>,
    ICommandHandler<SignOutCommand, bool>
{
    public Task<UserDto> Handle(RegisterUserCommand c, CancellationToken ct) => auth.RegisterAsync(c.Email, c.Password, c.FullName, ct);
    public Task<UserDto> Handle(SignInCommand c, CancellationToken ct) => auth.SignInAsync(c.Email, c.Password, c.Remember, ct);
    public Task<UserDto> Handle(ExternalSignInCommand c, CancellationToken ct) => auth.ExternalSignInAsync(ct);
    public async Task<bool> Handle(SignOutCommand c, CancellationToken ct) { await auth.SignOutAsync(); return true; }
}
