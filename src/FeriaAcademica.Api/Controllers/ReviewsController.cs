using System.Security.Claims;
using System.Text.Json.Serialization;
using FeriaAcademica.Application.Features.Works.Commands.DecideReview;
using FeriaAcademica.Application.Features.Works.Commands.StartReview;
using FeriaAcademica.Application.Features.Works.Queries.ListAssignedWorks;
using FeriaAcademica.Domain.Works;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeriaAcademica.Api.Controllers;

/// <summary>The reviewer's side of a work: what is waiting for them, opening it and recording the decision.</summary>
[ApiController, Route("api/reviews"), Authorize]
public class ReviewsController(IMediator mediator) : ControllerBase
{
    public sealed record DecisionRequest([property: JsonConverter(typeof(JsonStringEnumConverter))] ReviewDecision Decision, string? Feedback);

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("assigned")]
    public async Task<IReadOnlyList<AssignedWorkDto>> Assigned(CancellationToken ct) => await mediator.Send(new ListAssignedWorksQuery(UserId), ct);

    [HttpPost("{workId:guid}/start")]
    public async Task<IActionResult> Start(Guid workId, CancellationToken ct)
    {
        await mediator.Send(new StartReviewCommand(workId, UserId), ct);
        return NoContent();
    }

    [HttpPost("{workId:guid}/decision")]
    public async Task<IActionResult> Decide(Guid workId, DecisionRequest r, CancellationToken ct)
    {
        await mediator.Send(new DecideReviewCommand(workId, UserId, r.Decision, r.Feedback), ct);
        return NoContent();
    }
}
