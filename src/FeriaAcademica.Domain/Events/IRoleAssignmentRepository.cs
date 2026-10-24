namespace FeriaAcademica.Domain.Events;

public interface IRoleAssignmentRepository
{
    Task<IReadOnlyList<EventRole>> GetRolesAsync(Guid userId, Guid eventId, CancellationToken ct);
    Task AddAsync(RoleAssignment assignment, CancellationToken ct);
}
