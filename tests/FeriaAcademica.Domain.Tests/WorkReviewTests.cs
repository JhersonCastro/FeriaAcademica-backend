using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Works;

namespace FeriaAcademica.Domain.Tests;

/// <summary>Executable specification of the reviewer's decision on a work (EP-03).</summary>
public class WorkReviewTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Author = Guid.NewGuid();
    private static readonly Guid Professor = Guid.NewGuid();

    private static Work NewInReview(int maxRejections = 3)
    {
        var work = Work.Create(Guid.NewGuid(), Professor, "Solar tracker", "Summary", maxRejections, Author, T0);
        work.AddArtifact(ArtifactType.Document, "report.pdf", null, T0);
        work.Submit(Author, T0.AddMinutes(1));
        work.StartReview(Professor, T0.AddMinutes(2));
        return work;
    }

    [Fact]
    public void Approved_admits_the_work_and_keeps_the_optional_feedback()
    {
        var work = NewInReview();

        work.Decide(Professor, ReviewDecision.Approved, "Well argued.", T0.AddMinutes(3));

        Assert.Equal(WorkStatus.Admitted, work.Status);
        var item = Assert.Single(work.ReviewQueue);
        Assert.Equal(ReviewResult.Admitted, item.Result);
        Assert.Equal("Well argued.", item.Observations);
        Assert.Equal(T0.AddMinutes(3), item.ResolvedAt);
        Assert.Equal(Professor, work.History.Last().ActorId);
    }

    [Fact]
    public void Approved_does_not_require_feedback()
    {
        var work = NewInReview();

        work.Decide(Professor, ReviewDecision.Approved, "  ", T0.AddMinutes(3));

        Assert.Equal(WorkStatus.Admitted, work.Status);
        Assert.Null(Assert.Single(work.ReviewQueue).Observations);
    }

    [Fact]
    public void ApprovedWithChanges_returns_the_work_to_Draft_with_the_feedback()
    {
        var work = NewInReview();

        work.Decide(Professor, ReviewDecision.ApprovedWithChanges, "Add the methodology.", T0.AddMinutes(3));

        Assert.Equal(WorkStatus.Draft, work.Status);
        Assert.Equal(1, work.RejectionCount);
        var item = Assert.Single(work.ReviewQueue);
        Assert.Equal(ReviewResult.Rejected, item.Result);
        Assert.Equal("Add the methodology.", item.Observations);
    }

    [Fact]
    public void ApprovedWithChanges_at_the_rejection_limit_closes_the_work()
    {
        var work = NewInReview(maxRejections: 1);

        work.Decide(Professor, ReviewDecision.ApprovedWithChanges, "Add the methodology.", T0.AddMinutes(3));

        Assert.Equal(WorkStatus.RejectedDefinitively, work.Status);
        Assert.Equal(ClosureType.ByAttempts, work.Closure);
    }

    [Fact]
    public void Rejected_closes_the_work_with_the_feedback_as_reason()
    {
        var work = NewInReview();

        work.Decide(Professor, ReviewDecision.Rejected, "Out of scope.", T0.AddMinutes(3));

        Assert.Equal(WorkStatus.RejectedDefinitively, work.Status);
        Assert.Equal(ClosureType.Manual, work.Closure);
        Assert.Equal("Out of scope.", work.ClosureReason);
    }

    [Theory]
    [InlineData(ReviewDecision.ApprovedWithChanges)]
    [InlineData(ReviewDecision.Rejected)]
    public void A_decision_other_than_Approved_requires_feedback(ReviewDecision decision)
    {
        var work = NewInReview();

        var ex = Assert.Throws<DomainException>(() => work.Decide(Professor, decision, " ", T0.AddMinutes(3)));

        Assert.Contains("Feedback", ex.Message);
        Assert.Equal(WorkStatus.InReview, work.Status);
    }

    [Fact]
    public void Only_the_assigned_professor_can_decide()
    {
        var work = NewInReview();
        Assert.Throws<DomainException>(() => work.Decide(Guid.NewGuid(), ReviewDecision.Approved, null, T0.AddMinutes(3)));
        Assert.Equal(WorkStatus.InReview, work.Status);
    }

    [Fact]
    public void A_decision_needs_the_work_to_be_in_review()
    {
        var work = Work.Create(Guid.NewGuid(), Professor, "Solar tracker", null, 3, Author, T0);
        work.AddArtifact(ArtifactType.Document, "report.pdf", null, T0);
        work.Submit(Author, T0.AddMinutes(1));

        Assert.Throws<DomainException>(() => work.Decide(Professor, ReviewDecision.Approved, null, T0.AddMinutes(2)));
    }

    [Fact]
    public void An_unknown_decision_is_rejected()
    {
        var work = NewInReview();
        Assert.Throws<DomainException>(() => work.Decide(Professor, (ReviewDecision)99, "Feedback.", T0.AddMinutes(3)));
        Assert.Equal(WorkStatus.InReview, work.Status);
    }
}
