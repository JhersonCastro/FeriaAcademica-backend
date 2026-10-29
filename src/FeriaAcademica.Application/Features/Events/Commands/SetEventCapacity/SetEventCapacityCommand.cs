using Mediator;

namespace FeriaAcademica.Application.Features.Events.Commands.SetEventCapacity;

/// <summary>Sets how many people can register for the event; null removes the limit.</summary>
public sealed record SetEventCapacityCommand(Guid EventId, int? Capacity) : ICommand<Unit>;
