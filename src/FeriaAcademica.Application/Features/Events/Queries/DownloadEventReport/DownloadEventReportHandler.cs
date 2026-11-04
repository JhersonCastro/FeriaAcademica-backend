using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Application.Features.Events.Queries.GetEventReport;
using FeriaAcademica.Domain.Common;
using Mediator;

namespace FeriaAcademica.Application.Features.Events.Queries.DownloadEventReport;

internal sealed class DownloadEventReportHandler(IEventReportReadModel reports, IPdfRenderer pdf) : IQueryHandler<DownloadEventReportQuery, PdfFile>
{
    public async ValueTask<PdfFile> Handle(DownloadEventReportQuery q, CancellationToken ct)
    {
        var report = await reports.GetAsync(q.EventId, ct) ?? throw new DomainException("The event report has not been generated yet.");
        return new PdfFile($"informe-{FileNames.Slug(report.EventName)}.pdf", pdf.RenderHtml(report.Html));
    }
}
