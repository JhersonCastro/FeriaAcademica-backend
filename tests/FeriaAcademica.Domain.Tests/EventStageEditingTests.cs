using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;

namespace FeriaAcademica.Domain.Tests;

/// <summary>Executable specification of editing one stage of a fair and of the stages' order (T-13).</summary>
public class EventStageEditingTests
{
    private static readonly DateTime T0 = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    private static AcademicEvent NewEvent()
    {
        var academicEvent = AcademicEvent.Create(Guid.NewGuid(), "Feria 2026", Guid.NewGuid());
        academicEvent.ScheduleStage(StageType.Reception, T0, T0.AddDays(10));
        return academicEvent;
    }

    private static EventStage Reception(AcademicEvent academicEvent) => academicEvent.Stages.Single(s => s.Type == StageType.Reception);

    [Fact]
    public void Changing_only_the_end_keeps_the_start()
    {
        var academicEvent = NewEvent();

        academicEvent.RescheduleStage(StageType.Reception, null, T0.AddDays(20));

        Assert.Equal((T0, T0.AddDays(20)), (Reception(academicEvent).StartDate, Reception(academicEvent).EndDate));
    }

    [Fact]
    public void Changing_only_the_start_keeps_the_end()
    {
        var academicEvent = NewEvent();

        academicEvent.RescheduleStage(StageType.Reception, T0.AddDays(2), null);

        Assert.Equal((T0.AddDays(2), T0.AddDays(10)), (Reception(academicEvent).StartDate, Reception(academicEvent).EndDate));
    }

    [Fact]
    public void The_edited_stage_must_still_end_after_it_starts()
    {
        var academicEvent = NewEvent();

        Assert.Throws<DomainException>(() => academicEvent.RescheduleStage(StageType.Reception, T0.AddDays(15), null));
        Assert.Equal(T0, Reception(academicEvent).StartDate);
    }

    [Fact]
    public void A_stage_that_is_not_in_the_timeline_cannot_be_edited()
    {
        var academicEvent = NewEvent();
        academicEvent.ScheduleStage(StageType.Call, T0.AddDays(-5), T0);
        academicEvent.RemoveStage(StageType.Call);

        Assert.Throws<DomainException>(() => academicEvent.RescheduleStage(StageType.Call, null, T0.AddDays(1)));         // removed
        Assert.Throws<DomainException>(() => academicEvent.RescheduleStage(StageType.Evaluation, null, T0.AddDays(1)));   // never added
    }

    [Fact]
    public void A_stage_without_dates_needs_both_of_them()
    {
        var academicEvent = AcademicEvent.Create(Guid.NewGuid(), "Feria 2026", Guid.NewGuid());
        academicEvent.AddStage(StageType.Registration, active: true);

        var ex = Assert.Throws<DomainException>(() => academicEvent.RescheduleStage(StageType.Registration, T0, null));
        Assert.Contains("both", ex.Message);

        academicEvent.RescheduleStage(StageType.Registration, T0, T0.AddDays(1));
        Assert.Equal(T0.AddDays(1), academicEvent.Stages.Single().EndDate);
    }

    [Fact]
    public void The_stages_of_a_closed_event_cannot_be_edited()
    {
        var academicEvent = NewEvent();
        academicEvent.Publish();
        academicEvent.Close();

        Assert.Throws<DomainException>(() => academicEvent.RescheduleStage(StageType.Reception, null, T0.AddDays(20)));
    }

    [Fact]
    public void A_stages_order_is_its_place_in_the_natural_sequence()
    {
        var academicEvent = AcademicEvent.Create(Guid.NewGuid(), "Feria 2026", Guid.NewGuid());

        Assert.Equal(1, academicEvent.AddStage(StageType.Configuration, active: true).Order);
        Assert.Equal(4, academicEvent.AddStage(StageType.Reception, active: true).Order);
        Assert.Equal(9, academicEvent.AddStage(StageType.Publication, active: true).Order);
    }
}
