using Mediator;

namespace FeriaAcademica.Application.Features.Registrations.Commands.RegisterForEvent;

/// <summary>The signed-in user registers for an event and gets their personal QR. Registering again returns the same QR.</summary>
public sealed record RegisterForEventCommand(Guid EventId, Guid UserId) : ICommand<RegistrationDto>;
