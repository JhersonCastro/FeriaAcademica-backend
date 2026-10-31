using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Works;
using Mediator;

namespace FeriaAcademica.Application.Features.Works.Commands.AddArtifact;

internal sealed class AddArtifactHandler(IWorkRepository works, IParticipationRepository participations, IAcademicEventRepository events, IUnitOfWork uow)
    : ICommandHandler<AddArtifactCommand, AddedArtifact>
{
    public async ValueTask<AddedArtifact> Handle(AddArtifactCommand c, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(c.LinkUrl)) throw new DomainException("A link is required.");

        var work = await works.GetAsync(c.WorkId, ct) ?? throw new DomainException("Work not found.");
        var participation = await participations.GetAsync(work.ParticipationId, ct) ?? throw new DomainException("Participation not found.");
        participation.EnsureOwnedBy(c.ActorId);
        var academicEvent = await events.GetAsync(participation.EventId, ct) ?? throw new DomainException("Event not found.");

        var artifact = work.AddArtifact(c.Type, null, c.LinkUrl.Trim(), DateTime.UtcNow, academicEvent.SubmissionDeadline);
        await uow.SaveChangesAsync(ct);
        return new AddedArtifact(artifact.Id, artifact.Version);
    }
}
