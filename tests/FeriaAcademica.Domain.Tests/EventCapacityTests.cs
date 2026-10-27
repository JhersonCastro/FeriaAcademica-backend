using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;

namespace FeriaAcademica.Domain.Tests;

/// <summary>Executable specification of the seats of an event: registering needs a free seat.</summary>
public class EventCapacityTests
{
    private static AcademicEvent NewEvent(int? capacity) =>
        AcademicEvent.Create(Guid.NewGuid(), "Feria 2026", Guid.NewGuid(), capacity: capacity);

    [Fact]
    public void An_event_without_capacity_is_unlimited()
    {
        var academicEvent = NewEvent(null);

        academicEvent.EnsureHasRoom(registered: 10_000);
    }

    [Fact]
    public void An_event_with_free_seats_accepts_a_registration()
    {
        var academicEvent = NewEvent(2);

        academicEvent.EnsureHasRoom(registered: 1);
    }

    [Fact]
    public void A_full_event_rejects_a_registration()
    {
        var academicEvent = NewEvent(2);

        var error = Assert.Throws<DomainException>(() => academicEvent.EnsureHasRoom(registered: 2));
        Assert.Contains("full", error.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Capacity_must_be_at_least_one(int capacity) =>
        Assert.Throws<DomainException>(() => NewEvent(capacity));

    [Fact]
    public void Capacity_can_be_raised_or_removed()
    {
        var academicEvent = NewEvent(2);

        academicEvent.SetCapacity(5, registered: 2);
        Assert.Equal(5, academicEvent.Capacity);

        academicEvent.SetCapacity(null, registered: 2);
        Assert.Null(academicEvent.Capacity);
    }

    [Fact]
    public void Capacity_cannot_go_below_the_people_already_registered()
    {
        var academicEvent = NewEvent(5);

        Assert.Throws<DomainException>(() => academicEvent.SetCapacity(2, registered: 3));
        Assert.Equal(5, academicEvent.Capacity);
    }
}
