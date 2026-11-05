namespace FeriaAcademica.Domain.Registrations;

/// <summary>Persistence of issued certificates; implemented in Infrastructure.</summary>
public interface ICertificateRepository
{
    Task<Certificate?> GetAsync(Guid eventId, Guid userId, CertificateType type, CancellationToken ct);
    Task AddAsync(Certificate certificate, CancellationToken ct);
}
