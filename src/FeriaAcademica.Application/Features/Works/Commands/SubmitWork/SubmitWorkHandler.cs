using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Works;
using Mediator;

namespace FeriaAcademica.Application.Features.Works.Commands.SubmitWork;

internal sealed class SubmitWorkHandler(IWorkRepository works, IAcademicEventRepository events, IUnitOfWork uow) : ICommandHandler<SubmitWorkCommand, Unit>
{
    public async ValueTask<Unit> Handle(SubmitWorkCommand c, CancellationToken ct)
    {
        var work = await works.GetAsync(c.WorkId, ct) ?? throw new DomainException("Work not found.");
        var academicEvent = await events.GetByParticipationAsync(work.ParticipationId, ct) ?? throw new DomainException("Event not found.");
        work.Submit(c.ActorId, DateTime.UtcNow, academicEvent.SubmissionDeadline);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
