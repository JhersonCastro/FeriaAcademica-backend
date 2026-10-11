using Mediator;

namespace FeriaAcademica.Application.Features.Auth.Commands.RegisterUser;

public sealed record RegisterUserCommand(string Email, string Password, string FullName) : ICommand<UserDto>;
