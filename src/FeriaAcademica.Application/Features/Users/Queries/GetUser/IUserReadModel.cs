namespace FeriaAcademica.Application.Features.Users.Queries.GetUser;

public interface IUserReadModel
{
    Task<UserDto?> GetAsync(Guid id, CancellationToken ct);
}
