namespace FeriaAcademica.Domain.Events;

public interface IParticipationRepository
{
    Task<Participation?> GetAsync(Guid id, CancellationToken ct);
    Task<Participation?> GetByEventAndUserAsync(Guid eventId, Guid userId, CancellationToken ct);
    Task AddAsync(Participation participation, CancellationToken ct);
}
