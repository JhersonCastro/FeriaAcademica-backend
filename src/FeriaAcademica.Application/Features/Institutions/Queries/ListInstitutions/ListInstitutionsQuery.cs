using Mediator;

namespace FeriaAcademica.Application.Features.Institutions.Queries.ListInstitutions;

public sealed record ListInstitutionsQuery : IQuery<IReadOnlyList<InstitutionDto>>;
