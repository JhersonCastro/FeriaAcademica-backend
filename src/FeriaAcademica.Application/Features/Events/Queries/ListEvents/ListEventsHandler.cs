using Mediator;

namespace FeriaAcademica.Application.Features.Events.Queries.ListEvents;

internal sealed class ListEventsHandler(IEventReadModel readModel) : IQueryHandler<ListEventsQuery, IReadOnlyList<EventDto>>
{
    public async ValueTask<IReadOnlyList<EventDto>> Handle(ListEventsQuery q, CancellationToken ct) => await readModel.ListAsync(ct);
}
