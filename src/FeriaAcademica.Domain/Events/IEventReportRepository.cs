namespace FeriaAcademica.Domain.Events;

public interface IEventReportRepository
{
    Task<EventReport?> GetByEventAsync(Guid eventId, CancellationToken ct);
    Task AddAsync(EventReport report, CancellationToken ct);
}
