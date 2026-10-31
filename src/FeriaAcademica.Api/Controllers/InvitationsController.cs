using System.Security.Claims;
using FeriaAcademica.Application.Features.Invitations.Commands.AcceptInvitation;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeriaAcademica.Api.Controllers;

[ApiController, Route("api/invitations"), Authorize]
public class InvitationsController(IMediator mediator) : ControllerBase
{
    /// <summary>The invited student accepts with the token a professor gave them; returns the participation that new works are created under.</summary>
    [HttpPost("{token}/accept")]
    public async Task<IActionResult> Accept(string token, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var participationId = await mediator.Send(new AcceptInvitationCommand(token, userId, User.FindFirstValue(ClaimTypes.Email) ?? ""), ct);
        return Ok(new { participationId });
    }
}
