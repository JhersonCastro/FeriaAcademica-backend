using Mediator;

namespace FeriaAcademica.Application.Features.Institutions.Commands.CreateInstitution;

public sealed record CreateInstitutionCommand(string Name, string? Acronym) : ICommand<Guid>;
