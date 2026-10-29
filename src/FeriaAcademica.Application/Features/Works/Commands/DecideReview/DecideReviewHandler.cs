using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Notifications;
using FeriaAcademica.Domain.Works;
using Mediator;

namespace FeriaAcademica.Application.Features.Works.Commands.DecideReview;

internal sealed class DecideReviewHandler(
    IWorkRepository works, IParticipationRepository participations, INotificationRepository notifications, IUnitOfWork uow)
    : ICommandHandler<DecideReviewCommand, Unit>
{
    public const string NotificationType = "WorkReviewed";

    public async ValueTask<Unit> Handle(DecideReviewCommand c, CancellationToken ct)
    {
        if (!Enum.IsDefined(c.Decision)) throw new DomainException("The decision must be Approved, ApprovedWithChanges or Rejected.");

        var work = await works.GetAsync(c.WorkId, ct) ?? throw new DomainException("Work not found.");
        var now = DateTime.UtcNow;
        work.Decide(c.ReviewerId, c.Decision, c.Feedback?.Trim(), now);

        // The message follows the resulting status, so a closure by the rejection limit is reported as a rejection.
        var participation = await participations.GetAsync(work.ParticipationId, ct) ?? throw new DomainException("Participation not found.");
        var outcome = work.Status switch
        {
            WorkStatus.Admitted => "was approved",
            WorkStatus.Draft => "was approved with changes: apply the feedback and submit it again",
            _ => "was rejected",
        };
        await notifications.AddAsync(
            Notification.Create(participation.UserId, participation.EventId, NotificationType, $"Your work \"{work.Title}\" {outcome}.", now), ct);

        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
