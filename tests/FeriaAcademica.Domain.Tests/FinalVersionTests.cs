using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Works;

namespace FeriaAcademica.Domain.Tests;

/// <summary>Executable specification of the final version of an approved work (HU-030).</summary>
public class FinalVersionTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Author = Guid.NewGuid();
    private static readonly Guid Professor = Guid.NewGuid();

    private static Work NewWork(WorkStatus target)
    {
        var work = Work.Create(Guid.NewGuid(), Professor, "Solar tracker", null, 3, Author, T0);
        if (target == WorkStatus.Draft) return work;
        work.AddArtifact(ArtifactType.Document, "draft.pdf", null, T0);
        work.Submit(Author, T0.AddMinutes(1));
        if (target == WorkStatus.Submitted) return work;
        work.StartReview(Professor, T0.AddMinutes(2));
        if (target == WorkStatus.InReview) return work;
        work.Admit(Professor, T0.AddMinutes(3));
        return work;
    }

    [Fact]
    public void An_approved_work_accepts_its_final_version_and_marks_it_as_final()
    {
        var work = NewWork(WorkStatus.Admitted);

        var artifact = work.SubmitFinalVersion("final.pdf", "final.pdf", 2_000_000, T0.AddDays(1));

        Assert.True(artifact.IsFinal);
        Assert.Equal(ArtifactType.Document, artifact.Type);
        Assert.Equal("final.pdf", artifact.FileUrl);
        Assert.True(work.HasFinalVersion);
        Assert.Equal(WorkStatus.Admitted, work.Status);   // delivering it does not change the state of the work
    }

    [Fact]
    public void The_final_version_is_a_new_version_of_the_documents_and_keeps_the_earlier_ones()
    {
        var work = NewWork(WorkStatus.Admitted);

        var artifact = work.SubmitFinalVersion("final.pdf", "final.pdf", 1000, T0.AddDays(1));

        Assert.Equal(2, artifact.Version);
        Assert.Equal(2, work.Artifacts.Count);
        Assert.Single(work.Artifacts, a => a.IsFinal);
    }

    [Fact]
    public void An_evaluated_work_can_also_deliver_its_final_version()
    {
        var work = NewWork(WorkStatus.Admitted);
        typeof(Work).GetProperty(nameof(Work.Status))!.SetValue(work, WorkStatus.Evaluated);   // the jury step is not modelled yet

        Assert.True(work.SubmitFinalVersion("final.pdf", "final.pdf", 1000, T0).IsFinal);
    }

    [Theory]
    [InlineData(WorkStatus.Draft)]
    [InlineData(WorkStatus.Submitted)]
    [InlineData(WorkStatus.InReview)]
    public void A_work_that_is_not_approved_cannot_deliver_a_final_version(WorkStatus status)
    {
        var work = NewWork(status);

        var ex = Assert.Throws<DomainException>(() => work.SubmitFinalVersion("final.pdf", "final.pdf", 1000, T0.AddDays(1)));

        Assert.Contains("approved", ex.Message);
        Assert.False(work.HasFinalVersion);
    }

    [Fact]
    public void A_work_cannot_deliver_two_final_versions()
    {
        var work = NewWork(WorkStatus.Admitted);
        work.SubmitFinalVersion("final.pdf", "final.pdf", 1000, T0.AddDays(1));

        var ex = Assert.Throws<DomainException>(() => work.SubmitFinalVersion("final-2.pdf", "final-2.pdf", 1000, T0.AddDays(2)));

        Assert.Contains("already", ex.Message);
        Assert.Single(work.Artifacts, a => a.IsFinal);
    }

    [Theory]
    [InlineData("final.docx")]
    [InlineData("final.exe")]
    [InlineData("final")]
    public void The_final_version_must_have_an_allowed_format(string fileName)
    {
        var work = NewWork(WorkStatus.Admitted);

        var ex = Assert.Throws<DomainException>(() => work.SubmitFinalVersion("url", fileName, 1000, T0.AddDays(1)));

        Assert.Contains("format", ex.Message);
        Assert.False(work.HasFinalVersion);
    }

    [Fact]
    public void The_final_version_must_not_exceed_the_maximum_size()
    {
        var work = NewWork(WorkStatus.Admitted);

        var ex = Assert.Throws<DomainException>(() => work.SubmitFinalVersion("url", "final.pdf", ArtifactFileRules.MaxSizeBytes + 1, T0.AddDays(1)));

        Assert.Contains("maximum size", ex.Message);
        Assert.False(work.HasFinalVersion);
    }

    [Fact]
    public void The_final_version_can_have_exactly_the_maximum_size()
    {
        var work = NewWork(WorkStatus.Admitted);

        Assert.True(work.SubmitFinalVersion("url", "FINAL.PDF", ArtifactFileRules.MaxSizeBytes, T0.AddDays(1)).IsFinal);
    }

    [Fact]
    public void The_final_version_cannot_be_empty()
    {
        var work = NewWork(WorkStatus.Admitted);

        Assert.Throws<DomainException>(() => work.SubmitFinalVersion("url", "final.pdf", 0, T0.AddDays(1)));
    }

    [Fact]
    public void Ordinary_artifacts_are_never_final()
    {
        var work = NewWork(WorkStatus.Draft);

        var artifact = work.AddArtifact(ArtifactType.Document, "a.pdf", null, T0);

        Assert.False(artifact.IsFinal);
        Assert.False(work.HasFinalVersion);
    }
}
