using FeriaAcademica.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace FeriaAcademica.Infrastructure.Storage;

/// <summary>Keeps uploaded files on disk under <c>Storage:Path</c> (default <c>uploads</c>, next to where the API runs).</summary>
internal sealed class LocalFileStorage(IConfiguration config) : IFileStorage
{
    private readonly string _root = Path.GetFullPath(config["Storage:Path"] ?? "uploads");

    public async Task SaveAsync(string key, Stream content, CancellationToken ct)
    {
        var path = Path.GetFullPath(Path.Combine(_root, key));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal)) throw new ArgumentException("Invalid storage key.", nameof(key));

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        await content.CopyToAsync(file, ct);
    }
}
