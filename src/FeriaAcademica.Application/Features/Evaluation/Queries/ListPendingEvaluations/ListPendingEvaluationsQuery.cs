using Mediator;

namespace FeriaAcademica.Application.Features.Evaluation.Queries.ListPendingEvaluations;

public sealed record ListPendingEvaluationsQuery(Guid UserId) : IQuery<IReadOnlyList<PendingEvaluationDto>>;
