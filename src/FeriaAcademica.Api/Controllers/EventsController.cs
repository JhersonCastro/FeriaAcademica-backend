using FeriaAcademica.Application.Features.Events.Commands.AssignEventRole;
using FeriaAcademica.Application.Features.Events.Commands.CloseEvent;
using FeriaAcademica.Application.Features.Events.Commands.CreateEvent;
using FeriaAcademica.Application.Features.Events.Commands.PublishEvent;
using FeriaAcademica.Application.Features.Events.Commands.SaveEventReport;
using FeriaAcademica.Application.Features.Events.Commands.ScheduleEventStage;
using FeriaAcademica.Application.Features.Events.Commands.SetEventCapacity;
using FeriaAcademica.Application.Features.Events.Queries.DownloadEventReport;
using FeriaAcademica.Application.Features.Events.Queries.GetEventReport;
using FeriaAcademica.Application.Features.Events.Queries.GetEventSummary;
using FeriaAcademica.Application.Features.Events.Queries.ListEvents;
using FeriaAcademica.Application.Features.Invitations.Commands.SendInvitation;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Infrastructure.Identity;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeriaAcademica.Api.Controllers;

[ApiController, Route("api/events"), Authorize]
public class EventsController(IMediator mediator) : ControllerBase
{
    public sealed record CreateEventRequest(Guid InstitutionId, string Name, int? Capacity = null);
    public sealed record SetCapacityRequest(int? Capacity);
    public sealed record ScheduleStageRequest(DateTime StartDate, DateTime EndDate);
    public sealed record AssignRoleRequest(Guid UserId, string Role);
    public sealed record InviteRequest(string Email, string Role);
    public sealed record SaveReportRequest(string? Html);

    [HttpGet]
    public async Task<IReadOnlyList<EventDto>> List(CancellationToken ct) => await mediator.Send(new ListEventsQuery(), ct);

    [HttpPost, Authorize(Policy = AppClaims.SuperAdminPolicy)]
    public async Task<IActionResult> Create(CreateEventRequest r, CancellationToken ct)
    {
        var id = await mediator.Send(new CreateEventCommand(r.InstitutionId, r.Name, User.UserId(), r.Capacity), ct);
        return Created($"/api/events/{id}", new { id });
    }

    [HttpPost("{id:guid}/publish"), Authorize(Policy = AppClaims.SuperAdminPolicy)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        await mediator.Send(new PublishEventCommand(id), ct);
        return NoContent();
    }

    /// <summary>Ends a published or ongoing event: certificates become available and registrations stop.</summary>
    [HttpPost("{id:guid}/close"), Authorize(Policy = AppClaims.SuperAdminPolicy)]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
    {
        await mediator.Send(new CloseEventCommand(id), ct);
        return NoContent();
    }

    /// <summary>Sets the seats for registrations (null removes the limit); it cannot go below the people already registered.</summary>
    [HttpPut("{id:guid}/capacity"), Authorize(Policy = AppClaims.SuperAdminPolicy)]
    public async Task<IActionResult> SetCapacity(Guid id, SetCapacityRequest r, CancellationToken ct)
    {
        await mediator.Send(new SetEventCapacityCommand(id, r.Capacity), ct);
        return NoContent();
    }

    /// <summary>Sets a stage's dates; the end of the Reception stage is the submission deadline.</summary>
    [HttpPut("{id:guid}/stages/{type}"), Authorize(Policy = AppClaims.SuperAdminPolicy)]
    public async Task<IActionResult> ScheduleStage(Guid id, StageType type, ScheduleStageRequest r, CancellationToken ct)
    {
        await mediator.Send(new ScheduleEventStageCommand(id, type, r.StartDate, r.EndDate), ct);
        return NoContent();
    }

    /// <summary>Gives a user a role in this event (Coordinator, Teacher, Jury, Participant or Attendee).</summary>
    [HttpPost("{id:guid}/roles"), Authorize(Policy = AppClaims.SuperAdminPolicy)]
    public async Task<IActionResult> AssignRole(Guid id, AssignRoleRequest r, CancellationToken ct)
    {
        await mediator.Send(new AssignEventRoleCommand(id, r.UserId, r.Role), ct);
        return NoContent();
    }

    /// <summary>A teacher of the event invites a student (Participant) or a guest (Attendee), internal or external.</summary>
    [HttpPost("{id:guid}/invitations")]
    public async Task<IActionResult> Invite(Guid id, InviteRequest r, CancellationToken ct)
    {
        var invitationId = await mediator.Send(new SendInvitationCommand(id, r.Email, r.Role, User.UserId()), ct);
        return Created($"/api/events/{id}/invitations/{invitationId}", new { id = invitationId });
    }

    /// <summary>The event's consolidated figures (works, registered people and attendees), up to date when asked for.</summary>
    [HttpGet("{id:guid}/report/summary"), Authorize(Policy = AppClaims.SuperAdminPolicy)]
    public async Task<IActionResult> GetSummary(Guid id, CancellationToken ct) =>
        await mediator.Send(new GetEventSummaryQuery(id), ct) is { } summary ? Ok(summary) : NotFound();

    /// <summary>The event's report: the HTML the administrator composed.</summary>
    [HttpGet("{id:guid}/report"), Authorize(Policy = AppClaims.SuperAdminPolicy)]
    public async Task<IActionResult> GetReport(Guid id, CancellationToken ct) =>
        await mediator.Send(new GetEventReportQuery(id), ct) is { } report ? Ok(report) : NotFound();

    /// <summary>Creates the report or replaces its content (text, styles and inline images as HTML).</summary>
    [HttpPut("{id:guid}/report"), Authorize(Policy = AppClaims.SuperAdminPolicy)]
    public async Task<IActionResult> SaveReport(Guid id, SaveReportRequest r, CancellationToken ct)
    {
        await mediator.Send(new SaveEventReportCommand(id, r.Html, User.UserId()), ct);
        return NoContent();
    }

    /// <summary>The report as a PDF, with the same content and format as the page.</summary>
    [HttpGet("{id:guid}/report/pdf"), Authorize(Policy = AppClaims.SuperAdminPolicy)]
    public async Task<IActionResult> DownloadReport(Guid id, CancellationToken ct)
    {
        var pdf = await mediator.Send(new DownloadEventReportQuery(id), ct);
        return File(pdf.Content, "application/pdf", pdf.FileName);
    }
}
