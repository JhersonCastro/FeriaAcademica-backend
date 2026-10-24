using Mediator;

namespace FeriaAcademica.Application.Features.Attendance.Commands.ScanAttendance;

/// <summary>Someone at the door scans a QR code: <paramref name="QrToken"/> is the content the frontend read from it.</summary>
public sealed record ScanAttendanceCommand(Guid EventId, string QrToken, Guid ActorId, bool ActorIsSuperAdmin) : ICommand<ScanResultDto>;

public enum ScanOutcome { Valid, AlreadyEntered, Invalid }

/// <summary>A scan always gets a clear answer for the door screen; only misuse (no permission, event not live) is an error.</summary>
public sealed record ScanResultDto(string Outcome, string Message, string? AttendeeName, DateTime? EnteredAt);
