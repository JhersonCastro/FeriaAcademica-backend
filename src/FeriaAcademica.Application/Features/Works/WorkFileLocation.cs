namespace FeriaAcademica.Application.Features.Works;

/// <summary>Where an uploaded file of a work is served from (its <c>FileUrl</c>) and where it is kept in the storage.</summary>
internal static class WorkFileLocation
{
    public static string Url(Guid workId, Guid fileId) => $"/api/works/{workId}/files/{fileId}";

    /// <summary>The storage key without extension; the extension of the uploaded file is appended when saving.</summary>
    public static string Key(Guid workId, Guid fileId) => $"{workId}/{fileId}";
}
