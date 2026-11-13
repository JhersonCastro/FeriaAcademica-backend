using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;

namespace FeriaAcademica.Domain.Tests;

/// <summary>Executable specification of the event stage scheduling rules.</summary>
public class AcademicEventTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static AcademicEvent NewEvent() => AcademicEvent.Create(Guid.NewGuid(), "Feria 2026", Guid.NewGuid());

    [Fact]
    public void SubmissionDeadline_is_the_end_of_the_Reception_stage()
    {
        var academicEvent = NewEvent();
        academicEvent.ScheduleStage(StageType.Call, T0, T0.AddDays(3));
        academicEvent.ScheduleStage(StageType.Reception, T0, T0.AddDays(10));

        Assert.Equal(T0.AddDays(10), academicEvent.SubmissionDeadline);
    }

    [Fact]
    public void SubmissionDeadline_is_null_when_Reception_has_no_dates()
    {
        var academicEvent = NewEvent();
        academicEvent.AddStage(StageType.Reception, active: true);

        Assert.Null(academicEvent.SubmissionDeadline);
    }

    [Fact]
    public void ScheduleStage_adds_the_stage_when_it_is_missing()
    {
        var academicEvent = NewEvent();

        academicEvent.ScheduleStage(StageType.Reception, T0, T0.AddDays(10));

        var stage = Assert.Single(academicEvent.Stages);
        Assert.Equal(StageType.Reception, stage.Type);
        Assert.Equal(StageStatus.Scheduled, stage.Status);
    }

    [Fact]
    public void ScheduleStage_requires_the_end_to_be_after_the_start()
    {
        var academicEvent = NewEvent();
        Assert.Throws<DomainException>(() => academicEvent.ScheduleStage(StageType.Reception, T0, T0));
    }

    [Fact]
    public void ScheduleStage_puts_a_disabled_stage_back_in_the_timeline()
    {
        var academicEvent = NewEvent();
        academicEvent.AddStage(StageType.Reception, active: false);

        academicEvent.ScheduleStage(StageType.Reception, T0, T0.AddDays(10));

        var stage = Assert.Single(academicEvent.Stages);
        Assert.Equal((StageStatus.Scheduled, T0, T0.AddDays(10)), (stage.Status, stage.StartDate, stage.EndDate));
    }

    [Fact]
    public void RemoveStage_disables_the_stage_and_forgets_its_dates()
    {
        var academicEvent = NewEvent();
        academicEvent.ScheduleStage(StageType.Call, T0, T0.AddDays(3));
        academicEvent.ScheduleStage(StageType.Reception, T0, T0.AddDays(10));

        academicEvent.RemoveStage(StageType.Reception);

        var stage = academicEvent.Stages.Single(s => s.Type == StageType.Reception);
        Assert.Equal((StageStatus.Disabled, (DateTime?)null, (DateTime?)null), (stage.Status, stage.StartDate, stage.EndDate));
        Assert.Null(academicEvent.SubmissionDeadline);
    }

    [Fact]
    public void A_removed_stage_can_be_scheduled_again()
    {
        var academicEvent = NewEvent();
        academicEvent.ScheduleStage(StageType.Reception, T0, T0.AddDays(10));
        academicEvent.RemoveStage(StageType.Reception);

        academicEvent.ScheduleStage(StageType.Reception, T0.AddDays(1), T0.AddDays(20));

        Assert.Equal(T0.AddDays(20), academicEvent.SubmissionDeadline);
    }

    [Fact]
    public void RemoveStage_rejects_a_stage_that_is_not_in_the_timeline()
    {
        var academicEvent = NewEvent();
        academicEvent.AddStage(StageType.Reception, active: false);

        Assert.Throws<DomainException>(() => academicEvent.RemoveStage(StageType.Call));        // never added
        Assert.Throws<DomainException>(() => academicEvent.RemoveStage(StageType.Reception));   // already disabled
    }

    [Fact]
    public void A_published_event_keeps_at_least_one_active_stage()
    {
        var academicEvent = NewEvent();
        academicEvent.ScheduleStage(StageType.Call, T0, T0.AddDays(3));
        academicEvent.ScheduleStage(StageType.Reception, T0, T0.AddDays(10));
        academicEvent.Publish();

        academicEvent.RemoveStage(StageType.Call);
        var ex = Assert.Throws<DomainException>(() => academicEvent.RemoveStage(StageType.Reception));

        Assert.Contains("at least one active stage", ex.Message);
    }

    [Fact]
    public void An_event_in_configuration_can_remove_all_its_stages()
    {
        var academicEvent = NewEvent();
        academicEvent.ScheduleStage(StageType.Call, T0, T0.AddDays(3));

        academicEvent.RemoveStage(StageType.Call);

        Assert.All(academicEvent.Stages, s => Assert.Equal(StageStatus.Disabled, s.Status));
    }

    [Fact]
    public void The_stages_of_a_closed_event_cannot_be_removed()
    {
        var academicEvent = NewEvent();
        academicEvent.ScheduleStage(StageType.Call, T0, T0.AddDays(3));
        academicEvent.ScheduleStage(StageType.Reception, T0, T0.AddDays(10));
        academicEvent.Publish();
        academicEvent.Close();

        Assert.Throws<DomainException>(() => academicEvent.RemoveStage(StageType.Call));
    }
}
