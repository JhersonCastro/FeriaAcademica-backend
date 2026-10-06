using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Application.Events.Commands;
using FeriaAcademica.Application.Events.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeriaAcademica.Api.Controllers;

[ApiController, Route("api/events"), Authorize]
public class EventsController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record CreateEventRequest(string Title, string Category, string Organizer, DateOnly Date, int Capacity);

    [HttpGet]
    public async Task<IReadOnlyList<EventDto>> List(CancellationToken ct) => await dispatcher.Ask(new ListEventsQuery(), ct);

    [HttpPost]
    public async Task<IActionResult> Create(CreateEventRequest r, CancellationToken ct)
    {
        var id = await dispatcher.Send(new CreateEventCommand(r.Title, r.Category, r.Organizer, r.Date, r.Capacity), ct);
        return Created($"/api/events/{id}", new { id });
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        await dispatcher.Send(new ApproveEventCommand(id), ct);
        return NoContent();
    }
}
