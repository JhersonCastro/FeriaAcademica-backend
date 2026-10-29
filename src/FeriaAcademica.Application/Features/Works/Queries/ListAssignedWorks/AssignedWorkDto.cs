namespace FeriaAcademica.Application.Features.Works.Queries.ListAssignedWorks;

public sealed record AssignedWorkDto(
    Guid Id, Guid EventId, string EventName, string Title, string? Summary, string Status,
    int CurrentAttempt, int RejectionCount, int MaxRejections, DateTime? EnqueuedAt, IReadOnlyList<ArtifactDto> Artifacts);

/// <summary>Latest version of each artifact type.</summary>
public sealed record ArtifactDto(Guid Id, string Type, int Version, string? FileUrl, string? LinkUrl, DateTime UploadedAt);
