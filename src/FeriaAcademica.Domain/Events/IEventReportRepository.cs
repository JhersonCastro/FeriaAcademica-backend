namespace FeriaAcademica.Domain.Events;

/// <summary>Persistence of event reports; implemented in Infrastructure.</summary>
public interface IEventReportRepository
{
    Task<EventReport?> GetByEventAsync(Guid eventId, CancellationToken ct);
    Task AddAsync(EventReport report, CancellationToken ct);
}
