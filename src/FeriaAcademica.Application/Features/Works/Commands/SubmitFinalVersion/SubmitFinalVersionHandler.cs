using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Application.Features.Works.Commands.AddArtifact;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Works;
using Mediator;

namespace FeriaAcademica.Application.Features.Works.Commands.SubmitFinalVersion;

internal sealed class SubmitFinalVersionHandler(IWorkRepository works, IParticipationRepository participations, IUnitOfWork uow)
    : ICommandHandler<SubmitFinalVersionCommand, AddedArtifact>
{
    public async ValueTask<AddedArtifact> Handle(SubmitFinalVersionCommand c, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(c.FileUrl) || string.IsNullOrWhiteSpace(c.FileName) || c.FileSizeBytes is null)
            throw new DomainException("The final version needs the file, its name and its size.");

        var work = await works.GetAsync(c.WorkId, ct) ?? throw new DomainException("Work not found.");
        var participation = await participations.GetAsync(work.ParticipationId, ct) ?? throw new DomainException("Participation not found.");
        participation.EnsureOwnedBy(c.ActorId);

        var artifact = work.SubmitFinalVersion(c.FileUrl, c.FileName, c.FileSizeBytes.Value, DateTime.UtcNow);
        await uow.SaveChangesAsync(ct);
        return new AddedArtifact(artifact.Id, artifact.Version);
    }
}
