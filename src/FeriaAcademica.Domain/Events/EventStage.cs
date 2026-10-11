using FeriaAcademica.Domain.Common;

namespace FeriaAcademica.Domain.Events;

public enum StageType { Configuration, Call, Nomination, Reception, Admission, Registration, Evaluation, Results, Publication }
public enum StageStatus { Disabled, Scheduled, Open, Closed }

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
}
