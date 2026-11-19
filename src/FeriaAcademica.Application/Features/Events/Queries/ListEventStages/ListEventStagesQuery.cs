using Mediator;

namespace FeriaAcademica.Application.Features.Events.Queries.ListEventStages;

/// <summary>The stages in the event's timeline (the removed ones are left out), in date order; null when the event does not exist.</summary>
public sealed record ListEventStagesQuery(Guid EventId) : IQuery<IReadOnlyList<EventStageDto>?>;

/// <param name="Type">Configuration, Call, Nomination, Reception, Admission, Registration, Evaluation, Results or Publication.</param>
/// <param name="Order">Position of the stage in a fair's natural sequence, from 1 (Configuration) to 9 (Publication).</param>
/// <param name="Status">Scheduled, Open or Closed. Dates are in UTC and null while the stage has none.</param>
public sealed record EventStageDto(string Type, int Order, string Status, DateTime? StartDate, DateTime? EndDate);
