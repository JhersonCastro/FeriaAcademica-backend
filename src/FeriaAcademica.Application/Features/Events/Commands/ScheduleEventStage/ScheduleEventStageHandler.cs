using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using Mediator;

namespace FeriaAcademica.Application.Features.Events.Commands.ScheduleEventStage;

internal sealed class ScheduleEventStageHandler(IAcademicEventRepository events, IUnitOfWork uow) : ICommandHandler<ScheduleEventStageCommand, Unit>
{
    public async ValueTask<Unit> Handle(ScheduleEventStageCommand c, CancellationToken ct)
    {
        var academicEvent = await events.GetAsync(c.EventId, ct) ?? throw new DomainException("Event not found.");
        academicEvent.ScheduleStage(c.Type, c.StartDate.ToUniversalTime(), c.EndDate.ToUniversalTime());
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
