using Mediator;

namespace FeriaAcademica.Application.Features.Events.Queries.ListEventStages;

internal sealed class ListEventStagesHandler(IEventStageReadModel readModel) : IQueryHandler<ListEventStagesQuery, IReadOnlyList<EventStageDto>?>
{
    public async ValueTask<IReadOnlyList<EventStageDto>?> Handle(ListEventStagesQuery q, CancellationToken ct) => await readModel.ListAsync(q.EventId, ct);
}
