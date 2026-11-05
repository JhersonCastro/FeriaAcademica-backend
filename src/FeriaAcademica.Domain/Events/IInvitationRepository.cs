namespace FeriaAcademica.Domain.Events;

/// <summary>Persistence of invitations; implemented in Infrastructure.</summary>
public interface IInvitationRepository
{
    Task<Invitation?> GetByTokenAsync(string token, CancellationToken ct);
    Task AddAsync(Invitation invitation, CancellationToken ct);
}
