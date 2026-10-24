namespace FeriaAcademica.Application.Features.Users.Queries.ListUsers;

public sealed record UserSummaryDto(Guid Id, string Email, string FullName, bool IsSuperAdmin, string Status, IReadOnlyList<string> Roles);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
