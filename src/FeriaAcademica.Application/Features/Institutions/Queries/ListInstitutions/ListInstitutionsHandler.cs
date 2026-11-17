using Mediator;

namespace FeriaAcademica.Application.Features.Institutions.Queries.ListInstitutions;

internal sealed class ListInstitutionsHandler(IInstitutionReadModel readModel) : IQueryHandler<ListInstitutionsQuery, IReadOnlyList<InstitutionDto>>
{
    public async ValueTask<IReadOnlyList<InstitutionDto>> Handle(ListInstitutionsQuery q, CancellationToken ct) => await readModel.ListAsync(ct);
}
