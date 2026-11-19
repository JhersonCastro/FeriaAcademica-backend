using Mediator;

namespace FeriaAcademica.Application.Features.Institutions.Commands.UpdateInstitution;

/// <summary>Returns false when the institution does not exist.</summary>
public sealed record UpdateInstitutionCommand(Guid Id, string Name, string? Acronym) : ICommand<bool>;
