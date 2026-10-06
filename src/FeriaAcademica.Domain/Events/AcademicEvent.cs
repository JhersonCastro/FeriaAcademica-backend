using FeriaAcademica.Domain.Common;

namespace FeriaAcademica.Domain.Events;

public enum EventStatus { InReview, Approved, ApprovedWithChanges, Rejected }

public sealed record AcademicEventCreated(Guid EventId, DateTime OccurredAt) : IDomainEvent;
public sealed record AcademicEventApproved(Guid EventId, DateTime OccurredAt) : IDomainEvent;

public sealed class AcademicEvent : AggregateRoot
{
    public string Title { get; private set; } = default!;
    public string Category { get; private set; } = default!;
    public string Organizer { get; private set; } = default!;
    public DateOnly Date { get; private set; }
    public int Capacity { get; private set; }
    public EventStatus Status { get; private set; }

    private AcademicEvent() { } // EF Core

    public static AcademicEvent Create(string title, string category, string organizer, DateOnly date, int capacity)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Title is required.");
        if (capacity <= 0) throw new DomainException("Capacity must be greater than zero.");

        var academicEvent = new AcademicEvent
        {
            Title = title.Trim(), Category = category, Organizer = organizer,
            Date = date, Capacity = capacity, Status = EventStatus.InReview,
        };
        academicEvent.Raise(new AcademicEventCreated(academicEvent.Id, DateTime.UtcNow));
        return academicEvent;
    }

    public void Approve()
    {
        if (Status == EventStatus.Rejected) throw new DomainException("A rejected event cannot be approved.");
        Status = EventStatus.Approved;
        Raise(new AcademicEventApproved(Id, DateTime.UtcNow));
    }

    public void Reject() => Status = EventStatus.Rejected;
}
