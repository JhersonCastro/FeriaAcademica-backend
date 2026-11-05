namespace FeriaAcademica.Domain.Registrations;

public interface ICertificateRepository
{
    Task<Certificate?> GetAsync(Guid eventId, Guid userId, CertificateType type, CancellationToken ct);
    Task AddAsync(Certificate certificate, CancellationToken ct);
}
