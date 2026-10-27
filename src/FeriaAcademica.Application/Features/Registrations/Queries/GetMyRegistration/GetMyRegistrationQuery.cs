using Mediator;

namespace FeriaAcademica.Application.Features.Registrations.Queries.GetMyRegistration;

public sealed record GetMyRegistrationQuery(Guid EventId, Guid UserId) : IQuery<RegistrationDto>;
