using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Application.Features.Users;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using Mediator;

namespace FeriaAcademica.Application.Features.Events.Commands.AssignEventRole;

internal sealed class AssignEventRoleHandler(IAcademicEventRepository events, IUserAccounts accounts, IRoleAssignmentRepository roles, IUnitOfWork uow)
    : ICommandHandler<AssignEventRoleCommand, Unit>
{
    public async ValueTask<Unit> Handle(AssignEventRoleCommand c, CancellationToken ct)
    {
        if (!Enum.TryParse<EventRole>(c.Role, ignoreCase: true, out var role)) throw new DomainException($"Unknown role '{c.Role}'.");
        var academicEvent = await events.GetAsync(c.EventId, ct) ?? throw new DomainException("Event not found.");
        var user = await accounts.FindAsync(c.UserId, ct) ?? throw new DomainException("User not found.");

        var current = await roles.GetRolesAsync(user.Id, academicEvent.Id, ct);
        await roles.AddAsync(RoleAssignment.Assign(user.Id, academicEvent.Id, role, current), ct);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
