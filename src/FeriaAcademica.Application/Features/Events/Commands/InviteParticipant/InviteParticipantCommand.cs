using Mediator;

namespace FeriaAcademica.Application.Features.Events.Commands.InviteParticipant;

/// <summary>A professor of the event invites a student by email; the professor becomes the reviewer of that student's works.</summary>
public sealed record InviteParticipantCommand(Guid EventId, Guid ProfessorId, string Email) : ICommand<InvitationCreated>;

/// <summary>The token is what the professor hands to the student; it is the only time it is shown.</summary>
public sealed record InvitationCreated(Guid Id, string Token, DateTime ExpiresAt);
