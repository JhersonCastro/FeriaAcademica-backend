using System.Security.Claims;
using FeriaAcademica.Application.Features.Events.Commands.CreateEvent;
using FeriaAcademica.Application.Features.Events.Commands.PublishEvent;
using FeriaAcademica.Application.Features.Events.Commands.ScheduleEventStage;
using FeriaAcademica.Application.Features.Events.Queries.ListEvents;
using FeriaAcademica.Domain.Events;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeriaAcademica.Api.Controllers;

[ApiController, Route("api/events"), Authorize]
public class EventsController(IMediator mediator) : ControllerBase
{
    public sealed record CreateEventRequest(Guid InstitutionId, string Name);
    public sealed record ScheduleStageRequest(DateTime StartDate, DateTime EndDate);

    [HttpGet]
    public async Task<IReadOnlyList<EventDto>> List(CancellationToken ct) => await mediator.Send(new ListEventsQuery(), ct);

    [HttpPost]
    public async Task<IActionResult> Create(CreateEventRequest r, CancellationToken ct)
    {
        var createdBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var id = await mediator.Send(new CreateEventCommand(r.InstitutionId, r.Name, createdBy), ct);
        return Created($"/api/events/{id}", new { id });
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        await mediator.Send(new PublishEventCommand(id), ct);
        return NoContent();
    }

    /// <summary>Sets a stage's dates; the end of the Reception stage is the submission deadline.</summary>
    [HttpPut("{id:guid}/stages/{type}")]
    public async Task<IActionResult> ScheduleStage(Guid id, StageType type, ScheduleStageRequest r, CancellationToken ct)
    {
        await mediator.Send(new ScheduleEventStageCommand(id, type, r.StartDate, r.EndDate), ct);
        return NoContent();
    }
}
