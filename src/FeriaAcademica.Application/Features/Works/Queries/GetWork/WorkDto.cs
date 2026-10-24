namespace FeriaAcademica.Application.Features.Works.Queries.GetWork;

public sealed record WorkDto(
    Guid Id, Guid ParticipationId, Guid EventId, string EventName, Guid ProfessorId, string Title, string? Summary, string Status,
    int CurrentAttempt, int RejectionCount, int MaxRejections, DateTime? SubmittedAt, DateTime? SubmissionDeadline,
    string? Closure, string? ClosureReason, DateTime? ClosedAt,
    IReadOnlyList<WorkArtifactDto> Artifacts, IReadOnlyList<WorkReviewDto> Reviews);

/// <summary>Every version is listed; the highest version of each type is the current one.</summary>
public sealed record WorkArtifactDto(Guid Id, string Type, int Version, string? FileUrl, string? LinkUrl, DateTime UploadedAt);

/// <summary>The reviewer's decision and feedback on one attempt.</summary>
public sealed record WorkReviewDto(int Attempt, string Result, string? Observations, DateTime? ResolvedAt);
