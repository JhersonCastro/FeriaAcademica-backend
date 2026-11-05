using FeriaAcademica.Application.Features.Attendance.Queries.GetAttendanceSummary;
using FeriaAcademica.Domain.Works;
using Mediator;

namespace FeriaAcademica.Application.Features.Events.Queries.GetEventSummary;

internal sealed class GetEventSummaryHandler(IEventSummaryReadModel summaries, IAttendanceReadModel attendance) : IQueryHandler<GetEventSummaryQuery, EventSummaryDto?>
{
    public async ValueTask<EventSummaryDto?> Handle(GetEventSummaryQuery q, CancellationToken ct)
    {
        var counts = await summaries.GetWorkCountsAsync(q.EventId, ct);
        if (counts is null) return null;

        var works = WorkTally.From(counts.ReceivedWorksByStatus);
        var people = await attendance.GetSummaryAsync(q.EventId, ct);
        return new EventSummaryDto(q.EventId, counts.EventName, counts.EventStatus,
            works.Received, works.Approved, works.Rejected, works.InReview, works.ReturnedForChanges, works.Withdrawn,
            people.Registered, people.Entered, DateTime.UtcNow);
    }
}
