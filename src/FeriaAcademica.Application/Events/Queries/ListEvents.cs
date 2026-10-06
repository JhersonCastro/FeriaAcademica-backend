using FeriaAcademica.Application.Abstractions;

namespace FeriaAcademica.Application.Events.Queries;

public sealed record EventDto(Guid Id, string Title, string Category, string Organizer, DateOnly Date, int Capacity, string Status);

public sealed record ListEventsQuery : IQuery<IReadOnlyList<EventDto>>;

public interface IEventReadModel { Task<IReadOnlyList<EventDto>> ListAsync(CancellationToken ct); }

internal sealed class ListEventsHandler(IEventReadModel readModel) : IQueryHandler<ListEventsQuery, IReadOnlyList<EventDto>>
{
    public Task<IReadOnlyList<EventDto>> Handle(ListEventsQuery q, CancellationToken ct) => readModel.ListAsync(ct);
}
