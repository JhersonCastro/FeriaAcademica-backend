using Mediator;

namespace FeriaAcademica.Application.Features.Users.Commands.UpdateMyProfile;

/// <summary>The signed-in user edits their own personal data. The email is not editable here.</summary>
public sealed record UpdateMyProfileCommand(
    Guid UserId, string FullName, string? PhoneNumber, DateOnly? BirthDate, string? GuardianName, string? GuardianEmail) : ICommand<Unit>;
