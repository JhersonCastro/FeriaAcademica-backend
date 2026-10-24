using FeriaAcademica.Application.Features.Auth.Commands.ForgotPassword;
using FeriaAcademica.Application.Features.Auth.Commands.ResetPassword;
using FeriaAcademica.Application.Features.Auth.Commands.SignIn;
using FeriaAcademica.Application.Features.Auth.Commands.SignOut;
using FeriaAcademica.Application.Features.Users.Queries.GetUser;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeriaAcademica.Api.Controllers;

[ApiController, Route("api/auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    public sealed record LoginRequest(string Email, string Password, bool Remember = false);
    public sealed record ForgotPasswordRequest(string Email);
    public sealed record ResetPasswordRequest(Guid UserId, string Token, string NewPassword);

    [HttpPost("login")]
    public async Task<UserDto> Login(LoginRequest r, CancellationToken ct) =>
        await mediator.Send(new SignInCommand(r.Email, r.Password, r.Remember), ct);

    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await mediator.Send(new SignOutCommand(), ct);
        return NoContent();
    }

    /// <summary>The session's user, with super admin flag and roles per event, so the frontend can build the menu.</summary>
    [HttpGet("me"), Authorize]
    public async Task<UserDto> Me(CancellationToken ct) => await mediator.Send(new GetUserQuery(User.UserId()), ct);

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest r, CancellationToken ct)
    {
        await mediator.Send(new ForgotPasswordCommand(r.Email), ct);
        return NoContent();
    }

    /// <summary>Used both to recover a password and to set the first one of an account created by the super admin.</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest r, CancellationToken ct)
    {
        await mediator.Send(new ResetPasswordCommand(r.UserId, r.Token, r.NewPassword), ct);
        return NoContent();
    }
}
