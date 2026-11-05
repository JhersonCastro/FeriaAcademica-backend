namespace FeriaAcademica.Application.Features.Events.Queries.GetMyParticipation;

/// <param name="Id">What a new work is created under (<c>participationId</c>).</param>
/// <param name="InvitedBy">The professor whose invitation made the person a participant; null when there was no invitation.</param>
public sealed record ParticipationDto(Guid Id, Guid EventId, Guid? InvitedBy);
