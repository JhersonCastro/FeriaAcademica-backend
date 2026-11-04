using PdfSharp.Fonts;

namespace FeriaAcademica.Infrastructure.Pdf;

/// <summary>
/// PDFsharp needs to be told where the fonts are. Every requested family is drawn with the first sans-serif family found on the
/// machine (Windows fonts folder, Linux and macOS font folders), so the PDFs look the same wherever the API runs.
/// </summary>
internal sealed class SystemFontResolver : IFontResolver
{
    // Candidate families: (regular, bold, italic, bold italic) file names, in order of preference.
    private static readonly string[][] Families =
    [
        ["arial.ttf", "arialbd.ttf", "ariali.ttf", "arialbi.ttf"],
        ["LiberationSans-Regular.ttf", "LiberationSans-Bold.ttf", "LiberationSans-Italic.ttf", "LiberationSans-BoldItalic.ttf"],
        ["DejaVuSans.ttf", "DejaVuSans-Bold.ttf", "DejaVuSans-Oblique.ttf", "DejaVuSans-BoldOblique.ttf"],
        ["Arial.ttf", "Arial Bold.ttf", "Arial Italic.ttf", "Arial Bold Italic.ttf"],
    ];

    private static readonly string[] Directories =
    [
        Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
        "/usr/share/fonts", "/usr/local/share/fonts", "/Library/Fonts", "/System/Library/Fonts/Supplemental",
    ];

    private static readonly Lazy<string[]?> Files = new(FindFamily);

    public static void Register()
    {
        if (GlobalFontSettings.FontResolver is null) GlobalFontSettings.FontResolver = new SystemFontResolver();
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic) =>
        new((isBold ? "b" : "r") + (isItalic ? "i" : "n"));

    public byte[]? GetFont(string faceName)
    {
        var files = Files.Value ?? throw new InvalidOperationException("No sans-serif font was found on this machine to draw PDFs.");
        var index = faceName switch { "rn" => 0, "bn" => 1, "ri" => 2, _ => 3 };
        return File.ReadAllBytes(files[index]);
    }

    private static string[]? FindFamily()
    {
        foreach (var family in Families)
        {
            var found = family.Select(name => Directories.Where(Directory.Exists)
                    .Select(dir => Directory.EnumerateFiles(dir, name, SearchOption.AllDirectories).FirstOrDefault())
                    .FirstOrDefault(path => path is not null))
                .ToArray();
            if (found.All(path => path is not null)) return found!;
        }
        return null;
    }
}
