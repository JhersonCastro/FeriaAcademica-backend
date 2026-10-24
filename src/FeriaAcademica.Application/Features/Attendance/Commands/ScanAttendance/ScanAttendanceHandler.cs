using FeriaAcademica.Application.Features.Users;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Registrations;
using Mediator;

namespace FeriaAcademica.Application.Features.Attendance.Commands.ScanAttendance;

internal sealed class ScanAttendanceHandler(
    IAcademicEventRepository events, IRoleAssignmentRepository roles, IRegistrationRepository registrations,
    IAttendanceRepository attendances, IUserAccounts accounts)
    : ICommandHandler<ScanAttendanceCommand, ScanResultDto>
{
    public async ValueTask<ScanResultDto> Handle(ScanAttendanceCommand c, CancellationToken ct)
    {
        var academicEvent = await events.GetAsync(c.EventId, ct) ?? throw new DomainException("Event not found.");
        academicEvent.EnsureIsLive();
        AttendancePolicy.EnsureCanManage(c.ActorIsSuperAdmin, await roles.GetRolesAsync(c.ActorId, academicEvent.Id, ct));

        var token = c.QrToken?.Trim();
        var registration = string.IsNullOrEmpty(token) ? null : await registrations.GetByQrTokenAsync(token, ct);
        // A QR from another event is as invalid here as one that does not exist, and says nothing about the other event.
        if (registration is null || registration.EventId != academicEvent.Id)
            return new ScanResultDto(nameof(ScanOutcome.Invalid), "This QR code is not registered for this event.", null, null);

        var name = (await accounts.FindAsync(registration.UserId, ct))?.FullName;
        var existing = await attendances.GetByRegistrationAsync(registration.Id, ct);
        var result = registration.CheckIn(existing, c.ActorId, DateTime.UtcNow);

        if (result.Outcome == CheckInOutcome.Entered && !await attendances.TryAddAsync(result.Attendance!, ct))
            result = new CheckInResult(CheckInOutcome.AlreadyEntered, await attendances.GetByRegistrationAsync(registration.Id, ct));

        return result.Outcome switch
        {
            CheckInOutcome.Entered => new ScanResultDto(nameof(ScanOutcome.Valid), "Attendance registered.", name, result.Attendance!.At),
            CheckInOutcome.AlreadyEntered => new ScanResultDto(nameof(ScanOutcome.AlreadyEntered), "This person already entered.", name, result.Attendance?.At),
            _ => new ScanResultDto(nameof(ScanOutcome.Invalid), "This registration was cancelled.", name, null),
        };
    }
}
