using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Works;

namespace FeriaAcademica.Domain.Registrations;

public enum RegistrationStatus { Registered, Cancelled }
public enum CertificateType { Attendance, Participation, Recognition }

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

/// <summary>
/// Who may download a certificate: only once the event is over, and only a person who attended (attendance certificate),
/// who delivered the final version of their work (participation certificate) or who helped run the event as a
/// coordinator, teacher or jury member (recognition certificate).
/// </summary>
public static class CertificateEligibility
{
    public static readonly IReadOnlyList<EventRole> RecognizedRoles = [EventRole.Coordinator, EventRole.Teacher, EventRole.Jury];

    public static void EnsureCanDownload(CertificateType type, AcademicEvent academicEvent, bool attended, Work? workWithFinalVersion, IReadOnlyCollection<EventRole>? eventRoles = null)
    {
        if (!academicEvent.IsFinished) throw new DomainException("Certificates are available once the event has finished.");
        switch (type)
        {
            case CertificateType.Attendance when !attended:
                throw new DomainException("An attendance certificate requires having attended the event.");
            case CertificateType.Participation when workWithFinalVersion is null || !workWithFinalVersion.HasFinalVersion:
                throw new DomainException("A participation certificate requires a work with its final version delivered.");
            case CertificateType.Recognition when eventRoles is null || !eventRoles.Any(RecognizedRoles.Contains):
                throw new DomainException("A recognition certificate requires being a coordinator, teacher or jury member of the event.");
        }
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
