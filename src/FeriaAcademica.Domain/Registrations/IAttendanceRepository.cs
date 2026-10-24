namespace FeriaAcademica.Domain.Registrations;

public interface IAttendanceRepository
{
    Task<Attendance?> GetByRegistrationAsync(Guid registrationId, CancellationToken ct);

    /// <summary>
    /// Saves the attendance right away. Returns false, saving nothing, if the registration already has one:
    /// two scanners reading the same QR at the same time must still produce a single attendance.
    /// </summary>
    Task<bool> TryAddAsync(Attendance attendance, CancellationToken ct);
}
