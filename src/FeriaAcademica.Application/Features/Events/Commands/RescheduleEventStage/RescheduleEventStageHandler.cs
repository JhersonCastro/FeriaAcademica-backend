using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using Mediator;

namespace FeriaAcademica.Application.Features.Events.Commands.RescheduleEventStage;

internal sealed class RescheduleEventStageHandler(IAcademicEventRepository events, IUnitOfWork uow) : ICommandHandler<RescheduleEventStageCommand, Unit>
{
    public async ValueTask<Unit> Handle(RescheduleEventStageCommand c, CancellationToken ct)
    {
        if (c.StartDate is null && c.EndDate is null) throw new DomainException("Send the start date, the end date or both.");

        var academicEvent = await events.GetAsync(c.EventId, ct) ?? throw new DomainException("Event not found.");
        academicEvent.RescheduleStage(c.Type, c.StartDate?.ToUniversalTime(), c.EndDate?.ToUniversalTime());
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
