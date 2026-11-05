using Mediator;

namespace FeriaAcademica.Application.Features.Users.Queries.ListUsers;

/// <summary><paramref name="Role"/> is "SuperAdmin" or an event role (Coordinator, Teacher, Jury, Participant, Attendee).</summary>
public sealed record ListUsersQuery(string? Search, string? Role, string? Status, int Page = 1, int PageSize = 20) : IQuery<PagedResult<UserSummaryDto>>;
