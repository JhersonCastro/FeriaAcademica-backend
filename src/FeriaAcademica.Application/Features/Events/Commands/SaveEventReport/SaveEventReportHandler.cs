using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using Mediator;

namespace FeriaAcademica.Application.Features.Events.Commands.SaveEventReport;

internal sealed class SaveEventReportHandler(IAcademicEventRepository events, IEventReportRepository reports, IUnitOfWork uow)
    : ICommandHandler<SaveEventReportCommand, Unit>
{
    public async ValueTask<Unit> Handle(SaveEventReportCommand c, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(c.Html)) throw new DomainException("The report content is required.");
        var academicEvent = await events.GetAsync(c.EventId, ct) ?? throw new DomainException("Event not found.");

        var now = DateTime.UtcNow;
        var report = await reports.GetByEventAsync(academicEvent.Id, ct);
        if (report is null) await reports.AddAsync(EventReport.Create(academicEvent.Id, c.Html, c.ActorId, now), ct);
        else report.Update(c.Html, c.ActorId, now);

        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
