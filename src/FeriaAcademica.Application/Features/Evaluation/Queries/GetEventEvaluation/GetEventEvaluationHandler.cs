using FeriaAcademica.Domain.Evaluation;
using FeriaAcademica.Domain.Events;
using Mediator;

namespace FeriaAcademica.Application.Features.Evaluation.Queries.GetEventEvaluation;

internal sealed class GetEventEvaluationHandler(IRoleAssignmentRepository roles, IEvaluationReadModel readModel) : IQueryHandler<GetEventEvaluationQuery, EventEvaluationDto?>
{
    public async ValueTask<EventEvaluationDto?> Handle(GetEventEvaluationQuery q, CancellationToken ct)
    {
        EvaluationPolicy.EnsureCanManage(q.ActorIsSuperAdmin, await roles.GetRolesAsync(q.ActorId, q.EventId, ct));
        return await readModel.GetEventEvaluationAsync(q.EventId, ct);
    }
}
