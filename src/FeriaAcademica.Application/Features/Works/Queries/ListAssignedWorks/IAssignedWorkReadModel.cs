namespace FeriaAcademica.Application.Features.Works.Queries.ListAssignedWorks;

/// <summary>Reads straight from the database into a DTO, without going through the entity.</summary>
public interface IAssignedWorkReadModel { Task<IReadOnlyList<AssignedWorkDto>> ListAsync(Guid reviewerId, CancellationToken ct); }
