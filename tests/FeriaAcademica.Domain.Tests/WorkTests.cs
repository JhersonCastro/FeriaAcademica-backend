using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Works;

namespace FeriaAcademica.Domain.Tests;

/// <summary>Executable specification of the Work rules (derived from the reference scenarios in servicio.py).</summary>
public class WorkTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Author = Guid.NewGuid();
    private static readonly Guid Professor = Guid.NewGuid();
    private static readonly Guid Coordinator = Guid.NewGuid();

    private static Work NewDraft(int maxRejections = 3) =>
        Work.Create(Guid.NewGuid(), Professor, "Solar tracker", "Summary", maxRejections, Author, T0);

    private static Work NewSubmitted(int maxRejections = 3)
    {
        var work = NewDraft(maxRejections);
        work.AddArtifact(ArtifactType.Document, "report.pdf", null, T0);
        work.Submit(Author, T0.AddMinutes(1));
        return work;
    }

    private static Work NewInReview(int maxRejections = 3)
    {
        var work = NewSubmitted(maxRejections);
        work.StartReview(Professor, T0.AddMinutes(2));
        return work;
    }

    /// <summary>Takes a work through reject/resubmit cycles; leaves it in review after the last resubmission, or closed by attempts.</summary>
    private static void RejectOnce(Work work)
    {
        work.Reject(Professor, "Needs a clearer objective.", T0.AddMinutes(3));
        if (work.Status != WorkStatus.Draft) return;
        work.Submit(Author, T0.AddMinutes(4));
        work.StartReview(Professor, T0.AddMinutes(5));
    }

    // ---- Status is only changed by the entity's own methods ----

    [Fact]
    public void Status_has_no_public_setter()
    {
        var setter = typeof(Work).GetProperty(nameof(Work.Status))!.SetMethod;
        Assert.True(setter is null || !setter.IsPublic);
    }

    // ---- Enviar ----

    [Fact]
    public void Submit_moves_a_draft_with_an_artifact_to_Submitted()
    {
        var work = NewDraft();
        work.AddArtifact(ArtifactType.Document, null, "https://example.org/paper", T0);

        work.Submit(Author, T0.AddMinutes(1));

        Assert.Equal(WorkStatus.Submitted, work.Status);
        Assert.Equal(T0.AddMinutes(1), work.SubmittedAt);
        var queued = Assert.Single(work.ReviewQueue);
        Assert.Equal(QueueStatus.Pending, queued.Status);
    }

    [Fact]
    public void Submit_without_artifacts_is_rejected()
    {
        var work = NewDraft();
        var ex = Assert.Throws<DomainException>(() => work.Submit(Author, T0));
        Assert.Contains("artifact", ex.Message);
        Assert.Equal(WorkStatus.Draft, work.Status);
    }

    [Fact]
    public void Submit_is_only_allowed_from_Draft()
    {
        var work = NewSubmitted();
        Assert.Throws<DomainException>(() => work.Submit(Author, T0.AddMinutes(9)));
    }

    // ---- Rechazar ----

    [Fact]
    public void Reject_requires_observations()
    {
        var work = NewInReview();
        Assert.Throws<DomainException>(() => work.Reject(Professor, "  ", T0));
        Assert.Equal(WorkStatus.InReview, work.Status);
        Assert.Equal(0, work.RejectionCount);
    }

    [Fact]
    public void Reject_returns_the_work_to_Draft_and_counts_the_rejection()
    {
        var work = NewInReview();

        work.Reject(Professor, "Needs a clearer objective.", T0.AddMinutes(3));

        Assert.Equal(WorkStatus.Draft, work.Status);
        Assert.Equal(1, work.RejectionCount);
        Assert.Equal(2, work.CurrentAttempt);
        Assert.Null(work.Closure);
        Assert.Equal("Needs a clearer objective.", work.History.Last().Reason);
    }

    [Fact]
    public void Rejection_number_N_closes_the_work_by_attempts()
    {
        var work = NewInReview(maxRejections: 2);

        RejectOnce(work);                    // rejection 1 -> back to draft, resubmitted, in review again
        Assert.Equal(WorkStatus.InReview, work.Status);
        RejectOnce(work);                    // rejection 2 = the cap -> closed

        Assert.Equal(WorkStatus.RejectedDefinitively, work.Status);
        Assert.Equal(ClosureType.ByAttempts, work.Closure);
        Assert.NotNull(work.ClosureReason);
        Assert.Equal(2, work.RejectionCount);
        Assert.Null(work.History.Last().ActorId); // closed by the system
    }

    // ---- Rechazar definitivamente ----

    [Fact]
    public void RejectDefinitively_requires_a_reason()
    {
        var work = NewInReview();
        Assert.Throws<DomainException>(() => work.RejectDefinitively(Professor, "", T0));
        Assert.Equal(WorkStatus.InReview, work.Status);
    }

    [Fact]
    public void RejectDefinitively_closes_the_work_manually()
    {
        var work = NewInReview();

        work.RejectDefinitively(Professor, "Plagiarism.", T0.AddMinutes(3));

        Assert.Equal(WorkStatus.RejectedDefinitively, work.Status);
        Assert.Equal(ClosureType.Manual, work.Closure);
        Assert.Equal("Plagiarism.", work.ClosureReason);
        Assert.Equal(0, work.RejectionCount);
    }

    // ---- Retirar ----

    [Fact]
    public void Withdraw_from_Draft_cancels_the_work()
    {
        var work = NewDraft();

        work.Withdraw(Author, T0.AddMinutes(1));

        Assert.Equal(WorkStatus.Cancelled, work.Status);
        Assert.Equal(ClosureType.Withdrawal, work.Closure);
    }

    [Fact]
    public void Withdraw_from_Submitted_returns_it_to_Draft_if_nobody_opened_it()
    {
        var work = NewSubmitted();

        work.Withdraw(Author, T0.AddMinutes(3));

        Assert.Equal(WorkStatus.Draft, work.Status);
        Assert.Equal(QueueStatus.Withdrawn, Assert.Single(work.ReviewQueue).Status);
        Assert.Equal(0, work.RejectionCount);
    }

    [Fact]
    public void Withdraw_is_not_allowed_while_in_review()
    {
        var work = NewInReview();
        Assert.Throws<DomainException>(() => work.Withdraw(Author, T0.AddMinutes(3)));
        Assert.Equal(WorkStatus.InReview, work.Status);
    }

    [Fact]
    public void Resubmitting_after_a_withdrawal_requeues_the_same_attempt_at_the_back()
    {
        var work = NewSubmitted();
        work.Withdraw(Author, T0.AddMinutes(3));

        work.Submit(Author, T0.AddMinutes(10));

        var queued = Assert.Single(work.ReviewQueue);
        Assert.Equal(QueueStatus.Pending, queued.Status);
        Assert.Equal(T0.AddMinutes(10), queued.EnqueuedAt);
        Assert.Equal(T0.AddMinutes(1), work.SubmittedAt); // the first submission date is kept for tie-breaks
    }

    // ---- Reabrir ----

    [Fact]
    public void Reopen_is_only_allowed_from_RejectedDefinitively()
    {
        var work = NewInReview();
        Assert.Throws<DomainException>(() => work.Reopen(Coordinator, "Second chance.", T0));
    }

    [Fact]
    public void Reopen_requires_a_reason()
    {
        var work = NewInReview();
        work.RejectDefinitively(Professor, "Plagiarism.", T0.AddMinutes(3));

        Assert.Throws<DomainException>(() => work.Reopen(Coordinator, " ", T0.AddMinutes(4)));
        Assert.Equal(WorkStatus.RejectedDefinitively, work.Status);
    }

    [Fact]
    public void Reopen_after_the_cap_raises_it_to_the_rejection_count_plus_one()
    {
        var work = NewInReview(maxRejections: 2);
        RejectOnce(work);
        RejectOnce(work); // closed by attempts with 2 rejections, cap 2

        work.Reopen(Coordinator, "Exceptional circumstances.", T0.AddMinutes(20));

        Assert.Equal(WorkStatus.Draft, work.Status);
        Assert.Equal(3, work.MaxRejections);   // max(2, 2 + 1)
        Assert.Equal(2, work.RejectionCount);  // the counter is kept
        Assert.Null(work.Closure);
        Assert.Null(work.ClosureReason);
        Assert.Null(work.ClosedAt);
        var last = work.History.Last();
        Assert.True(last.Forced);
        Assert.Equal(Coordinator, last.ActorId);
    }

    [Fact]
    public void Reopen_keeps_the_cap_when_it_is_already_higher()
    {
        var work = NewInReview(maxRejections: 5);
        work.RejectDefinitively(Professor, "Plagiarism.", T0.AddMinutes(3));

        work.Reopen(Coordinator, "Appeal accepted.", T0.AddMinutes(4));

        Assert.Equal(5, work.MaxRejections);   // max(5, 0 + 1)
    }

    // ---- Other rules of the entity ----

    [Fact]
    public void Admit_moves_a_work_in_review_to_Admitted()
    {
        var work = NewInReview();

        work.Admit(Professor, T0.AddMinutes(3));

        Assert.Equal(WorkStatus.Admitted, work.Status);
        Assert.Equal(ReviewResult.Admitted, Assert.Single(work.ReviewQueue).Result);
    }

    [Fact]
    public void Only_a_work_in_review_can_be_admitted_rejected_or_rejected_definitively()
    {
        var work = NewSubmitted();
        Assert.Throws<DomainException>(() => work.Admit(Professor, T0));
        Assert.Throws<DomainException>(() => work.Reject(Professor, "No.", T0));
        Assert.Throws<DomainException>(() => work.RejectDefinitively(Professor, "No.", T0));
    }

    [Fact]
    public void Only_the_assigned_professor_can_review()
    {
        var work = NewInReview();
        var other = Guid.NewGuid();
        Assert.Throws<DomainException>(() => work.Admit(other, T0));
        Assert.Throws<DomainException>(() => work.Reject(other, "No.", T0));
        Assert.Throws<DomainException>(() => work.RejectDefinitively(other, "No.", T0));
    }

    [Fact]
    public void An_artifact_needs_exactly_one_of_file_or_link()
    {
        var work = NewDraft();
        Assert.Throws<DomainException>(() => work.AddArtifact(ArtifactType.Document, null, null, T0));
        Assert.Throws<DomainException>(() => work.AddArtifact(ArtifactType.Document, "a.pdf", "https://x.org", T0));
    }

    [Fact]
    public void Artifacts_of_the_same_type_are_versioned_and_only_editable_in_Draft()
    {
        var work = NewDraft();
        work.AddArtifact(ArtifactType.Poster, "v1.png", null, T0);
        var second = work.AddArtifact(ArtifactType.Poster, "v2.png", null, T0);
        Assert.Equal(2, second.Version);

        work.Submit(Author, T0);
        Assert.Throws<DomainException>(() => work.AddArtifact(ArtifactType.Poster, "v3.png", null, T0));
    }
}
