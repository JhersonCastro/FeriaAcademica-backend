using FeriaAcademica.Domain.Common;

namespace FeriaAcademica.Domain.Works;

/// <summary>Accepted format and size of uploaded artifact files. Change the allowed files here.</summary>
public static class ArtifactFileRules
{
    public static readonly IReadOnlyList<string> AllowedExtensions = [".pdf"];
    public const long MaxSizeBytes = 10 * 1024 * 1024; // 10 MB

    internal static void Validate(string fileName, long sizeBytes)
    {
        if (string.IsNullOrWhiteSpace(fileName)) throw new DomainException("The file name is required.");
        var extension = Path.GetExtension(fileName.Trim());
        if (!AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            throw new DomainException($"The file format is not allowed. Allowed formats: {string.Join(", ", AllowedExtensions)}.");
        if (sizeBytes <= 0) throw new DomainException("The file is empty.");
        if (sizeBytes > MaxSizeBytes) throw new DomainException($"The file exceeds the maximum size of {MaxSizeBytes / (1024 * 1024)} MB.");
    }
}
