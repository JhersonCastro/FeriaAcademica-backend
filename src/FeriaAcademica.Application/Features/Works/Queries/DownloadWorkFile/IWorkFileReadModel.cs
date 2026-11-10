namespace FeriaAcademica.Application.Features.Works.Queries.DownloadWorkFile;

public interface IWorkFileReadModel { Task<WorkFileInfo?> FindAsync(Guid workId, string fileUrl, CancellationToken ct); }

/// <summary>What is needed to decide who may read an uploaded file and to name it.</summary>
public sealed record WorkFileInfo(string ArtifactType, int Version, Guid AuthorId, Guid ReviewerId);
