using FeriaAcademica.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace FeriaAcademica.Infrastructure.Storage;

/// <summary>Keeps uploaded files on disk under <c>Storage:Path</c> (default <c>uploads</c>, next to where the API runs).</summary>
internal sealed class LocalFileStorage(IConfiguration config) : IFileStorage
{
    private readonly string _root = Path.GetFullPath(config["Storage:Path"] ?? "uploads");

    public async Task SaveAsync(string key, Stream content, CancellationToken ct)
    {
        var path = PathOf(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        await content.CopyToAsync(file, ct);
    }

    public Task<StoredFile?> OpenReadAsync(string key, CancellationToken ct)
    {
        var path = PathOf(key);
        var directory = Path.GetDirectoryName(path)!;
        if (!Directory.Exists(directory)) return Task.FromResult<StoredFile?>(null);

        // Files are saved as <key><extension>, so the key alone finds them whatever the extension was.
        var found = File.Exists(path) ? path : Directory.EnumerateFiles(directory, Path.GetFileName(path) + ".*").FirstOrDefault();
        if (found is null) return Task.FromResult<StoredFile?>(null);

        var stream = new FileStream(found, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        return Task.FromResult<StoredFile?>(new StoredFile(stream, Path.GetExtension(found)));
    }

    private string PathOf(string key)
    {
        var path = Path.GetFullPath(Path.Combine(_root, key));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal)) throw new ArgumentException("Invalid storage key.", nameof(key));
        return path;
    }
}
