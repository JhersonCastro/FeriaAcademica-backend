using FeriaAcademica.Application.Abstractions;
using Mediator;

namespace FeriaAcademica.Application.Features.Events.Queries.DownloadEventReport;

public sealed record DownloadEventReportQuery(Guid EventId) : IQuery<PdfFile>;
