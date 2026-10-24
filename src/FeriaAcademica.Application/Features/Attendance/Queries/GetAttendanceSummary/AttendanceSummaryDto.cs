namespace FeriaAcademica.Application.Features.Attendance.Queries.GetAttendanceSummary;

/// <summary>Counts only active registrations. <paramref name="Pending"/> is registered people who have not entered yet.</summary>
public sealed record AttendanceSummaryDto(Guid EventId, int Registered, int Entered, int Pending, DateTime? LastEntryAt);
