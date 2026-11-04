using Mediator;

namespace FeriaAcademica.Application.Features.Certificates.Queries.ListMyCertificates;

internal sealed class ListMyCertificatesHandler(ICertificateReadModel certificates) : IQueryHandler<ListMyCertificatesQuery, IReadOnlyList<MyCertificateDto>>
{
    public async ValueTask<IReadOnlyList<MyCertificateDto>> Handle(ListMyCertificatesQuery q, CancellationToken ct) => await certificates.ListAsync(q.UserId, ct);
}
