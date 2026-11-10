namespace FeriaAcademica.Application.Features.Works.Queries.DownloadWorkFile;

public interface IWorkFileReadModel { Task<WorkFileInfo?> FindAsync(Guid workId, string fileUrl, Guid userId, CancellationToken ct); }

/// <summary>What is needed to decide who may read an uploaded file and to name it; <paramref name="IsAssignedJury"/> is about the user asking.</summary>
public sealed record WorkFileInfo(string ArtifactType, int Version, Guid EventId, Guid AuthorId, Guid ReviewerId, bool IsAssignedJury);
