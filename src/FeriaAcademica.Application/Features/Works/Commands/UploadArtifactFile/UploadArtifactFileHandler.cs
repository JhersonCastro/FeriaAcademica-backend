using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Application.Features.Works.Commands.AddArtifact;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Works;
using Mediator;

namespace FeriaAcademica.Application.Features.Works.Commands.UploadArtifactFile;

internal sealed class UploadArtifactFileHandler(
    IWorkRepository works, IParticipationRepository participations, IAcademicEventRepository events, IFileStorage storage, IUnitOfWork uow)
    : ICommandHandler<UploadArtifactFileCommand, AddedArtifact>
{
    public async ValueTask<AddedArtifact> Handle(UploadArtifactFileCommand c, CancellationToken ct)
    {
        var work = await works.GetAsync(c.WorkId, ct) ?? throw new DomainException("Work not found.");
        var participation = await participations.GetAsync(work.ParticipationId, ct) ?? throw new DomainException("Participation not found.");
        participation.EnsureOwnedBy(c.ActorId);
        var academicEvent = await events.GetAsync(participation.EventId, ct) ?? throw new DomainException("Event not found.");

        var fileId = Guid.NewGuid();
        var fileUrl = $"/api/works/{work.Id}/files/{fileId}";
        var artifact = work.AddFile(c.Type, fileUrl, c.FileName, c.SizeBytes, DateTime.UtcNow, academicEvent.SubmissionDeadline);   // validates before anything is stored

        await storage.SaveAsync($"{work.Id}/{fileId}{Path.GetExtension(c.FileName.Trim()).ToLowerInvariant()}", c.Content, ct);
        await uow.SaveChangesAsync(ct);
        return new AddedArtifact(artifact.Id, artifact.Version);
    }
}
