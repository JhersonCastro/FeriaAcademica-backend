using FeriaAcademica.Domain.Common;

namespace FeriaAcademica.Domain.Works;

public enum ArtifactType { Document, Poster, Presentation, Video, Code, Other }
public enum QueueStatus { Pending, InReview, Resolved, Withdrawn }
public enum ReviewResult { Admitted, Rejected, DefinitiveRejection }

public sealed class Artifact : Entity
{
    public Guid WorkId { get; private set; }
    public ArtifactType Type { get; private set; }
    public int Version { get; private set; }
    public string? FileUrl { get; private set; }
    public string? LinkUrl { get; private set; }
    public DateTime UploadedAt { get; private set; }

    private Artifact() { } // EF Core

    internal Artifact(Guid workId, ArtifactType type, int version, string? fileUrl, string? linkUrl, DateTime uploadedAt)
    {
        if (string.IsNullOrWhiteSpace(fileUrl) == string.IsNullOrWhiteSpace(linkUrl))
            throw new DomainException("An artifact needs exactly one of a file or a link.");
        WorkId = workId; Type = type; Version = version; FileUrl = fileUrl; LinkUrl = linkUrl; UploadedAt = uploadedAt;
    }
}

/// <summary>Append-only log of every state change of a work.</summary>
public sealed class WorkHistoryEntry : Entity
{
    public Guid WorkId { get; private set; }
    public int Attempt { get; private set; }
    public WorkStatus? PreviousStatus { get; private set; }
    public WorkStatus NewStatus { get; private set; }
    public Guid? ActorId { get; private set; }   // null = system
    public string? Reason { get; private set; }
    public bool Forced { get; private set; }
    public DateTime At { get; private set; }

    private WorkHistoryEntry() { } // EF Core

    internal WorkHistoryEntry(Guid workId, int attempt, WorkStatus? previous, WorkStatus next, Guid? actorId, string? reason, bool forced, DateTime at)
    {
        WorkId = workId; Attempt = attempt; PreviousStatus = previous; NewStatus = next; ActorId = actorId; Reason = reason; Forced = forced; At = at;
    }
}

public sealed class ReviewQueueItem : Entity
{
    public Guid WorkId { get; private set; }
    public int Attempt { get; private set; }
    public QueueStatus Status { get; private set; } = QueueStatus.Pending;
    public ReviewResult? Result { get; private set; }
    public DateTime EnqueuedAt { get; private set; }
    public DateTime? TakenAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? Observations { get; private set; }

    private ReviewQueueItem() { } // EF Core

    internal ReviewQueueItem(Guid workId, int attempt, DateTime enqueuedAt)
    {
        WorkId = workId; Attempt = attempt; EnqueuedAt = enqueuedAt;
    }

    internal void Requeue(DateTime now) { Status = QueueStatus.Pending; EnqueuedAt = now; }
    internal void Withdraw() => Status = QueueStatus.Withdrawn;
    internal void Take(DateTime now) { Status = QueueStatus.InReview; TakenAt = now; }

    internal void Resolve(ReviewResult result, string? observations, DateTime now)
    {
        Status = QueueStatus.Resolved; Result = result; Observations = observations; ResolvedAt = now;
    }
}

public sealed class JuryAssignment : Entity
{
    public Guid WorkId { get; private set; }
    public Guid JuryId { get; private set; }

    private JuryAssignment() { } // EF Core

    public static JuryAssignment Create(Guid workId, Guid juryId) => new() { WorkId = workId, JuryId = juryId };
}
