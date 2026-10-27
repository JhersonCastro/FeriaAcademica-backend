namespace FeriaAcademica.Domain.Registrations;

public interface IRegistrationRepository
{
    Task<Registration?> GetAsync(Guid eventId, Guid userId, CancellationToken ct);
    Task<Registration?> GetByQrTokenAsync(string qrToken, CancellationToken ct);
    /// <summary>Registrations of the event that hold a seat (not cancelled).</summary>
    Task<int> CountActiveAsync(Guid eventId, CancellationToken ct);
    Task AddAsync(Registration registration, CancellationToken ct);
}
