using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Registrations;

namespace FeriaAcademica.Application.Abstractions;

public sealed record PdfFile(string FileName, byte[] Content);

/// <param name="RecognizedRoles">The roles a recognition certificate mentions; the other types ignore them.</param>
public sealed record CertificateDocument(string HolderName, string EventName, CertificateType Type, string? WorkTitle, string Code, DateTime IssuedAt, IReadOnlyList<EventRole> RecognizedRoles);

/// <summary>Draws the PDFs the platform hands out.</summary>
public interface IPdfRenderer
{
    byte[] RenderCertificate(CertificateDocument certificate);

    /// <summary>Converts the HTML as written (text, styles, inline images) into a PDF.</summary>
    byte[] RenderHtml(string html);
}
