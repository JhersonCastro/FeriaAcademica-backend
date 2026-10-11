using FeriaAcademica.Domain.Common;

namespace FeriaAcademica.Domain.Registrations;

public enum RegistrationStatus { Registered, Cancelled }
public enum CertificateType { Attendance, Participation }

public sealed class Registration : Entity
{
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public string QrToken { get; private set; } = default!;
    public RegistrationStatus Status { get; private set; } = RegistrationStatus.Registered;

    private Registration() { } // EF Core

    public static Registration Create(Guid eventId, Guid userId, string qrToken)
    {
        if (string.IsNullOrWhiteSpace(qrToken)) throw new DomainException("QR token is required.");
        return new Registration { EventId = eventId, UserId = userId, QrToken = qrToken };
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
