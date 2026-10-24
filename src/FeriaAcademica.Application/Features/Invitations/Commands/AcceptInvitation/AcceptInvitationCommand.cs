using Mediator;

namespace FeriaAcademica.Application.Features.Invitations.Commands.AcceptInvitation;

/// <summary>
/// An invited person with an account must be signed in as that account (<paramref name="CurrentUserId"/>).
/// One without an account creates it here with <paramref name="FullName"/> and <paramref name="Password"/>.
/// </summary>
public sealed record AcceptInvitationCommand(string Token, Guid? CurrentUserId, string? FullName, string? Password) : ICommand<Unit>;
