namespace FeriaAcademica.Application.Features.Institutions.Queries.ListInstitutions;

public interface IInstitutionReadModel { Task<IReadOnlyList<InstitutionDto>> ListAsync(CancellationToken ct); }
