using System.Security.Cryptography;
using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Registrations;
using FeriaAcademica.Application.Features.Attendance.Queries.GetAttendanceSummary;
using FeriaAcademica.Application.Features.Registrations.Queries.GetMyRegistration;
using Mediator;

namespace FeriaAcademica.Application.Features.Registrations.Commands.RegisterForEvent;

internal sealed class RegisterForEventHandler(IAcademicEventRepository events, IRegistrationRepository registrations, IRegistrationReadModel readModel, IUnitOfWork uow,
    IAttendanceReadModel summaries, IRealtimePublisher realtime)
    : ICommandHandler<RegisterForEventCommand, RegistrationDto>
{
    public async ValueTask<RegistrationDto> Handle(RegisterForEventCommand c, CancellationToken ct)
    {
        var academicEvent = await events.GetAsync(c.EventId, ct) ?? throw new DomainException("Event not found.");
        academicEvent.EnsureIsLive();

        var registration = await registrations.GetAsync(academicEvent.Id, c.UserId, ct);
        if (registration is null)
        {
            academicEvent.EnsureHasRoom(await registrations.CountActiveAsync(academicEvent.Id, ct));

            // 256 random bits: the QR cannot be guessed or derived from the person or the event.
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
            registration = Registration.Create(academicEvent.Id, c.UserId, token);
            await registrations.AddAsync(registration, ct);
            await uow.SaveChangesAsync(ct);
            realtime.ToEvent(academicEvent.Id, RealtimeEvents.Summary, await summaries.GetSummaryAsync(academicEvent.Id, ct));   // one more registered person
        }

        return await readModel.GetAsync(academicEvent.Id, c.UserId, ct) ?? throw new DomainException("Registration not found.");
    }
}
