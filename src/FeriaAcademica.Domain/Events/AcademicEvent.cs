using FeriaAcademica.Domain.Common;

namespace FeriaAcademica.Domain.Events;

public enum EventStatus { Configuration, Published, InProgress, Closed, Archived, Cancelled }

public sealed class AcademicEvent : Entity
{
    public const string DefaultTieBreakRulesJson = """["jury","attendees","submission_date"]""";

    private readonly List<EventStage> _stages = [];

    public Guid InstitutionId { get; private set; }
    public Guid? TemplateId { get; private set; }   // origin only: the event copies the template, it does not depend on it
    public string Name { get; private set; } = default!;
    public EventStatus Status { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime? StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public int MaxRejections { get; private set; }
    public double JuryWeight { get; private set; }
    public double AttendeeWeight { get; private set; }
    public string TieBreakRulesJson { get; private set; } = DefaultTieBreakRulesJson;
    public Guid CreatedBy { get; private set; }
    public IReadOnlyCollection<EventStage> Stages => _stages;

    /// <summary>Works can be submitted or resubmitted until the reception stage ends; null means no deadline has been set.</summary>
    public DateTime? SubmissionDeadline =>
        _stages.FirstOrDefault(s => s.Type == StageType.Reception && s.Status != StageStatus.Disabled)?.EndDate;

    private AcademicEvent() { } // EF Core

    public static AcademicEvent Create(Guid institutionId, string name, Guid createdBy, Guid? templateId = null,
        int maxRejections = 3, double juryWeight = 0.7, double attendeeWeight = 0.3)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name is required.");
        if (maxRejections < 1) throw new DomainException("Max rejections must be at least one.");
        if (Math.Abs(juryWeight + attendeeWeight - 1.0) > 1e-9) throw new DomainException("Jury and attendee weights must add up to 100%.");

        return new AcademicEvent
        {
            InstitutionId = institutionId, TemplateId = templateId, Name = name.Trim(), Status = EventStatus.Configuration,
            MaxRejections = maxRejections, JuryWeight = juryWeight, AttendeeWeight = attendeeWeight, CreatedBy = createdBy,
        };
    }

    public EventStage AddStage(StageType type, bool active)
    {
        if (_stages.Any(s => s.Type == type)) throw new DomainException($"The event already has a {type} stage.");
        var stage = EventStage.Create(Id, type, active);
        _stages.Add(stage);
        return stage;
    }

    public void ScheduleStage(StageType type, DateTime startDate, DateTime endDate)
    {
        if (Status is EventStatus.Closed or EventStatus.Archived or EventStatus.Cancelled)
            throw new DomainException($"The stages of an event in state {Status} cannot be rescheduled.");
        var stage = _stages.FirstOrDefault(s => s.Type == type) ?? AddStage(type, active: true);   // scheduling a missing stage adds it
        stage.Schedule(startDate, endDate);
    }

    public void Publish()
    {
        if (Status != EventStatus.Configuration) throw new DomainException($"An event in state {Status} cannot be published.");
        if (_stages.All(s => s.Status == StageStatus.Disabled)) throw new DomainException("At least one stage must be active.");
        Status = EventStatus.Published;
    }
}
