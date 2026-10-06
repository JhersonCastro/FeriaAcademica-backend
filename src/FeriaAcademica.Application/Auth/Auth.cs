using FeriaAcademica.Application.Abstractions;

namespace FeriaAcademica.Application.Auth;

public sealed record UsuarioDto(string Id, string Email, string Nombre);

/// <summary>
/// Puerto de identidad. La implementación (ASP.NET Core Identity + SignInManager) vive en Infrastructure
/// y es quien emite la cookie de sesión.
/// </summary>
public interface IAuthService
{
    Task<UsuarioDto> RegistrarAsync(string email, string password, string nombre, CancellationToken ct);
    Task<UsuarioDto> LoginAsync(string email, string password, bool recordar, CancellationToken ct);
    /// <summary>Completa el login con Google/Microsoft: crea el usuario si no existe, vincula el login externo e inicia sesión.</summary>
    Task<UsuarioDto> LoginExternoAsync(CancellationToken ct);
    Task CerrarSesionAsync();
}

public sealed record RegistrarUsuarioCommand(string Email, string Password, string Nombre) : ICommand<UsuarioDto>;
public sealed record IniciarSesionCommand(string Email, string Password, bool Recordar) : ICommand<UsuarioDto>;
public sealed record IniciarSesionExternaCommand : ICommand<UsuarioDto>;
public sealed record CerrarSesionCommand : ICommand<bool>;

internal sealed class AuthHandlers(IAuthService auth) :
    ICommandHandler<RegistrarUsuarioCommand, UsuarioDto>,
    ICommandHandler<IniciarSesionCommand, UsuarioDto>,
    ICommandHandler<IniciarSesionExternaCommand, UsuarioDto>,
    ICommandHandler<CerrarSesionCommand, bool>
{
    public Task<UsuarioDto> Handle(RegistrarUsuarioCommand c, CancellationToken ct) => auth.RegistrarAsync(c.Email, c.Password, c.Nombre, ct);
    public Task<UsuarioDto> Handle(IniciarSesionCommand c, CancellationToken ct) => auth.LoginAsync(c.Email, c.Password, c.Recordar, ct);
    public Task<UsuarioDto> Handle(IniciarSesionExternaCommand c, CancellationToken ct) => auth.LoginExternoAsync(ct);
    public async Task<bool> Handle(CerrarSesionCommand c, CancellationToken ct) { await auth.CerrarSesionAsync(); return true; }
}
