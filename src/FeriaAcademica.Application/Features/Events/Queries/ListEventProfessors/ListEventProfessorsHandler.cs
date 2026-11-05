using Mediator;

namespace FeriaAcademica.Application.Features.Events.Queries.ListEventProfessors;

internal sealed class ListEventProfessorsHandler(IEventProfessorReadModel readModel) : IQueryHandler<ListEventProfessorsQuery, IReadOnlyList<ProfessorDto>>
{
    public async ValueTask<IReadOnlyList<ProfessorDto>> Handle(ListEventProfessorsQuery q, CancellationToken ct) => await readModel.ListAsync(q.EventId, ct);
}
