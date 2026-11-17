using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Registrations;
using Mediator;

namespace FeriaAcademica.Application.Features.Registrations.Queries.ListEventRegistrations;

internal sealed class ListEventRegistrationsHandler(IAcademicEventRepository events, IRoleAssignmentRepository roles, IEventRegistrationReadModel registrations)
    : IQueryHandler<ListEventRegistrationsQuery, IReadOnlyList<EventRegistrationDto>>
{
    public async ValueTask<IReadOnlyList<EventRegistrationDto>> Handle(ListEventRegistrationsQuery q, CancellationToken ct)
    {
        var academicEvent = await events.GetAsync(q.EventId, ct) ?? throw new DomainException("Event not found.");
        AttendancePolicy.EnsureCanManage(q.ActorIsSuperAdmin, await roles.GetRolesAsync(q.ActorId, academicEvent.Id, ct));
        return await registrations.ListAsync(academicEvent.Id, ct);
    }
}
