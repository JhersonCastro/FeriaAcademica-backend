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
        var isLink = !string.IsNullOrWhiteSpace(c.LinkUrl);
        var isFile = !string.IsNullOrWhiteSpace(c.FileUrl);
        if (isLink == isFile) throw new DomainException("Send either a link or a file.");
        if (isFile && (string.IsNullOrWhiteSpace(c.FileName) || c.FileSizeBytes is null))
            throw new DomainException("A file needs its name and size.");

        var work = await works.GetAsync(c.WorkId, ct) ?? throw new DomainException("Work not found.");
        var participation = await participations.GetAsync(work.ParticipationId, ct) ?? throw new DomainException("Participation not found.");
        participation.EnsureOwnedBy(c.ActorId);
        var academicEvent = await events.GetAsync(participation.EventId, ct) ?? throw new DomainException("Event not found.");

        var now = DateTime.UtcNow;
        var artifact = isLink
            ? work.AddArtifact(c.Type, null, c.LinkUrl!.Trim(), now, academicEvent.SubmissionDeadline)
            : work.AddFile(c.Type, c.FileUrl!.Trim(), c.FileName!, c.FileSizeBytes!.Value, now, academicEvent.SubmissionDeadline);
        await uow.SaveChangesAsync(ct);
        return new AddedArtifact(artifact.Id, artifact.Version);
    }
}
