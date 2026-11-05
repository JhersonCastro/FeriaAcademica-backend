using Mediator;

namespace FeriaAcademica.Application.Features.Events.Queries.GetEventReport;

public sealed record GetEventReportQuery(Guid EventId) : IQuery<EventReportDto?>;
