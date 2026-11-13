using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using Mediator;

namespace FeriaAcademica.Application.Features.Events.Commands.RemoveEventStage;

internal sealed class RemoveEventStageHandler(IAcademicEventRepository events, IUnitOfWork uow) : ICommandHandler<RemoveEventStageCommand, Unit>
{
    public async ValueTask<Unit> Handle(RemoveEventStageCommand c, CancellationToken ct)
    {
        var academicEvent = await events.GetAsync(c.EventId, ct) ?? throw new DomainException("Event not found.");
        academicEvent.RemoveStage(c.Type);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
