using FeriaAcademica.Application.Features.Users.Queries.GetUser;
using Mediator;

namespace FeriaAcademica.Application.Features.Auth.Commands.SignIn;

public sealed record SignInCommand(string Email, string Password, bool Remember) : ICommand<UserDto>;
