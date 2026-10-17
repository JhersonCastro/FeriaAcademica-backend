using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Works;

namespace FeriaAcademica.Domain.Tests;

/// <summary>Executable specification of how an author uploads, submits and replaces a work (EP-02).</summary>
public class WorkSubmissionTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Deadline = T0.AddDays(1);
    private static readonly Guid Author = Guid.NewGuid();
    private static readonly Guid Professor = Guid.NewGuid();
    private const long OneMb = 1024 * 1024;

    private static Work NewDraft() => Work.Create(Guid.NewGuid(), Professor, "Solar tracker", null, 3, Author, T0);

    // ---- HU-010: upload a work within the deadline ----

    [Fact]
    public void Only_the_participant_can_manage_the_works_of_their_participation()
    {
        var participation = Participation.Create(Guid.NewGuid(), Author);

        participation.EnsureOwnedBy(Author);
        Assert.Throws<DomainException>(() => participation.EnsureOwnedBy(Guid.NewGuid()));
    }

    [Fact]
    public void A_work_cannot_be_created_after_the_deadline()
    {
        Assert.Throws<DomainException>(() =>
            Work.Create(Guid.NewGuid(), Professor, "Solar tracker", null, 3, Author, Deadline.AddMinutes(1), Deadline));
    }

    [Fact]
    public void A_pdf_within_the_size_limit_is_accepted()
    {
        var work = NewDraft();

        var artifact = work.AddFile(ArtifactType.Document, "https://files.example.org/report.pdf", "Report.PDF", 2 * OneMb, T0, Deadline);

        Assert.Equal("https://files.example.org/report.pdf", artifact.FileUrl);
        Assert.Equal(1, artifact.Version);
    }

    [Fact]
    public void A_file_in_a_format_that_is_not_allowed_is_rejected()
    {
        var work = NewDraft();

        var ex = Assert.Throws<DomainException>(() => work.AddFile(ArtifactType.Document, "https://x.org/report.docx", "report.docx", OneMb, T0));

        Assert.Contains(".pdf", ex.Message);
        Assert.Empty(work.Artifacts);
    }

    [Fact]
    public void A_file_larger_than_the_size_limit_is_rejected()
    {
        var work = NewDraft();

        Assert.Throws<DomainException>(() =>
            work.AddFile(ArtifactType.Document, "https://x.org/report.pdf", "report.pdf", ArtifactFileRules.MaxSizeBytes + 1, T0));
        Assert.Empty(work.Artifacts);
    }

    [Fact]
    public void The_default_file_rules_are_pdf_up_to_10_MB()
    {
        Assert.Equal([".pdf"], ArtifactFileRules.AllowedExtensions);
        Assert.Equal(10 * OneMb, ArtifactFileRules.MaxSizeBytes);
    }

    [Fact]
    public void An_artifact_cannot_be_added_after_the_deadline()
    {
        var work = NewDraft();

        Assert.Throws<DomainException>(() =>
            work.AddFile(ArtifactType.Document, "https://x.org/report.pdf", "report.pdf", OneMb, Deadline.AddMinutes(1), Deadline));
        Assert.Empty(work.Artifacts);
    }
}
