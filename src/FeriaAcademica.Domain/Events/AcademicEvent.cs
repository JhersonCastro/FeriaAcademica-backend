using FeriaAcademica.Domain.Common;

namespace FeriaAcademica.Domain.Events;

/// <summary>Lifecycle of an event, from Configuration to Closed, Archived or Cancelled.</summary>
public enum EventStatus { Configuration, Published, InProgress, Closed, Archived, Cancelled }

/// <summary>A fair: its stages, capacity, rejection limit and scoring weights. The end of its Reception stage is the submission deadline for works.</summary>
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
    public int? Capacity { get; private set; }   // seats for registrations; null means unlimited
    public IReadOnlyCollection<EventStage> Stages => _stages;

    /// <summary>Works can be submitted or resubmitted until the reception stage ends; null means no deadline has been set.</summary>
    public DateTime? SubmissionDeadline =>
        _stages.FirstOrDefault(s => s.Type == StageType.Reception && s.Status != StageStatus.Disabled)?.EndDate;

    /// <summary>Certificates are handed out once the event is over.</summary>
    public bool IsFinished => Status is EventStatus.Closed or EventStatus.Archived;

    private AcademicEvent() { } // EF Core

    public static AcademicEvent Create(Guid institutionId, string name, Guid createdBy, Guid? templateId = null,
        int maxRejections = 3, double juryWeight = 0.7, double attendeeWeight = 0.3, int? capacity = null)
    {
        EnsureValidCapacity(capacity);
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name is required.");
        if (maxRejections < 1) throw new DomainException("Max rejections must be at least one.");
        if (Math.Abs(juryWeight + attendeeWeight - 1.0) > 1e-9) throw new DomainException("Jury and attendee weights must add up to 100%.");

        return new AcademicEvent
        {
            InstitutionId = institutionId, TemplateId = templateId, Name = name.Trim(), Status = EventStatus.Configuration,
            MaxRejections = maxRejections, JuryWeight = juryWeight, AttendeeWeight = attendeeWeight, CreatedBy = createdBy,
            Capacity = capacity,
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
        var stage = _stages.FirstOrDefault(s => s.Type == type) ?? AddStage(type, active: true);   // scheduling a missing or removed stage adds it back
        stage.Schedule(startDate, endDate);
    }

    /// <summary>
    /// Takes a stage out of the timeline (it is disabled, not deleted). Without the Reception stage there is no submission deadline.
    /// A published event keeps at least one active stage.
    /// </summary>
    public void RemoveStage(StageType type)
    {
        if (Status is EventStatus.Closed or EventStatus.Archived or EventStatus.Cancelled)
            throw new DomainException($"The stages of an event in state {Status} cannot be changed.");
        var stage = _stages.FirstOrDefault(s => s.Type == type && s.Status != StageStatus.Disabled)
            ?? throw new DomainException($"The event has no {type} stage in its timeline.");
        if (Status != EventStatus.Configuration && _stages.Count(s => s.Status != StageStatus.Disabled) == 1)
            throw new DomainException("A published event must keep at least one active stage.");
        stage.Disable();
    }

    /// <summary>Changes the seats; it cannot go below the people already registered. Null removes the limit.</summary>
    public void SetCapacity(int? capacity, int registered)
    {
        EnsureValidCapacity(capacity);
        if (capacity < registered) throw new DomainException($"The capacity cannot be lower than the {registered} people already registered.");
        Capacity = capacity;
    }

    /// <summary>A new registration needs a free seat; <paramref name="registered"/> counts only registrations that are not cancelled.</summary>
    public void EnsureHasRoom(int registered)
    {
        if (Capacity is { } capacity && registered >= capacity) throw new DomainException("The event is full: there are no seats left.");
    }

    private static void EnsureValidCapacity(int? capacity)
    {
        if (capacity < 1) throw new DomainException("Capacity must be at least one.");
    }

    public void Publish()
    {
        if (Status != EventStatus.Configuration) throw new DomainException($"An event in state {Status} cannot be published.");
        if (_stages.All(s => s.Status == StageStatus.Disabled)) throw new DomainException("At least one stage must be active.");
        Status = EventStatus.Published;
    }

    /// <summary>Ends the event; from then on certificates can be downloaded and registrations are no longer accepted.</summary>
    public void Close()
    {
        if (Status is not (EventStatus.Published or EventStatus.InProgress)) throw new DomainException($"An event in state {Status} cannot be closed.");
        Status = EventStatus.Closed;
    }

    /// <summary>People can register and attendance can be taken only while the event is published or in progress.</summary>
    public void EnsureIsLive()
    {
        if (Status is not (EventStatus.Published or EventStatus.InProgress))
            throw new DomainException($"An event in state {Status} does not accept registrations or attendance.");
    }
}
