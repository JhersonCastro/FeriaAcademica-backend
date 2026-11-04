namespace FeriaAcademica.Application.Features.Events.Queries.GetEventReport;

/// <summary>Reads straight from the database into a DTO, without going through the entity.</summary>
public interface IEventReportReadModel { Task<EventReportDto?> GetAsync(Guid eventId, CancellationToken ct); }
