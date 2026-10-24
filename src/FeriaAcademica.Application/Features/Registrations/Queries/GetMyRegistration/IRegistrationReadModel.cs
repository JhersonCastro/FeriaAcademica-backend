namespace FeriaAcademica.Application.Features.Registrations.Queries.GetMyRegistration;

public interface IRegistrationReadModel
{
    Task<RegistrationDto?> GetAsync(Guid eventId, Guid userId, CancellationToken ct);
}
