namespace FeriaAcademica.Application.Features.Events.Queries.GetEventSummary;

/// <summary>
/// <paramref name="WorksReceived"/> is the sum of the other work figures. <paramref name="Registered"/> and <paramref name="Attended"/> count only active registrations.
/// </summary>
public sealed record EventSummaryDto(
    Guid EventId, string EventName, string EventStatus,
    int WorksReceived, int WorksApproved, int WorksRejected, int WorksInReview, int WorksReturnedForChanges, int WorksWithdrawn,
    int Registered, int Attended, DateTime GeneratedAt);
