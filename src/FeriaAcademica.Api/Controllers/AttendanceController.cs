using FeriaAcademica.Application.Features.Attendance.Commands.ScanAttendance;
using FeriaAcademica.Application.Features.Attendance.Queries.GetAttendanceSummary;
using FeriaAcademica.Application.Features.Registrations;
using FeriaAcademica.Application.Features.Registrations.Commands.RegisterForEvent;
using FeriaAcademica.Application.Features.Registrations.Queries.GetMyRegistration;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeriaAcademica.Api.Controllers;

/// <summary>Registration with a personal QR, and attendance taken by scanning it at the door.</summary>
[ApiController, Route("api/events/{eventId:guid}"), Authorize]
public class AttendanceController(IMediator mediator) : ControllerBase
{
    /// <summary>QrToken is the content the frontend read from the scanned QR code.</summary>
    public sealed record ScanRequest(string QrToken);

    /// <summary>Registers the signed-in user and returns the QR content to draw. Calling it again returns the same QR.</summary>
    [HttpPost("registrations")]
    public async Task<RegistrationDto> Register(Guid eventId, CancellationToken ct) =>
        await mediator.Send(new RegisterForEventCommand(eventId, User.UserId()), ct);

    /// <summary>The signed-in user's registration, with the QR content to draw and whether they already entered.</summary>
    [HttpGet("registrations/me")]
    public async Task<RegistrationDto> MyRegistration(Guid eventId, CancellationToken ct) =>
        await mediator.Send(new GetMyRegistrationQuery(eventId, User.UserId()), ct);

    /// <summary>Takes attendance from a scanned QR: Valid, AlreadyEntered or Invalid. Super admin, coordinators and teachers of the event only.</summary>
    [HttpPost("attendance/scan")]
    public async Task<ScanResultDto> Scan(Guid eventId, ScanRequest r, CancellationToken ct) =>
        await mediator.Send(new ScanAttendanceCommand(eventId, r.QrToken, User.UserId(), User.IsSuperAdmin()), ct);

    /// <summary>Registered, entered and pending counts; the frontend polls it to follow the turnout.</summary>
    [HttpGet("attendance/summary")]
    public async Task<AttendanceSummaryDto> Summary(Guid eventId, CancellationToken ct) =>
        await mediator.Send(new GetAttendanceSummaryQuery(eventId, User.UserId(), User.IsSuperAdmin()), ct);
}
