namespace FeriaAcademica.Application.Features.Events.Queries.ListEventStages;

/// <param name="now">The instant against which each stage is Scheduled, Open or Closed.</param>
public interface IEventStageReadModel { Task<IReadOnlyList<EventStageDto>?> ListAsync(Guid eventId, DateTime now, CancellationToken ct); }
