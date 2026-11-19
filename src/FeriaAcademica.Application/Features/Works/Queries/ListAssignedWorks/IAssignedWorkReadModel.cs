namespace FeriaAcademica.Application.Features.Works.Queries.ListAssignedWorks;

/// <summary>Reads straight from the database into a DTO, without going through the entity.</summary>
public interface IAssignedWorkReadModel
{
    Task<IReadOnlyList<AssignedWorkDto>> ListAsync(Guid reviewerId, CancellationToken ct);

    /// <summary>Works of the reviewer that were approved (admitted or already evaluated), the most recently decided first.</summary>
    Task<IReadOnlyList<AssignedWorkDto>> ListApprovedAsync(Guid reviewerId, CancellationToken ct);
}
