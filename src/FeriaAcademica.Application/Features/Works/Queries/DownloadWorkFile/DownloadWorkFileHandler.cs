using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Works;
using Mediator;

namespace FeriaAcademica.Application.Features.Works.Queries.DownloadWorkFile;

internal sealed class DownloadWorkFileHandler(IWorkFileReadModel files, IRoleAssignmentRepository roles, IFileStorage storage)
    : IQueryHandler<DownloadWorkFileQuery, WorkFileDownload?>
{
    public async ValueTask<WorkFileDownload?> Handle(DownloadWorkFileQuery q, CancellationToken ct)
    {
        var info = await files.FindAsync(q.WorkId, WorkFileLocation.Url(q.WorkId, q.FileId), q.UserId, ct);
        if (info is null) return null;

        var eventRoles = await roles.GetRolesAsync(q.UserId, info.EventId, ct);
        if (!WorkFileAccess.CanRead(q.UserId, info.AuthorId, info.ReviewerId, q.IsSuperAdmin, eventRoles, info.IsAssignedJury)) return null;

        var stored = await storage.OpenReadAsync(WorkFileLocation.Key(q.WorkId, q.FileId), ct);
        if (stored is null) return null;

        var contentType = stored.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase) ? "application/pdf" : "application/octet-stream";
        return new WorkFileDownload(stored.Content, contentType, $"{info.ArtifactType.ToLowerInvariant()}-v{info.Version}{stored.Extension.ToLowerInvariant()}");
    }
}
