using Mediator;

namespace FeriaAcademica.Application.Features.Events.Commands.CreateEvent;

public sealed record CreateEventCommand(Guid InstitutionId, string Name, Guid CreatedBy) : ICommand<Guid>;
