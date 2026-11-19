using Mediator;

namespace FeriaAcademica.Application.Features.Institutions.Commands.DeactivateInstitution;

/// <summary>Returns false when the institution does not exist.</summary>
public sealed record DeactivateInstitutionCommand(Guid Id) : ICommand<bool>;
