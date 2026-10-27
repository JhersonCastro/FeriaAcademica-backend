using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Registrations;
using Mediator;

namespace FeriaAcademica.Application.Features.Attendance.Queries.GetAttendanceSummary;

internal sealed class GetAttendanceSummaryHandler(IAcademicEventRepository events, IRoleAssignmentRepository roles, IAttendanceReadModel summary)
    : IQueryHandler<GetAttendanceSummaryQuery, AttendanceSummaryDto>
{
    public async ValueTask<AttendanceSummaryDto> Handle(GetAttendanceSummaryQuery q, CancellationToken ct)
    {
        var academicEvent = await events.GetAsync(q.EventId, ct) ?? throw new DomainException("Event not found.");
        AttendancePolicy.EnsureCanManage(q.ActorIsSuperAdmin, await roles.GetRolesAsync(q.ActorId, academicEvent.Id, ct));
        return await summary.GetSummaryAsync(academicEvent.Id, ct);
    }
}
