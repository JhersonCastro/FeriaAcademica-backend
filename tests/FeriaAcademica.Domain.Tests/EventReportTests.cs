using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;

namespace FeriaAcademica.Domain.Tests;

/// <summary>Executable specification of the event report that the administrator composes and downloads as a PDF (HU-032).</summary>
public class EventReportTests
{
    private static readonly DateTime T0 = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Event = Guid.NewGuid();
    private static readonly Guid Admin = Guid.NewGuid();

    [Fact]
    public void A_report_keeps_the_html_exactly_as_it_was_written()
    {
        const string html = "<h1>Informe</h1><p>120 asistentes</p><img src=\"data:image/png;base64,AAAA\"/>";

        var report = EventReport.Create(Event, html, Admin, T0);

        Assert.Equal(html, report.Html);
        Assert.Equal(Event, report.EventId);
        Assert.Equal(Admin, report.UpdatedBy);
        Assert.Equal(T0, report.UpdatedAt);
    }

    [Fact]
    public void Updating_replaces_the_content_and_records_who_and_when()
    {
        var report = EventReport.Create(Event, "<p>v1</p>", Admin, T0);
        var other = Guid.NewGuid();

        report.Update("<p>v2</p>", other, T0.AddDays(1));

        Assert.Equal("<p>v2</p>", report.Html);
        Assert.Equal(other, report.UpdatedBy);
        Assert.Equal(T0.AddDays(1), report.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_report_is_rejected(string html)
    {
        Assert.Throws<DomainException>(() => EventReport.Create(Event, html, Admin, T0));

        var report = EventReport.Create(Event, "<p>ok</p>", Admin, T0);
        Assert.Throws<DomainException>(() => report.Update(html, Admin, T0));
        Assert.Equal("<p>ok</p>", report.Html);
    }

    [Fact]
    public void A_report_larger_than_the_limit_is_rejected()
    {
        var html = new string('a', EventReport.MaxHtmlLength + 1);

        Assert.Throws<DomainException>(() => EventReport.Create(Event, html, Admin, T0));
    }
}
