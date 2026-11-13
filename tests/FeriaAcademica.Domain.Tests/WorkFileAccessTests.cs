using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Works;

namespace FeriaAcademica.Domain.Tests;

/// <summary>Executable specification of who may read the files uploaded to a work.</summary>
public class WorkFileAccessTests
{
    private static readonly Guid Author = Guid.NewGuid();
    private static readonly Guid Reviewer = Guid.NewGuid();
    private static readonly EventRole[] NoRoles = [];

    private static bool CanRead(Guid user, EventRole[]? roles = null, bool superAdmin = false, bool assignedJury = false) =>
        WorkFileAccess.CanRead(user, Author, Reviewer, superAdmin, roles ?? NoRoles, assignedJury);

    [Fact]
    public void The_author_can_read_their_files() => Assert.True(CanRead(Author));

    [Fact]
    public void The_reviewer_can_read_the_files() => Assert.True(CanRead(Reviewer));

    [Fact]
    public void The_super_admin_can_read_any_file() => Assert.True(CanRead(Guid.NewGuid(), superAdmin: true));

    [Fact]
    public void A_coordinator_of_the_event_can_read_the_files() => Assert.True(CanRead(Guid.NewGuid(), [EventRole.Coordinator]));

    [Fact]
    public void A_jury_member_assigned_to_the_work_can_read_the_files() => Assert.True(CanRead(Guid.NewGuid(), [EventRole.Jury], assignedJury: true));

    [Fact]
    public void A_jury_member_who_is_not_assigned_to_the_work_cannot_read_the_files() => Assert.False(CanRead(Guid.NewGuid(), [EventRole.Jury]));

    [Theory]
    [InlineData(EventRole.Teacher)]
    [InlineData(EventRole.Participant)]
    [InlineData(EventRole.Attendee)]
    public void Other_roles_of_the_event_do_not_give_access(EventRole role) => Assert.False(CanRead(Guid.NewGuid(), [role]));

    [Fact]
    public void Anyone_else_cannot_read_the_files() => Assert.False(CanRead(Guid.NewGuid()));
}
