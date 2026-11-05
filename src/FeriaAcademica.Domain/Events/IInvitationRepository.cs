namespace FeriaAcademica.Domain.Events;

public interface IInvitationRepository
{
    Task<Invitation?> GetByTokenAsync(string token, CancellationToken ct);
    Task AddAsync(Invitation invitation, CancellationToken ct);
}
