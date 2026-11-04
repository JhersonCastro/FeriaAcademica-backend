using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using Mediator;

namespace FeriaAcademica.Application.Features.Events.Commands.CloseEvent;

internal sealed class CloseEventHandler(IAcademicEventRepository events, IUnitOfWork uow) : ICommandHandler<CloseEventCommand, Unit>
{
    public async ValueTask<Unit> Handle(CloseEventCommand c, CancellationToken ct)
    {
        var academicEvent = await events.GetAsync(c.EventId, ct) ?? throw new DomainException("Event not found.");
        academicEvent.Close();
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
