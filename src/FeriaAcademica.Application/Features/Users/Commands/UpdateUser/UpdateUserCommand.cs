using Mediator;

namespace FeriaAcademica.Application.Features.Users.Commands.UpdateUser;

public sealed record UpdateUserCommand(
    Guid UserId, string Email, string FullName, string? PhoneNumber, DateOnly? BirthDate, string? GuardianName, string? GuardianEmail) : ICommand<Unit>;
