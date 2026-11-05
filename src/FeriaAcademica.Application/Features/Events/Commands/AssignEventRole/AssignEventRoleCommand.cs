using Mediator;

namespace FeriaAcademica.Application.Features.Events.Commands.AssignEventRole;

/// <summary><paramref name="Role"/> is an event role: Coordinator, Teacher, Jury, Participant or Attendee.</summary>
public sealed record AssignEventRoleCommand(Guid EventId, Guid UserId, string Role) : ICommand<Unit>;
