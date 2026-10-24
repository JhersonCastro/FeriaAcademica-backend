namespace FeriaAcademica.Application.Features.Registrations;

/// <summary><paramref name="QrToken"/> is the exact content the person's QR code must carry; the frontend draws the code from it.</summary>
public sealed record RegistrationDto(Guid Id, Guid EventId, string QrToken, string Status, DateTime? EnteredAt);
