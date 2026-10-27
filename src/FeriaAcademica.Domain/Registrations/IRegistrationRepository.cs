namespace FeriaAcademica.Domain.Registrations;

public interface IRegistrationRepository
{
    Task<Registration?> GetAsync(Guid eventId, Guid userId, CancellationToken ct);
    Task<Registration?> GetByQrTokenAsync(string qrToken, CancellationToken ct);
    Task AddAsync(Registration registration, CancellationToken ct);
}
