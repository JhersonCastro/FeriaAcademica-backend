namespace FeriaAcademica.Application.Features.Registrations.Queries.ListEventRegistrations;

/// <param name="Status">Registered or Cancelled.</param>
/// <param name="EnteredAt">When the person's QR was first scanned at the door, in UTC; null if they have not entered.</param>
public sealed record EventRegistrationDto(Guid RegistrationId, Guid UserId, string FullName, string Email, string Status, DateTime? EnteredAt);
