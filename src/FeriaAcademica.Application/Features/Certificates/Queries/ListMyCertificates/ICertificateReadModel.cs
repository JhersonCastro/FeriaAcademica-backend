namespace FeriaAcademica.Application.Features.Certificates.Queries.ListMyCertificates;

/// <summary>Reads straight from the database into a DTO, without going through the entity.</summary>
public interface ICertificateReadModel { Task<IReadOnlyList<MyCertificateDto>> ListAsync(Guid userId, CancellationToken ct); }
