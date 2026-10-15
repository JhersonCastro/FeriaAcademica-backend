using FeriaAcademica.Domain.Common;

namespace FeriaAcademica.Domain.Works;

public enum WorkStatus { Draft, Submitted, InReview, Admitted, Evaluated, RejectedDefinitively, Cancelled }
public enum ClosureType { Manual, ByAttempts, Withdrawal }

public sealed class Work : Entity
{
    private readonly List<Artifact> _artifacts = [];
    private readonly List<WorkHistoryEntry> _history = [];
    private readonly List<ReviewQueueItem> _reviewQueue = [];

    public Guid ParticipationId { get; private set; }
    public Guid ProfessorId { get; private set; }   // reviewer: the professor who nominated the participant
    public string Title { get; private set; } = default!;
    public string? Summary { get; private set; }
    public WorkStatus Status { get; private set; } = WorkStatus.Draft;   // only changes through the methods below
    public int CurrentAttempt { get; private set; } = 1;
    public int RejectionCount { get; private set; }
    public int MaxRejections { get; private set; }   // copied from the event
    public DateTime? SubmittedAt { get; private set; }   // first submission (tie-break)
    public ClosureType? Closure { get; private set; }
    public string? ClosureReason { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public string? Prize { get; private set; }
    public IReadOnlyCollection<Artifact> Artifacts => _artifacts;
    public IReadOnlyCollection<WorkHistoryEntry> History => _history;
    public IReadOnlyCollection<ReviewQueueItem> ReviewQueue => _reviewQueue;

    private Work() { } // EF Core

    public static Work Create(Guid participationId, Guid professorId, string title, string? summary, int maxRejections, Guid authorId, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Title is required.");
        if (maxRejections < 1) throw new DomainException("Max rejections must be at least one.");

        var work = new Work
        {
            ParticipationId = participationId, ProfessorId = professorId, Title = title.Trim(), Summary = summary, MaxRejections = maxRejections,
        };
        work._history.Add(new WorkHistoryEntry(work.Id, 1, null, WorkStatus.Draft, authorId, null, false, now));
        return work;
    }

    public Artifact AddArtifact(ArtifactType type, string? fileUrl, string? linkUrl, DateTime now)
    {
        if (Status != WorkStatus.Draft) throw new DomainException("A work can only be edited while it is a draft.");
        var version = _artifacts.Where(a => a.Type == type).Select(a => a.Version).DefaultIfEmpty(0).Max() + 1;
        var artifact = new Artifact(Id, type, version, fileUrl, linkUrl, now);
        _artifacts.Add(artifact);
        return artifact;
    }

    public void Submit(Guid actorId, DateTime now)
    {
        if (Status != WorkStatus.Draft) throw new DomainException("Only a draft can be submitted.");
        if (_artifacts.Count == 0) throw new DomainException("A work needs at least one artifact to be submitted.");

        var item = CurrentQueueItem();
        if (item is null) _reviewQueue.Add(new ReviewQueueItem(Id, CurrentAttempt, now));
        else item.Requeue(now);

        SubmittedAt ??= now;
        MoveTo(WorkStatus.Submitted, actorId, now);
    }

    public void Withdraw(Guid actorId, DateTime now)
    {
        switch (Status)
        {
            case WorkStatus.Draft:
                Close(ClosureType.Withdrawal, "Withdrawn by the author.", now);
                MoveTo(WorkStatus.Cancelled, actorId, now);
                break;
            case WorkStatus.Submitted: // nobody has opened it yet
                CurrentQueueItem()?.Withdraw();
                MoveTo(WorkStatus.Draft, actorId, now, "Withdrawn before review.");
                break;
            case WorkStatus.InReview:
                throw new DomainException("A work in review can no longer be withdrawn.");
            default:
                throw new DomainException($"A work in state {Status} cannot be withdrawn.");
        }
    }

    public void StartReview(Guid reviewerId, DateTime now)
    {
        if (Status != WorkStatus.Submitted) throw new DomainException("Only a submitted work can enter review.");
        EnsureReviewer(reviewerId);
        CurrentQueueItem()?.Take(now);
        MoveTo(WorkStatus.InReview, reviewerId, now);
    }

    /// <summary>Records the reviewer's decision: approved, approved with changes (back to draft to correct it) or rejected (closed).</summary>
    public void Decide(Guid reviewerId, ReviewDecision decision, string? feedback, DateTime now)
    {
        if (decision is not ReviewDecision.Approved && string.IsNullOrWhiteSpace(feedback))
            throw new DomainException("Feedback is required unless the work is approved.");

        switch (decision)
        {
            case ReviewDecision.Approved: Admit(reviewerId, now, feedback); break;
            case ReviewDecision.ApprovedWithChanges: Reject(reviewerId, feedback!, now); break;
            case ReviewDecision.Rejected: RejectDefinitively(reviewerId, feedback!, now); break;
            default: throw new DomainException($"Unknown review decision {decision}.");
        }
    }

    public void Admit(Guid reviewerId, DateTime now, string? observations = null)
    {
        EnsureInReviewBy(reviewerId);
        observations = string.IsNullOrWhiteSpace(observations) ? null : observations;
        CurrentQueueItem()?.Resolve(ReviewResult.Admitted, observations, now);
        MoveTo(WorkStatus.Admitted, reviewerId, now, observations);
    }

    public void Reject(Guid reviewerId, string observations, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(observations)) throw new DomainException("A rejection requires observations.");
        EnsureInReviewBy(reviewerId);
        CurrentQueueItem()?.Resolve(ReviewResult.Rejected, observations, now);

        RejectionCount++;
        if (RejectionCount >= MaxRejections) // rejection number N closes the work on its own
        {
            Close(ClosureType.ByAttempts, $"The limit of {MaxRejections} rejections was reached.", now);
            MoveTo(WorkStatus.RejectedDefinitively, null, now, "Rejection limit reached.");
        }
        else
        {
            MoveTo(WorkStatus.Draft, reviewerId, now, observations);
            CurrentAttempt++;
        }
    }

