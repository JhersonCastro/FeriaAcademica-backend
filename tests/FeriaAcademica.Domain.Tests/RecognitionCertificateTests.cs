using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Registrations;

namespace FeriaAcademica.Domain.Tests;

/// <summary>Executable specification of who receives a recognition certificate (HU-060).</summary>
public class RecognitionCertificateTests
{
    private static readonly DateTime T0 = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private static AcademicEvent NewEvent(bool closed)
    {
        var academicEvent = AcademicEvent.Create(Guid.NewGuid(), "Feria 2026", Guid.NewGuid());
        academicEvent.AddStage(StageType.Registration, active: true);
        academicEvent.Publish();
        if (closed) academicEvent.Close();
        return academicEvent;
    }

    [Theory]
    [InlineData(EventRole.Coordinator)]
    [InlineData(EventRole.Teacher)]
    [InlineData(EventRole.Jury)]
    public void Those_who_helped_run_the_event_qualify(EventRole role)
    {
        var ex = Record.Exception(() => CertificateEligibility.EnsureCanDownload(CertificateType.Recognition, NewEvent(closed: true), false, null, [role]));

        Assert.Null(ex);
    }

    [Theory]
    [InlineData(EventRole.Participant)]
    [InlineData(EventRole.Attendee)]
    public void Students_and_guests_do_not_qualify(EventRole role)
    {
        var ex = Assert.Throws<DomainException>(() => CertificateEligibility.EnsureCanDownload(CertificateType.Recognition, NewEvent(closed: true), true, null, [role]));

        Assert.Contains("recognition", ex.Message);
    }

    [Fact]
    public void A_person_without_a_role_in_the_event_does_not_qualify()
    {
        Assert.Throws<DomainException>(() => CertificateEligibility.EnsureCanDownload(CertificateType.Recognition, NewEvent(closed: true), false, null, []));
    }

    [Fact]
    public void It_is_not_available_until_the_event_has_finished()
    {
        var ex = Assert.Throws<DomainException>(() => CertificateEligibility.EnsureCanDownload(CertificateType.Recognition, NewEvent(closed: false), false, null, [EventRole.Jury]));

        Assert.Contains("finished", ex.Message);
    }

    [Fact]
    public void A_recognition_certificate_references_no_work()
    {
        Assert.Null(Certificate.Create(Guid.NewGuid(), Guid.NewGuid(), CertificateType.Recognition, null, "FA-1", T0).WorkId);
        Assert.Throws<DomainException>(() => Certificate.Create(Guid.NewGuid(), Guid.NewGuid(), CertificateType.Recognition, Guid.NewGuid(), "FA-1", T0));
    }
}
