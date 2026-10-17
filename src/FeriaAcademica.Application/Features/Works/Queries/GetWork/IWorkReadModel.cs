namespace FeriaAcademica.Application.Features.Works.Queries.GetWork;

/// <summary>Reads straight from the database into a DTO, without going through the entity.</summary>
public interface IWorkReadModel
{
    /// <summary>Only returns the work if <paramref name="authorId"/> is the participant it belongs to.</summary>
    Task<WorkDto?> GetAsync(Guid id, Guid authorId, CancellationToken ct);
}
