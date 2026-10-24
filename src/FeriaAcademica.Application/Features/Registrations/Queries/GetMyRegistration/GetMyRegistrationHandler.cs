using FeriaAcademica.Domain.Common;
using Mediator;

namespace FeriaAcademica.Application.Features.Registrations.Queries.GetMyRegistration;

internal sealed class GetMyRegistrationHandler(IRegistrationReadModel registrations) : IQueryHandler<GetMyRegistrationQuery, RegistrationDto>
{
    public async ValueTask<RegistrationDto> Handle(GetMyRegistrationQuery q, CancellationToken ct) =>
        await registrations.GetAsync(q.EventId, q.UserId, ct) ?? throw new DomainException("You are not registered for this event.");
}
