using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Registrations;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using TheArtOfDev.HtmlRenderer.PdfSharp;

namespace FeriaAcademica.Infrastructure.Pdf;

internal sealed class PdfRenderer : IPdfRenderer
{
    private const string Font = "Sans";

    static PdfRenderer() => SystemFontResolver.Register();

    public byte[] RenderHtml(string html)
    {
        var config = new PdfGenerateConfig { PageSize = PdfSharp.PageSize.A4, MarginTop = 40, MarginBottom = 40, MarginLeft = 40, MarginRight = 40 };

        // The report is composed by a person but rendered by the server: nothing outside the HTML itself (remote images,
        // linked stylesheets) is fetched, so a report cannot make the server call other addresses. Inline images (data URIs) work.
        using var document = PdfGenerator.GeneratePdf(html, config, cssData: null,
            stylesheetLoad: (_, e) => e.SetStyleSheet = "/* external stylesheets are not loaded */",
            imageLoad: (_, e) =>
            {
                if (e.Src.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return;
                e.Handled = true;
                e.Callback();
            });
        return Save(document);
    }

    public byte[] RenderCertificate(CertificateDocument c)
    {
        using var document = new PdfDocument();
        document.Info.Title = $"Certificate {c.Code}";
        var page = document.AddPage();
        page.Size = PdfSharp.PageSize.A4;
        page.Orientation = PdfSharp.PageOrientation.Landscape;

        using var g = XGraphics.FromPdfPage(page);
        var area = new XRect(0, 0, page.Width.Point, page.Height.Point);
        g.DrawRectangle(new XPen(XColors.DarkSlateBlue, 3), new XRect(24, 24, area.Width - 48, area.Height - 48));

        var (title, text) = c.Type == CertificateType.Attendance
            ? ("CERTIFICADO DE ASISTENCIA", "Se certifica que asistió al evento")
            : ("CERTIFICADO DE PARTICIPACIÓN", "Se certifica que participó en el evento");

        Line(g, title, 30, XFontStyleEx.Bold, area.Width, 120);
        Line(g, text, 16, XFontStyleEx.Regular, area.Width, 180);
        Line(g, c.HolderName, 28, XFontStyleEx.Bold, area.Width, 235);
        Line(g, "en", 16, XFontStyleEx.Regular, area.Width, 275);
        Line(g, c.EventName, 22, XFontStyleEx.Bold, area.Width, 315);
        if (!string.IsNullOrWhiteSpace(c.WorkTitle)) Line(g, $"con el trabajo \"{c.WorkTitle}\"", 16, XFontStyleEx.Italic, area.Width, 355);
        Line(g, $"Emitido el {c.IssuedAt:yyyy-MM-dd} · Código {c.Code}", 11, XFontStyleEx.Regular, area.Width, area.Height - 60);
        return Save(document);
    }

    private static void Line(XGraphics g, string text, double size, XFontStyleEx style, double width, double y) =>
        g.DrawString(text, new XFont(Font, size, style), XBrushes.Black, new XRect(0, y, width, size + 6), XStringFormats.TopCenter);

    private static byte[] Save(PdfDocument document)
    {
        using var stream = new MemoryStream();
        document.Save(stream, closeStream: false);
        return stream.ToArray();
    }
}
