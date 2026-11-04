using FeriaAcademica.Domain.Common;

namespace FeriaAcademica.Domain.Events;

/// <summary>
/// The event's closing report: free HTML the administrator composes (text, images...). It is kept as written and
/// turned into a PDF on demand, so the PDF always matches what the administrator sees.
/// </summary>
public sealed class EventReport : Entity
{
    public const int MaxHtmlLength = 2_000_000; // images go inline (data URIs), so allow room for them

    public Guid EventId { get; private set; }
    public string Html { get; private set; } = default!;
    public Guid UpdatedBy { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private EventReport() { } // EF Core

    public static EventReport Create(Guid eventId, string html, Guid actorId, DateTime now)
    {
        var report = new EventReport { EventId = eventId };
        report.Write(html, actorId, now);
        return report;
    }

    public void Update(string html, Guid actorId, DateTime now) => Write(html, actorId, now);

    private void Write(string html, Guid actorId, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(html)) throw new DomainException("The report is empty.");
        if (html.Length > MaxHtmlLength) throw new DomainException("The report is too large.");
        Html = html; UpdatedBy = actorId; UpdatedAt = now;
    }
}
