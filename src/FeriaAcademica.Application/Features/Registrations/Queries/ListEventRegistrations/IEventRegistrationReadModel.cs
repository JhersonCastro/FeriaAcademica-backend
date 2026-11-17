namespace FeriaAcademica.Application.Features.Registrations.Queries.ListEventRegistrations;

/// <summary>Reads straight from the database into a DTO, without going through the entity.</summary>
public interface IEventRegistrationReadModel { Task<IReadOnlyList<EventRegistrationDto>> ListAsync(Guid eventId, CancellationToken ct); }
