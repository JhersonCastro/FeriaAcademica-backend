using Mediator;

namespace FeriaAcademica.Application.Features.Certificates.Queries.ListMyCertificates;

public sealed record ListMyCertificatesQuery(Guid UserId) : IQuery<IReadOnlyList<MyCertificateDto>>;
