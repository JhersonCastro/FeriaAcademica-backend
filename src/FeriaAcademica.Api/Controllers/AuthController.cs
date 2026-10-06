using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Application.Auth;
using FeriaAcademica.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FeriaAcademica.Api.Controllers;

[ApiController, Route("api/auth")]
public class AuthController(IDispatcher dispatcher, SignInManager<ApplicationUser> signIn, IConfiguration config) : ControllerBase
{
    public sealed record RegisterRequest(string Email, string Password, string FullName);
    public sealed record LoginRequest(string Email, string Password, bool Remember = false);

    [HttpPost("register")]
    public async Task<UserDto> Register(RegisterRequest r, CancellationToken ct) =>
        await dispatcher.Send(new RegisterUserCommand(r.Email, r.Password, r.FullName), ct);

    [HttpPost("login")]
    public async Task<UserDto> Login(LoginRequest r, CancellationToken ct) =>
        await dispatcher.Send(new SignInCommand(r.Email, r.Password, r.Remember), ct);

    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await dispatcher.Send(new SignOutCommand(), ct);
        return NoContent();
    }

    [HttpGet("me"), Authorize]
    public async Task<UserDto> Me([FromServices] UserManager<ApplicationUser> users)
    {
        var user = await users.GetUserAsync(User) ?? throw new UnauthorizedAccessException();
        return new UserDto(user.Id, user.Email!, user.FullName);
    }

    [HttpGet("google")]
    public IActionResult Google()
    {
        var props = signIn.ConfigureExternalAuthenticationProperties(GoogleDefaults.AuthenticationScheme, Url.Action(nameof(GoogleCallback)));
        return Challenge(props, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback(CancellationToken ct)
    {
        await dispatcher.Send(new ExternalSignInCommand(), ct);
        return Redirect(config["Frontend:Url"] ?? "http://localhost:4200");
    }
}
