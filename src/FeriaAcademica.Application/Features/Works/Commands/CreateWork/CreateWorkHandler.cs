using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Works;
using Mediator;

namespace FeriaAcademica.Application.Features.Works.Commands.CreateWork;

internal sealed class CreateWorkHandler(IParticipationRepository participations, IAcademicEventRepository events, IWorkRepository works, IUnitOfWork uow)
    : ICommandHandler<CreateWorkCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateWorkCommand c, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(c.Title)) throw new DomainException("A title is required.");

        var participation = await participations.GetAsync(c.ParticipationId, ct) ?? throw new DomainException("Participation not found.");
        participation.EnsureOwnedBy(c.AuthorId);
        var academicEvent = await events.GetAsync(participation.EventId, ct) ?? throw new DomainException("Event not found.");

        var summary = string.IsNullOrWhiteSpace(c.Summary) ? null : c.Summary.Trim();
        var work = Work.Create(participation.Id, c.ProfessorId, c.Title, summary, academicEvent.MaxRejections, c.AuthorId, DateTime.UtcNow,
            academicEvent.SubmissionDeadline);
        await works.AddAsync(work, ct);
        await uow.SaveChangesAsync(ct);
        return work.Id;
    }
}
