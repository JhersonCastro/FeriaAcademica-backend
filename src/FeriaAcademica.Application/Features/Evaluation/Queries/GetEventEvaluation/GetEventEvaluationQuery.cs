using Mediator;

namespace FeriaAcademica.Application.Features.Evaluation.Queries.GetEventEvaluation;

public sealed record GetEventEvaluationQuery(Guid EventId, Guid ActorId, bool ActorIsSuperAdmin) : IQuery<EventEvaluationDto?>;
