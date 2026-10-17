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

    /// <param name="deadline">End of the event's reception stage; no new works can be created after it.</param>
    public static Work Create(Guid participationId, Guid professorId, string title, string? summary, int maxRejections, Guid authorId, DateTime now,
        DateTime? deadline = null)
    {
        EnsureBeforeDeadline(now, deadline);
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Title is required.");
        if (maxRejections < 1) throw new DomainException("Max rejections must be at least one.");

        var work = new Work
        {
            ParticipationId = participationId, ProfessorId = professorId, Title = title.Trim(), Summary = summary, MaxRejections = maxRejections,
        };
        work._history.Add(new WorkHistoryEntry(work.Id, 1, null, WorkStatus.Draft, authorId, null, false, now));
        return work;
    }

    /// <summary>Adds an uploaded file, which must have an allowed format and size (see <see cref="ArtifactFileRules"/>).</summary>
    public Artifact AddFile(ArtifactType type, string fileUrl, string fileName, long sizeBytes, DateTime now, DateTime? deadline = null)
    {
        ArtifactFileRules.Validate(fileName, sizeBytes);
        return AddArtifact(type, fileUrl, null, now, deadline);
    }

    /// <summary>Adds a new version of an artifact; earlier versions are kept, so this is also how one is replaced.</summary>
    public Artifact AddArtifact(ArtifactType type, string? fileUrl, string? linkUrl, DateTime now, DateTime? deadline = null)
    {
        if (Status != WorkStatus.Draft) throw new DomainException("A work can only be edited while it is a draft.");
        EnsureBeforeDeadline(now, deadline);
        var version = _artifacts.Where(a => a.Type == type).Select(a => a.Version).DefaultIfEmpty(0).Max() + 1;
        var artifact = new Artifact(Id, type, version, fileUrl, linkUrl, now);
        _artifacts.Add(artifact);
        return artifact;
    }

    /// <param name="deadline">End of the event's reception stage; null when the event has not set one.</param>
    public void Submit(Guid actorId, DateTime now, DateTime? deadline = null)
    {
        if (Status != WorkStatus.Draft) throw new DomainException("Only a draft can be submitted.");
        EnsureBeforeDeadline(now, deadline);
        if (_artifacts.Count == 0) throw new DomainException("A work needs at least one artifact to be submitted.");

        var item = CurrentQueueItem();
        if (item is null) _reviewQueue.Add(new ReviewQueueItem(Id, CurrentAttempt, now));
        else item.Requeue(now);

        SubmittedAt ??= now;
        MoveTo(WorkStatus.Submitted, actorId, now);
    }

    /// <param name="deadline">A submitted work can only be taken back to draft (to replace it) until this date.</param>
    public void Withdraw(Guid actorId, DateTime now, DateTime? deadline = null)
    {
        switch (Status)
        {
            case WorkStatus.Draft:
                Close(ClosureType.Withdrawal, "Withdrawn by the author.", now);
                MoveTo(WorkStatus.Cancelled, actorId, now);
                break;
            case WorkStatus.Submitted: // nobody has opened it yet
                EnsureBeforeDeadline(now, deadline);
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

    public void Admit(Guid reviewerId, DateTime now)
    {
        EnsureInReviewBy(reviewerId);
        CurrentQueueItem()?.Resolve(ReviewResult.Admitted, null, now);
        MoveTo(WorkStatus.Admitted, reviewerId, now);
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

    private static void EnsureBeforeDeadline(DateTime now, DateTime? deadline)
    {
        if (now > deadline) throw new DomainException($"The call is closed: the submission deadline was {deadline:yyyy-MM-dd HH:mm} UTC.");
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
