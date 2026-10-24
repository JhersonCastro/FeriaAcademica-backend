using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;

namespace FeriaAcademica.Domain.Registrations;

public enum RegistrationStatus { Registered, Cancelled }
public enum CertificateType { Attendance, Participation }

public enum CheckInOutcome { Entered, AlreadyEntered, RegistrationCancelled }

public sealed record CheckInResult(CheckInOutcome Outcome, Attendance? Attendance);

public sealed class Registration : Entity
{
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public string QrToken { get; private set; } = default!;   // what the person's QR code carries; unguessable and unique
    public RegistrationStatus Status { get; private set; } = RegistrationStatus.Registered;

    private Registration() { } // EF Core

    public static Registration Create(Guid eventId, Guid userId, string qrToken)
    {
        if (string.IsNullOrWhiteSpace(qrToken)) throw new DomainException("QR token is required.");
        return new Registration { EventId = eventId, UserId = userId, QrToken = qrToken };
    }

    /// <summary>
    /// Scanning the person's QR at the door. The first valid scan creates the attendance; scanning again never creates a second one
    /// and reports the existing one. A cancelled registration does not get in.
    /// </summary>
    public CheckInResult CheckIn(Attendance? existing, Guid validatedBy, DateTime now)
    {
        if (Status != RegistrationStatus.Registered) return new CheckInResult(CheckInOutcome.RegistrationCancelled, null);
        if (existing is not null) return new CheckInResult(CheckInOutcome.AlreadyEntered, existing);
        return new CheckInResult(CheckInOutcome.Entered, Attendance.Create(Id, validatedBy, now));
    }
}

public sealed class Attendance : Entity
{
    public Guid RegistrationId { get; private set; }
    public Guid ValidatedBy { get; private set; }
    public DateTime At { get; private set; }

    private Attendance() { } // EF Core

    public static Attendance Create(Guid registrationId, Guid validatedBy, DateTime now) =>
        new() { RegistrationId = registrationId, ValidatedBy = validatedBy, At = now };
}

/// <summary>Who may take attendance (scan QR codes) and see who has come in: the super admin and the event's coordinators and teachers.</summary>
public static class AttendancePolicy
{
    public static void EnsureCanManage(bool isSuperAdmin, IReadOnlyCollection<EventRole> eventRoles)
    {
        if (isSuperAdmin || eventRoles.Contains(EventRole.Coordinator) || eventRoles.Contains(EventRole.Teacher)) return;
        throw new DomainException("Only the super admin or a coordinator or teacher of the event can take attendance.");
    }
}

public sealed class Certificate : Entity
{
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? WorkId { get; private set; }
    public CertificateType Type { get; private set; }
    public string Code { get; private set; } = default!;
    public DateTime IssuedAt { get; private set; }

    private Certificate() { } // EF Core

    public static Certificate Create(Guid eventId, Guid userId, CertificateType type, Guid? workId, string code, DateTime now)
    {
        if ((type == CertificateType.Participation) != workId.HasValue)
            throw new DomainException("Only participation certificates reference a work, and they must.");
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("Code is required.");
        return new Certificate { EventId = eventId, UserId = userId, WorkId = workId, Type = type, Code = code, IssuedAt = now };
    }
}
