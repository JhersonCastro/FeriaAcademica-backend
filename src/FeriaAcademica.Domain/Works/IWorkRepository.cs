namespace FeriaAcademica.Domain.Works;

/// <summary>Persistence of works; implemented in Infrastructure.</summary>
public interface IWorkRepository
{
    /// <summary>Loads the work with its artifacts and review queue, which its rules need.</summary>
    Task<Work?> GetAsync(Guid id, CancellationToken ct);
    /// <summary>The user's work in the event that already has its final version, if any.</summary>
    Task<Work?> GetWithFinalVersionAsync(Guid eventId, Guid userId, CancellationToken ct);
    Task AddAsync(Work work, CancellationToken ct);
}
