namespace FeriaAcademica.Application.Features.Events.Queries.ListEvents;

public interface IEventReadModel { Task<IReadOnlyList<EventDto>> ListAsync(CancellationToken ct); }
