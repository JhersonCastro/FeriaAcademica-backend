using Mediator;

namespace FeriaAcademica.Application.Features.Events.Commands.PublishEvent;

public sealed record PublishEventCommand(Guid EventId) : ICommand<bool>;
