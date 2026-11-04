using System.Globalization;
using System.Text;

namespace FeriaAcademica.Application.Abstractions;

internal static class FileNames
{
    /// <summary>Lowercase letters, digits and dashes only, so the name is safe in a download header.</summary>
    public static string Slug(string text)
    {
        var plain = new StringBuilder();
        foreach (var ch in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            plain.Append(char.IsAsciiLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-');
        }
        var slug = string.Join('-', plain.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));
        return slug.Length == 0 ? "evento" : slug;
    }
}
