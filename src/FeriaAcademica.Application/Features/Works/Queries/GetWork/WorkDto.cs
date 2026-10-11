namespace FeriaAcademica.Application.Features.Works.Queries.GetWork;

public sealed record WorkDto(
    Guid Id, Guid ParticipationId, Guid ProfessorId, string Title, string? Summary, string Status,
    int CurrentAttempt, int RejectionCount, int MaxRejections, DateTime? SubmittedAt,
    string? Closure, string? ClosureReason, DateTime? ClosedAt, int ArtifactCount);
