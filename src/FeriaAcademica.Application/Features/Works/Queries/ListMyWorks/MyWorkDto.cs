namespace FeriaAcademica.Application.Features.Works.Queries.ListMyWorks;

/// <summary>One of the signed-in participant's works, with the feedback of its latest review.</summary>
public sealed record MyWorkDto(
    Guid Id, Guid EventId, string EventName, string Title, string Status, int CurrentAttempt, int RejectionCount, int MaxRejections,
    DateTime? SubmittedAt, DateTime? SubmissionDeadline, string? LastReviewResult, string? LastReviewObservations);
