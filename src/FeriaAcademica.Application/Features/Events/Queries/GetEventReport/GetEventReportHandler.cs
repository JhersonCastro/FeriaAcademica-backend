using Mediator;

namespace FeriaAcademica.Application.Features.Events.Queries.GetEventReport;

internal sealed class GetEventReportHandler(IEventReportReadModel reports) : IQueryHandler<GetEventReportQuery, EventReportDto?>
{
    public async ValueTask<EventReportDto?> Handle(GetEventReportQuery q, CancellationToken ct) => await reports.GetAsync(q.EventId, ct);
}
