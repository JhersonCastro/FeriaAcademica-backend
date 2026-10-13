using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Users;

namespace FeriaAcademica.Application.Features.Users.Queries.ListUsers;

/// <summary>Search matches name or email. Role filters super admins, or users holding that role in at least one event.</summary>
public sealed record UserFilter(string? Search, bool SuperAdminsOnly, EventRole? Role, UserStatus? Status);

public interface IUserListReadModel
{
    Task<PagedResult<UserSummaryDto>> ListAsync(UserFilter filter, int page, int pageSize, CancellationToken ct);
}
