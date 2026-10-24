using Mediator;

namespace FeriaAcademica.Application.Features.Users.Queries.GetUser;

public sealed record GetUserQuery(Guid UserId) : IQuery<UserDto>;
