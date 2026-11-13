using Mediator;

namespace FeriaAcademica.Application.Features.Works.Queries.DownloadWorkFile;

/// <summary>A file uploaded to a work; null when it does not exist or the user may not read it (both look the same).</summary>
public sealed record DownloadWorkFileQuery(Guid WorkId, Guid FileId, Guid UserId, bool IsSuperAdmin) : IQuery<WorkFileDownload?>;

/// <summary>The file ready to be sent; whoever receives it disposes <paramref name="Content"/>.</summary>
public sealed record WorkFileDownload(Stream Content, string ContentType, string FileName);
