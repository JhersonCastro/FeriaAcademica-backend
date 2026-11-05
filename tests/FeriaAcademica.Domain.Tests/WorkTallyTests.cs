using FeriaAcademica.Domain.Works;

namespace FeriaAcademica.Domain.Tests;

/// <summary>Executable specification of how the works add up in the event report (HU-031).</summary>
public class WorkTallyTests
{
    [Fact]
    public void Admitted_and_evaluated_works_count_as_approved()
    {
        var tally = WorkTally.From(new Dictionary<WorkStatus, int> { [WorkStatus.Admitted] = 2, [WorkStatus.Evaluated] = 3 });

        Assert.Equal(5, tally.Approved);
    }

    [Fact]
    public void Only_a_definitive_rejection_counts_as_rejected()
    {
        var tally = WorkTally.From(new Dictionary<WorkStatus, int> { [WorkStatus.RejectedDefinitively] = 2, [WorkStatus.Draft] = 4 });

        Assert.Equal(2, tally.Rejected);
        Assert.Equal(4, tally.ReturnedForChanges);
    }

    [Fact]
    public void Submitted_works_waiting_for_the_reviewer_count_as_in_review()
    {
        var tally = WorkTally.From(new Dictionary<WorkStatus, int> { [WorkStatus.Submitted] = 1, [WorkStatus.InReview] = 2 });

        Assert.Equal(3, tally.InReview);
    }

    [Fact]
    public void The_groups_add_up_to_the_works_received()
    {
        var byStatus = Enum.GetValues<WorkStatus>().ToDictionary(s => s, s => (int)s + 1);

        var tally = WorkTally.From(byStatus);

        Assert.Equal(byStatus.Values.Sum(), tally.Received);
        Assert.Equal(tally.Received, tally.Approved + tally.Rejected + tally.InReview + tally.ReturnedForChanges + tally.Withdrawn);
    }

    [Fact]
    public void An_event_without_works_has_every_figure_at_zero()
    {
        Assert.Equal(new WorkTally(0, 0, 0, 0, 0, 0), WorkTally.From(new Dictionary<WorkStatus, int>()));
    }
}
