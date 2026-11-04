namespace FeriaAcademica.Application.Features.Events.Queries.GetEventReport;

public sealed record EventReportDto(Guid EventId, string EventName, string Html, DateTime UpdatedAt);
