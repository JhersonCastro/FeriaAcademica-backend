namespace FeriaAcademica.Application.Features.Institutions.Queries.ListInstitutions;

/// <param name="EventCount">Events created in the institution, in any status.</param>
public sealed record InstitutionDto(Guid Id, string Name, string? Acronym, bool IsActive, int EventCount);
