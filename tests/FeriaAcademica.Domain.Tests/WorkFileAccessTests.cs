using FeriaAcademica.Domain.Works;

namespace FeriaAcademica.Domain.Tests;

/// <summary>Executable specification of who may read the files uploaded to a work.</summary>
public class WorkFileAccessTests
{
    private static readonly Guid Author = Guid.NewGuid();
    private static readonly Guid Reviewer = Guid.NewGuid();

    [Fact]
    public void The_author_can_read_their_files() => Assert.True(WorkFileAccess.CanRead(Author, Author, Reviewer, isSuperAdmin: false));

    [Fact]
    public void The_reviewer_can_read_the_files() => Assert.True(WorkFileAccess.CanRead(Reviewer, Author, Reviewer, isSuperAdmin: false));

    [Fact]
    public void The_super_admin_can_read_any_file() => Assert.True(WorkFileAccess.CanRead(Guid.NewGuid(), Author, Reviewer, isSuperAdmin: true));

    [Fact]
    public void Anyone_else_cannot_read_the_files() => Assert.False(WorkFileAccess.CanRead(Guid.NewGuid(), Author, Reviewer, isSuperAdmin: false));
}
