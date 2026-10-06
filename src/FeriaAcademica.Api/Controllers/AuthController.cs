using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Application.Auth;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FeriaAcademica.Infrastructure.Identity;

namespace FeriaAcademica.Api.Controllers;

[ApiController, Route("api/auth")]
public class AuthController(IDispatcher dispatcher, SignInManager<ApplicationUser> signIn, IConfiguration config) : ControllerBase
{
    public sealed record RegistroRequest(string Email, string Password, string Nombre);
    public sealed record LoginRequest(string Email, string Password, bool Recordar = false);

    [HttpPost("registro")]
    public async Task<UsuarioDto> Registro(RegistroRequest r, CancellationToken ct) =>
        await dispatcher.Send(new RegistrarUsuarioCommand(r.Email, r.Password, r.Nombre), ct);

    /// <summary>Inicia sesión: la respuesta trae la cookie de Identity (HttpOnly).</summary>
    [HttpPost("login")]
    public async Task<UsuarioDto> Login(LoginRequest r, CancellationToken ct) =>
        await dispatcher.Send(new IniciarSesionCommand(r.Email, r.Password, r.Recordar), ct);

    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await dispatcher.Send(new CerrarSesionCommand(), ct);
        return NoContent();
    }

    /// <summary>Usuario de la sesión actual (el frontend lo consulta al arrancar).</summary>
    [HttpGet("me"), Authorize]
    public async Task<UsuarioDto> Me([FromServices] UserManager<ApplicationUser> users)
    {
        var u = await users.GetUserAsync(User) ?? throw new UnauthorizedAccessException();
        return new UsuarioDto(u.Id, u.Email!, u.NombreCompleto);
    }

    /// <summary>Paso 1 OAuth: redirige a Google.</summary>
    [HttpGet("google")]
    public IActionResult Google()
    {
        var props = signIn.ConfigureExternalAuthenticationProperties(GoogleDefaults.AuthenticationScheme, Url.Action(nameof(GoogleCallback)));
        return Challenge(props, GoogleDefaults.AuthenticationScheme);
    }

    /// <summary>Paso 2 OAuth: Google vuelve aquí; se crea/vincula el usuario de Identity, se emite la cookie y se vuelve al frontend.</summary>
    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback(CancellationToken ct)
    {
        await dispatcher.Send(new IniciarSesionExternaCommand(), ct);
        return Redirect(config["Frontend:Url"] ?? "http://localhost:4200");
    }
}
