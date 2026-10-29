using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Registrations;
using Mediator;

namespace FeriaAcademica.Application.Features.Events.Commands.SetEventCapacity;

internal sealed class SetEventCapacityHandler(IAcademicEventRepository events, IRegistrationRepository registrations, IUnitOfWork uow)
    : ICommandHandler<SetEventCapacityCommand, Unit>
{
    public async ValueTask<Unit> Handle(SetEventCapacityCommand c, CancellationToken ct)
    {
        var academicEvent = await events.GetAsync(c.EventId, ct) ?? throw new DomainException("Event not found.");
        academicEvent.SetCapacity(c.Capacity, await registrations.CountActiveAsync(academicEvent.Id, ct));
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
