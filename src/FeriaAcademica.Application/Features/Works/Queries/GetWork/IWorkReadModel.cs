namespace FeriaAcademica.Application.Features.Works.Queries.GetWork;

/// <summary>Reads straight from the database into a DTO, without going through the entity.</summary>
public interface IWorkReadModel { Task<WorkDto?> GetAsync(Guid id, CancellationToken ct); }
