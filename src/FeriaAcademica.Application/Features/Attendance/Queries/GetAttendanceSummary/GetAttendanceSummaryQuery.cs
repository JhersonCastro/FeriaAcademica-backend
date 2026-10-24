using Mediator;

namespace FeriaAcademica.Application.Features.Attendance.Queries.GetAttendanceSummary;

public sealed record GetAttendanceSummaryQuery(Guid EventId, Guid ActorId, bool ActorIsSuperAdmin) : IQuery<AttendanceSummaryDto>;
