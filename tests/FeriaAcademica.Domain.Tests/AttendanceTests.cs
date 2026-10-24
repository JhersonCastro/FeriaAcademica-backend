using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Registrations;

namespace FeriaAcademica.Domain.Tests;

/// <summary>Executable specification of registration by QR and attendance at the door (HU-042, HU-050, HU-051, HU-052).</summary>
public class AttendanceTests
{
    private static readonly DateTime T0 = new(2026, 5, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Event = Guid.NewGuid();
    private static readonly Guid Person = Guid.NewGuid();
    private static readonly Guid Scanner = Guid.NewGuid();

    private static Registration NewRegistration() => Registration.Create(Event, Person, "a1b2c3");

    private static AcademicEvent NewEvent(EventStatus target)
    {
        var academicEvent = AcademicEvent.Create(Guid.NewGuid(), "Feria 2026", Guid.NewGuid());
        academicEvent.AddStage(StageType.Registration, active: true);
        if (target == EventStatus.Configuration) return academicEvent;
        academicEvent.Publish();
        return academicEvent;
    }

    // ---- QR of a registration ----

    [Fact]
    public void A_registration_carries_the_QR_token_it_was_created_with()
    {
        var registration = NewRegistration();

        Assert.Equal("a1b2c3", registration.QrToken);
        Assert.Equal((Event, Person, RegistrationStatus.Registered), (registration.EventId, registration.UserId, registration.Status));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_registration_needs_a_QR_token(string token) =>
        Assert.Throws<DomainException>(() => Registration.Create(Event, Person, token));

    // ---- Scan: first entry ----

    [Fact]
    public void The_first_scan_registers_the_attendance_with_who_and_when()
    {
        var registration = NewRegistration();

        var result = registration.CheckIn(existing: null, Scanner, T0);

        Assert.Equal(CheckInOutcome.Entered, result.Outcome);
        Assert.Equal((registration.Id, Scanner, T0), (result.Attendance!.RegistrationId, result.Attendance.ValidatedBy, result.Attendance.At));
    }

    // ---- Scan: no duplicates ----

    [Fact]
    public void Scanning_again_reports_the_first_entry_and_does_not_create_another()
    {
        var registration = NewRegistration();
        var first = registration.CheckIn(null, Scanner, T0).Attendance!;

        var second = registration.CheckIn(first, Guid.NewGuid(), T0.AddMinutes(5));

        Assert.Equal(CheckInOutcome.AlreadyEntered, second.Outcome);
        Assert.Same(first, second.Attendance);
        Assert.Equal(T0, second.Attendance!.At);
    }

    // ---- Scan: cancelled registration ----

    [Fact]
    public void A_cancelled_registration_does_not_get_in()
    {
        var registration = NewRegistration();
        typeof(Registration).GetProperty(nameof(Registration.Status))!.SetValue(registration, RegistrationStatus.Cancelled);

        var result = registration.CheckIn(null, Scanner, T0);

        Assert.Equal(CheckInOutcome.RegistrationCancelled, result.Outcome);
        Assert.Null(result.Attendance);
    }

    // ---- Who takes attendance ----

    [Fact]
    public void The_super_admin_can_manage_attendance() =>
        AttendancePolicy.EnsureCanManage(isSuperAdmin: true, []);

    [Theory]
    [InlineData(EventRole.Coordinator)]
    [InlineData(EventRole.Teacher)]
    public void Coordinators_and_teachers_of_the_event_can_manage_attendance(EventRole role) =>
        AttendancePolicy.EnsureCanManage(isSuperAdmin: false, [role]);

    [Theory]
    [InlineData(EventRole.Jury)]
    [InlineData(EventRole.Participant)]
    [InlineData(EventRole.Attendee)]
    public void Other_roles_cannot_manage_attendance(EventRole role) =>
        Assert.Throws<DomainException>(() => AttendancePolicy.EnsureCanManage(isSuperAdmin: false, [role]));

    [Fact]
    public void Someone_without_a_role_in_the_event_cannot_manage_attendance() =>
        Assert.Throws<DomainException>(() => AttendancePolicy.EnsureCanManage(isSuperAdmin: false, []));

    [Fact]
    public void A_role_held_alongside_others_is_enough() =>
        AttendancePolicy.EnsureCanManage(isSuperAdmin: false, [EventRole.Participant, EventRole.Teacher]);

    // ---- When the event accepts registrations and attendance ----

    [Fact]
    public void A_published_event_accepts_registrations_and_attendance() =>
        NewEvent(EventStatus.Published).EnsureIsLive();

    [Fact]
    public void An_event_still_in_configuration_does_not_accept_them()
    {
        var ex = Assert.Throws<DomainException>(() => NewEvent(EventStatus.Configuration).EnsureIsLive());
        Assert.Contains("Configuration", ex.Message);
    }
}
