using FeriaAcademica.Domain.Common;
using Mediator;

namespace FeriaAcademica.Application.Features.Users.Queries.GetUser;

internal sealed class GetUserHandler(IUserReadModel users) : IQueryHandler<GetUserQuery, UserDto>
{
    public async ValueTask<UserDto> Handle(GetUserQuery q, CancellationToken ct) =>
        await users.GetAsync(q.UserId, ct) ?? throw new DomainException("User not found.");
}
