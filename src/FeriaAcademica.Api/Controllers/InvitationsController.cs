using FeriaAcademica.Application.Features.Invitations.Commands.AcceptInvitation;
using FeriaAcademica.Application.Features.Invitations.Queries.GetInvitation;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace FeriaAcademica.Api.Controllers;

/// <summary>Opened from the emailed link, so the person may not have an account or a session yet.</summary>
[ApiController, Route("api/invitations")]
public class InvitationsController(IMediator mediator) : ControllerBase
{
    /// <summary>FullName and Password are only needed when the invited email has no account yet.</summary>
    public sealed record AcceptRequest(string? FullName, string? Password);

    [HttpGet("{token}")]
    public async Task<InvitationDto> Get(string token, CancellationToken ct) => await mediator.Send(new GetInvitationQuery(token), ct);

    [HttpPost("{token}/accept")]
    public async Task<IActionResult> Accept(string token, AcceptRequest r, CancellationToken ct)
    {
        await mediator.Send(new AcceptInvitationCommand(token, User.UserIdOrNull(), r.FullName, r.Password), ct);
        return NoContent();
    }
}
