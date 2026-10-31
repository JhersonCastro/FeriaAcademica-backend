namespace FeriaAcademica.Domain.Events;

public interface IInvitationRepository
{
    Task<Invitation?> GetAsync(Guid id, CancellationToken ct);
    Task<Invitation?> GetByTokenAsync(string token, CancellationToken ct);
    Task AddAsync(Invitation invitation, CancellationToken ct);
}
