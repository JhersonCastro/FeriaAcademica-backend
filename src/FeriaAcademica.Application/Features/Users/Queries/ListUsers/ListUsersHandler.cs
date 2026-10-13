using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Users;
using Mediator;

namespace FeriaAcademica.Application.Features.Users.Queries.ListUsers;

internal sealed class ListUsersHandler(IUserListReadModel users) : IQueryHandler<ListUsersQuery, PagedResult<UserSummaryDto>>
{
    private const string SuperAdminRole = "SuperAdmin";

    public async ValueTask<PagedResult<UserSummaryDto>> Handle(ListUsersQuery q, CancellationToken ct)
    {
        if (q.Page < 1) throw new DomainException("Page must be at least 1.");
        if (q.PageSize is < 1 or > 100) throw new DomainException("Page size must be between 1 and 100.");

        var superAdminsOnly = string.Equals(q.Role, SuperAdminRole, StringComparison.OrdinalIgnoreCase);
        EventRole? role = null;
        if (!superAdminsOnly && !string.IsNullOrWhiteSpace(q.Role))
            role = Enum.TryParse<EventRole>(q.Role, ignoreCase: true, out var parsed) ? parsed : throw new DomainException($"Unknown role '{q.Role}'.");

        UserStatus? status = null;
        if (!string.IsNullOrWhiteSpace(q.Status))
            status = Enum.TryParse<UserStatus>(q.Status, ignoreCase: true, out var parsed) ? parsed : throw new DomainException($"Unknown status '{q.Status}'.");

        var search = string.IsNullOrWhiteSpace(q.Search) ? null : q.Search.Trim();
        return await users.ListAsync(new UserFilter(search, superAdminsOnly, role, status), q.Page, q.PageSize, ct);
    }
}
