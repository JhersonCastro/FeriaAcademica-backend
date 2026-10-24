namespace FeriaAcademica.Application.Features.Attendance.Queries.GetAttendanceSummary;

public interface IAttendanceReadModel
{
    Task<AttendanceSummaryDto> GetSummaryAsync(Guid eventId, CancellationToken ct);
}
