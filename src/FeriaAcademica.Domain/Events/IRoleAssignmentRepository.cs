namespace FeriaAcademica.Domain.Events;

public interface IRoleAssignmentRepository
{
    Task<bool> HasRoleAsync(Guid userId, Guid eventId, EventRole role, CancellationToken ct);
}
