using Mediator;

namespace FeriaAcademica.Application.Features.Institutions.Commands.ActivateInstitution;

/// <summary>Returns false when the institution does not exist.</summary>
public sealed record ActivateInstitutionCommand(Guid Id) : ICommand<bool>;
