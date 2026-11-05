using Mediator;

namespace FeriaAcademica.Application.Features.Events.Queries.GetMyParticipation;

public sealed record GetMyParticipationQuery(Guid EventId, Guid UserId) : IQuery<ParticipationDto?>;
