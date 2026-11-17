using Mediator;

namespace FeriaAcademica.Application.Features.Events.Commands.CreateEvent;

/// <param name="Stages">The timeline programmed in the creation form; it can be left out and scheduled later.</param>
public sealed record CreateEventCommand(Guid InstitutionId, string Name, Guid CreatedBy, int? Capacity = null, IReadOnlyList<NewEventStage>? Stages = null) : ICommand<Guid>;

/// <param name="Type">Configuration, Call, Nomination, Reception, Admission, Registration, Evaluation, Results or Publication.</param>
public sealed record NewEventStage(string Type, DateTime StartDate, DateTime EndDate);
