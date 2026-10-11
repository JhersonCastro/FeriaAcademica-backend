using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using Mediator;

namespace FeriaAcademica.Application.Features.Events.Commands.PublishEvent;

internal sealed class PublishEventHandler(IAcademicEventRepository events, IUnitOfWork uow) : ICommandHandler<PublishEventCommand, bool>
{
    public async ValueTask<bool> Handle(PublishEventCommand c, CancellationToken ct)
    {
        var academicEvent = await events.GetAsync(c.EventId, ct) ?? throw new DomainException("Event not found.");
        academicEvent.Publish();
        await uow.SaveChangesAsync(ct);
        return true;
    }
}
