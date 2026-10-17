namespace FeriaAcademica.Domain.Events;

public interface IParticipationRepository
{
    Task<Participation?> GetAsync(Guid id, CancellationToken ct);
}
