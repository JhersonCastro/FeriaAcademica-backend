using Mediator;

namespace FeriaAcademica.Application.Features.Registrations.Queries.ListEventRegistrations;

/// <summary>Everyone registered for the event, cancelled registrations included, sorted by name.</summary>
public sealed record ListEventRegistrationsQuery(Guid EventId, Guid ActorId, bool ActorIsSuperAdmin) : IQuery<IReadOnlyList<EventRegistrationDto>>;
