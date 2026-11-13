using FeriaAcademica.Domain.Common;

namespace FeriaAcademica.Domain.Events;

public enum StageType { Configuration, Call, Nomination, Reception, Admission, Registration, Evaluation, Results, Publication }
/// <summary>A stage is Disabled when the event does not use it; otherwise it is Scheduled, Open or Closed.</summary>
public enum StageStatus { Disabled, Scheduled, Open, Closed }

/// <summary>One phase of an event (call, reception, evaluation...) with its dates; it is created and scheduled only through <see cref="AcademicEvent"/>.</summary>
public sealed class EventStage : Entity
{
    public Guid EventId { get; private set; }
    public StageType Type { get; private set; }
    public StageStatus Status { get; private set; }
    public DateTime? StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }

    private EventStage() { } // EF Core

    internal static EventStage Create(Guid eventId, StageType type, bool active) =>
        new() { EventId = eventId, Type = type, Status = active ? StageStatus.Scheduled : StageStatus.Disabled };

    /// <summary>Scheduling a disabled stage puts it back in the timeline.</summary>
    internal void Schedule(DateTime startDate, DateTime endDate)
    {
        if (endDate <= startDate) throw new DomainException("A stage must end after it starts.");
        if (Status == StageStatus.Disabled) Status = StageStatus.Scheduled;
        StartDate = startDate;
        EndDate = endDate;
    }

    /// <summary>Takes the stage out of the timeline and forgets its dates; scheduling it again brings it back.</summary>
    internal void Disable()
    {
        Status = StageStatus.Disabled;
        StartDate = null;
        EndDate = null;
    }
}
