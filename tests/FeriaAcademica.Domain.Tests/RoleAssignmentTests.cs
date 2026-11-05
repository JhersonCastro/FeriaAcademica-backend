using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;

namespace FeriaAcademica.Domain.Tests;

public class RoleAssignmentTests
{
    private static readonly Guid User = Guid.NewGuid();
    private static readonly Guid Event = Guid.NewGuid();

    [Fact]
    public void A_user_can_hold_several_roles_in_the_same_event()
    {
        var assignment = RoleAssignment.Assign(User, Event, EventRole.Jury, [EventRole.Teacher]);
        Assert.Equal((User, Event, EventRole.Jury), (assignment.UserId, assignment.EventId, assignment.Role));
    }

    [Fact]
    public void A_user_cannot_get_the_same_role_twice_in_an_event() =>
        Assert.Throws<DomainException>(() => RoleAssignment.Assign(User, Event, EventRole.Teacher, [EventRole.Teacher]));
}
