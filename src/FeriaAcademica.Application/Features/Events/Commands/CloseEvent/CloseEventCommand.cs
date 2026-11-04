using Mediator;

namespace FeriaAcademica.Application.Features.Events.Commands.CloseEvent;

public sealed record CloseEventCommand(Guid EventId) : ICommand<Unit>;
