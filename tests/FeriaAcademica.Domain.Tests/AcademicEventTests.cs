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
    public void ScheduleStage_rejects_a_disabled_stage()
    {
        var academicEvent = NewEvent();
        academicEvent.AddStage(StageType.Reception, active: false);

        Assert.Throws<DomainException>(() => academicEvent.ScheduleStage(StageType.Reception, T0, T0.AddDays(10)));
    }
}
