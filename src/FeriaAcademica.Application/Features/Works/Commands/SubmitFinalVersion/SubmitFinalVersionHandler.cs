using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Application.Features.Works.Commands.AddArtifact;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Notifications;
using FeriaAcademica.Domain.Works;
using Mediator;

namespace FeriaAcademica.Application.Features.Works.Commands.SubmitFinalVersion;

internal sealed class SubmitFinalVersionHandler(IWorkRepository works, IParticipationRepository participations, INotificationRepository notifications, IFileStorage storage, IUnitOfWork uow)
    : ICommandHandler<SubmitFinalVersionCommand, AddedArtifact>
{
    public const string NotificationType = "FinalVersionSubmitted";

    public async ValueTask<AddedArtifact> Handle(SubmitFinalVersionCommand c, CancellationToken ct)
    {
        var work = await works.GetAsync(c.WorkId, ct) ?? throw new DomainException("Work not found.");
        var participation = await participations.GetAsync(work.ParticipationId, ct) ?? throw new DomainException("Participation not found.");
        participation.EnsureOwnedBy(c.ActorId);

        var now = DateTime.UtcNow;
        var fileId = Guid.NewGuid();
        var artifact = work.SubmitFinalVersion(WorkFileLocation.Url(work.Id, fileId), c.FileName, c.SizeBytes, now);   // validates before anything is stored

        await notifications.AddAsync(
            Notification.Create(work.ProfessorId, participation.EventId, NotificationType, $"The final version of \"{work.Title}\" was submitted.", now), ct);
        await storage.SaveAsync(WorkFileLocation.Key(work.Id, fileId) + Path.GetExtension(c.FileName.Trim()).ToLowerInvariant(), c.Content, ct);
        await uow.SaveChangesAsync(ct);
        return new AddedArtifact(artifact.Id, artifact.Version);
    }
}
