using Mediator;

namespace FeriaAcademica.Application.Features.Invitations.Commands.AcceptInvitation;

/// <summary>Accepts a participant invitation and returns the participation that the user's works hang from.</summary>
public sealed record AcceptInvitationCommand(string Token, Guid UserId, string UserEmail) : ICommand<Guid>;
