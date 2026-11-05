namespace FeriaAcademica.Domain.Events;

/// <summary>Persistence of the roles people hold in events; implemented in Infrastructure.</summary>
public interface IRoleAssignmentRepository
{
    Task<IReadOnlyList<EventRole>> GetRolesAsync(Guid userId, Guid eventId, CancellationToken ct);
    Task AddAsync(RoleAssignment assignment, CancellationToken ct);
}