    public void RejectDefinitively(Guid reviewerId, string reason, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("A definitive rejection requires a reason.");
        EnsureInReviewBy(reviewerId);
        CurrentQueueItem()?.Resolve(ReviewResult.DefinitiveRejection, reason, now);
        Close(ClosureType.Manual, reason, now);
        MoveTo(WorkStatus.RejectedDefinitively, reviewerId, now, reason);
    }

    public void Reopen(Guid actorId, string reason, DateTime now)
    {
        if (Status != WorkStatus.RejectedDefinitively) throw new DomainException("Only a definitively rejected work can be reopened.");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("Reopening requires a reason.");

        MaxRejections = Math.Max(MaxRejections, RejectionCount + 1);
        MoveTo(WorkStatus.Draft, actorId, now, reason, forced: true);
        CurrentAttempt++;
        Closure = null;
        ClosureReason = null;
        ClosedAt = null;
    }

    private ReviewQueueItem? CurrentQueueItem() => _reviewQueue.FirstOrDefault(q => q.Attempt == CurrentAttempt);

    private void EnsureReviewer(Guid reviewerId)
    {
        if (reviewerId != ProfessorId) throw new DomainException("Only the professor assigned to this work can review it.");
    }

    private void EnsureInReviewBy(Guid reviewerId)
    {
        if (Status != WorkStatus.InReview) throw new DomainException("The work is not in review.");
        EnsureReviewer(reviewerId);
    }

    private void Close(ClosureType type, string reason, DateTime now)
    {
        Closure = type;
        ClosureReason = reason;
        ClosedAt = now;
    }

    private void MoveTo(WorkStatus next, Guid? actorId, DateTime now, string? reason = null, bool forced = false)
    {
        _history.Add(new WorkHistoryEntry(Id, CurrentAttempt, Status, next, actorId, reason, forced, now));
        Status = next;
    }
}
