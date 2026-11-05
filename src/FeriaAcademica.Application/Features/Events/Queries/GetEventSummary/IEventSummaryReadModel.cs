using FeriaAcademica.Domain.Works;

namespace FeriaAcademica.Application.Features.Events.Queries.GetEventSummary;

/// <param name="ReceivedWorksByStatus">How many works that were submitted at least once are in each status now.</param>
public sealed record EventWorkCounts(string EventName, string EventStatus, IReadOnlyDictionary<WorkStatus, int> ReceivedWorksByStatus);

/// <summary>Reads straight from the database, without going through the entity.</summary>
public interface IEventSummaryReadModel { Task<EventWorkCounts?> GetWorkCountsAsync(Guid eventId, CancellationToken ct); }
