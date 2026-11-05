namespace FeriaAcademica.Application.Features.Invitations.Queries.GetInvitation;

/// <summary><paramref name="HasAccount"/> tells the frontend whether to ask the person to sign in or to create their account.</summary>
public sealed record InvitationDto(Guid EventId, string EventName, string Email, string Role, string Status, DateTime ExpiresAt, bool HasAccount);
