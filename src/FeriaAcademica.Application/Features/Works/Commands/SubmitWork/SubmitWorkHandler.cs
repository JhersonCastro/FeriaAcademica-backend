using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Works;
using Mediator;

namespace FeriaAcademica.Application.Features.Works.Commands.SubmitWork;

internal sealed class SubmitWorkHandler(IWorkRepository works, IParticipationRepository participations, IAcademicEventRepository events, IUnitOfWork uow)
    : ICommandHandler<SubmitWorkCommand, Unit>
{
    public async ValueTask<Unit> Handle(SubmitWorkCommand c, CancellationToken ct)
    {
        var work = await works.GetAsync(c.WorkId, ct) ?? throw new DomainException("Work not found.");
        var participation = await participations.GetAsync(work.ParticipationId, ct) ?? throw new DomainException("Participation not found.");
        participation.EnsureOwnedBy(c.ActorId);
        var academicEvent = await events.GetAsync(participation.EventId, ct) ?? throw new DomainException("Event not found.");
        work.Submit(c.ActorId, DateTime.UtcNow, academicEvent.SubmissionDeadline);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
