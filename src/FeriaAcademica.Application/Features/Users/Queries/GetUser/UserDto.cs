namespace FeriaAcademica.Application.Features.Users.Queries.GetUser;

public sealed record EventRoleDto(Guid EventId, string EventName, string Role);

public sealed record UserDto(
    Guid Id, string Email, string FullName, string? PhoneNumber, DateOnly? BirthDate, string? GuardianName, string? GuardianEmail,
    bool IsSuperAdmin, string Status, DateTime CreatedAt, IReadOnlyList<EventRoleDto> EventRoles);
