using Mediator;

namespace FeriaAcademica.Application.Features.Evaluation.Queries.ListPendingEvaluations;

internal sealed class ListPendingEvaluationsHandler(IEvaluationReadModel readModel) : IQueryHandler<ListPendingEvaluationsQuery, IReadOnlyList<PendingEvaluationDto>>
{
    public async ValueTask<IReadOnlyList<PendingEvaluationDto>> Handle(ListPendingEvaluationsQuery q, CancellationToken ct) => await readModel.ListPendingAsync(q.UserId, ct);
}
