using Mediator;

namespace FeriaAcademica.Application.Features.Events.Queries.ListEvents;

public sealed record ListEventsQuery : IQuery<IReadOnlyList<EventDto>>;
