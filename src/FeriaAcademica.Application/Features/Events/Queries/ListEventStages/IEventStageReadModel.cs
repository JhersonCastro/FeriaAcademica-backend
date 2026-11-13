namespace FeriaAcademica.Application.Features.Events.Queries.ListEventStages;

public interface IEventStageReadModel { Task<IReadOnlyList<EventStageDto>?> ListAsync(Guid eventId, CancellationToken ct); }
