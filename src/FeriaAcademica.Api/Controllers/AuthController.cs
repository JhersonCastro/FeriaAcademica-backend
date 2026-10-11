using FeriaAcademica.Application.Features.Auth;
using FeriaAcademica.Application.Features.Auth.Commands.RegisterUser;
using FeriaAcademica.Application.Features.Auth.Commands.SignIn;
using FeriaAcademica.Application.Features.Auth.Commands.SignOut;
using FeriaAcademica.Infrastructure.Identity;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FeriaAcademica.Api.Controllers;

[ApiController, Route("api/auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    public sealed record RegisterRequest(string Email, string Password, string FullName);
    public sealed record LoginRequest(string Email, string Password, bool Remember = false);

    [HttpPost("register")]
    public async Task<UserDto> Register(RegisterRequest r, CancellationToken ct) =>
        await mediator.Send(new RegisterUserCommand(r.Email, r.Password, r.FullName), ct);

    [HttpPost("login")]
    public async Task<UserDto> Login(LoginRequest r, CancellationToken ct) =>
        await mediator.Send(new SignInCommand(r.Email, r.Password, r.Remember), ct);

    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await mediator.Send(new SignOutCommand(), ct);
        return NoContent();
    }

    [HttpGet("me"), Authorize]
    public async Task<UserDto> Me([FromServices] UserManager<ApplicationUser> users)
    {
        var user = await users.GetUserAsync(User) ?? throw new UnauthorizedAccessException();
        return new UserDto(user.Id.ToString(), user.Email!, user.FullName);
    }
}
