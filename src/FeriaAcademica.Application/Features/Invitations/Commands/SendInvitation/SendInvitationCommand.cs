using Mediator;

namespace FeriaAcademica.Application.Features.Invitations.Commands.SendInvitation;

/// <summary>A teacher of the event invites someone by email, with or without an account. <paramref name="Role"/> is Participant or Attendee.</summary>
public sealed record SendInvitationCommand(Guid EventId, string Email, string Role, Guid SenderId) : ICommand<Guid>;
