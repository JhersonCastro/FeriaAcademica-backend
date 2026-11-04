using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Registrations;
using FeriaAcademica.Domain.Works;

namespace FeriaAcademica.Domain.Tests;

/// <summary>Executable specification of who can download a certificate (HU-061).</summary>
public class CertificateTests
{
    private static readonly DateTime T0 = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Author = Guid.NewGuid();
    private static readonly Guid Professor = Guid.NewGuid();

    private static AcademicEvent NewEvent(EventStatus target)
    {
        var academicEvent = AcademicEvent.Create(Guid.NewGuid(), "Feria 2026", Guid.NewGuid());
        academicEvent.AddStage(StageType.Registration, active: true);
        if (target == EventStatus.Configuration) return academicEvent;
        academicEvent.Publish();
        if (target == EventStatus.Published) return academicEvent;
        academicEvent.Close();
        return academicEvent;
    }

    private static Work NewWork(bool withFinalVersion)
    {
        var work = Work.Create(Guid.NewGuid(), Professor, "Solar tracker", null, 3, Author, T0);
        work.AddArtifact(ArtifactType.Document, "draft.pdf", null, T0);
        work.Submit(Author, T0.AddMinutes(1));
        work.StartReview(Professor, T0.AddMinutes(2));
        work.Admit(Professor, T0.AddMinutes(3));
        if (withFinalVersion) work.SubmitFinalVersion("final.pdf", "final.pdf", 1000, T0.AddMinutes(4));
        return work;
    }

    // ---- the event must be over ----

    [Fact]
    public void An_event_is_finished_once_it_is_closed()
    {
        Assert.False(NewEvent(EventStatus.Configuration).IsFinished);
        Assert.False(NewEvent(EventStatus.Published).IsFinished);
        Assert.True(NewEvent(EventStatus.Closed).IsFinished);
    }

    [Fact]
    public void Only_a_published_or_ongoing_event_can_be_closed()
    {
        var draft = NewEvent(EventStatus.Configuration);
        Assert.Throws<DomainException>(() => draft.Close());

        var closed = NewEvent(EventStatus.Closed);
        Assert.Throws<DomainException>(() => closed.Close());
    }

    [Fact]
    public void A_closed_event_no_longer_accepts_registrations_or_attendance()
    {
        Assert.Throws<DomainException>(() => NewEvent(EventStatus.Closed).EnsureIsLive());
    }

    // ---- attendance certificate ----

    [Fact]
    public void A_person_who_attended_can_download_the_attendance_certificate_when_the_event_is_over()
    {
        var ex = Record.Exception(() => CertificateEligibility.EnsureCanDownload(CertificateType.Attendance, NewEvent(EventStatus.Closed), attended: true, null));

        Assert.Null(ex);
    }

    [Fact]
    public void A_person_who_did_not_attend_cannot_download_the_attendance_certificate()
    {
        var ex = Assert.Throws<DomainException>(() =>
            CertificateEligibility.EnsureCanDownload(CertificateType.Attendance, NewEvent(EventStatus.Closed), attended: false, null));

        Assert.Contains("attended", ex.Message);
    }

    // ---- participation certificate ----

    [Fact]
    public void A_participant_with_a_final_version_can_download_the_participation_certificate_when_the_event_is_over()
    {
        var ex = Record.Exception(() =>
            CertificateEligibility.EnsureCanDownload(CertificateType.Participation, NewEvent(EventStatus.Closed), attended: false, NewWork(withFinalVersion: true)));

        Assert.Null(ex);
    }

    [Fact]
    public void Without_a_work_there_is_no_participation_certificate()
    {
        Assert.Throws<DomainException>(() =>
            CertificateEligibility.EnsureCanDownload(CertificateType.Participation, NewEvent(EventStatus.Closed), attended: true, null));
    }

    [Fact]
    public void A_work_without_its_final_version_gives_no_participation_certificate()
    {
        var ex = Assert.Throws<DomainException>(() =>
            CertificateEligibility.EnsureCanDownload(CertificateType.Participation, NewEvent(EventStatus.Closed), attended: true, NewWork(withFinalVersion: false)));

        Assert.Contains("final version", ex.Message);
    }

    // ---- not before the event is over ----

    [Theory]
    [InlineData(CertificateType.Attendance)]
    [InlineData(CertificateType.Participation)]
    public void No_certificate_is_available_while_the_event_has_not_finished(CertificateType type)
    {
        var ex = Assert.Throws<DomainException>(() =>
            CertificateEligibility.EnsureCanDownload(type, NewEvent(EventStatus.Published), attended: true, NewWork(withFinalVersion: true)));

        Assert.Contains("finished", ex.Message);
    }

    // ---- the certificate itself ----

    [Fact]
    public void An_attendance_certificate_has_no_work_and_a_participation_one_must_have_it()
    {
        var attendance = Certificate.Create(Guid.NewGuid(), Author, CertificateType.Attendance, null, "FA-1", T0);
        Assert.Null(attendance.WorkId);

        var workId = Guid.NewGuid();
        var participation = Certificate.Create(Guid.NewGuid(), Author, CertificateType.Participation, workId, "FA-2", T0);
        Assert.Equal(workId, participation.WorkId);

        Assert.Throws<DomainException>(() => Certificate.Create(Guid.NewGuid(), Author, CertificateType.Participation, null, "FA-3", T0));
        Assert.Throws<DomainException>(() => Certificate.Create(Guid.NewGuid(), Author, CertificateType.Attendance, workId, "FA-4", T0));
    }
}
