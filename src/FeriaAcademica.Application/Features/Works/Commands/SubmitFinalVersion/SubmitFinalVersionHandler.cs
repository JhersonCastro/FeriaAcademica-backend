using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Application.Features.Works.Commands.AddArtifact;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Works;
using Mediator;

namespace FeriaAcademica.Application.Features.Works.Commands.SubmitFinalVersion;

internal sealed class SubmitFinalVersionHandler(IWorkRepository works, IParticipationRepository participations, IFileStorage storage, IUnitOfWork uow)
    : ICommandHandler<SubmitFinalVersionCommand, AddedArtifact>
{
    public async ValueTask<AddedArtifact> Handle(SubmitFinalVersionCommand c, CancellationToken ct)
    {
        var work = await works.GetAsync(c.WorkId, ct) ?? throw new DomainException("Work not found.");
        var participation = await participations.GetAsync(work.ParticipationId, ct) ?? throw new DomainException("Participation not found.");
        participation.EnsureOwnedBy(c.ActorId);

        var fileId = Guid.NewGuid();
        var artifact = work.SubmitFinalVersion(WorkFileLocation.Url(work.Id, fileId), c.FileName, c.SizeBytes, DateTime.UtcNow);   // validates before anything is stored

        await storage.SaveAsync(WorkFileLocation.Key(work.Id, fileId) + Path.GetExtension(c.FileName.Trim()).ToLowerInvariant(), c.Content, ct);
        await uow.SaveChangesAsync(ct);
        return new AddedArtifact(artifact.Id, artifact.Version);
    }
}
