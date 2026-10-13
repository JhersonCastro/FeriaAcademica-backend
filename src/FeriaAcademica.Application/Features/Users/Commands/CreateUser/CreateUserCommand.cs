using Mediator;

namespace FeriaAcademica.Application.Features.Users.Commands.CreateUser;

/// <summary>The super admin creates the account; the user receives a link to set their own password.</summary>
public sealed record CreateUserCommand(string Email, string FullName, string? PhoneNumber) : ICommand<Guid>;
