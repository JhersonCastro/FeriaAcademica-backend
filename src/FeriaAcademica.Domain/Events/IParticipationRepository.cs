namespace FeriaAcademica.Domain.Events;

/// <summary>Persistence of participations; implemented in Infrastructure.</summary>
public interface IParticipationRepository
{
    Task<Participation?> GetAsync(Guid id, CancellationToken ct);
}
