namespace FeriaAcademica.Domain.Works;

public interface IWorkRepository
{
    /// <summary>Loads the work with its artifacts and review queue, which its rules need.</summary>
    Task<Work?> GetAsync(Guid id, CancellationToken ct);
    Task AddAsync(Work work, CancellationToken ct);
}
