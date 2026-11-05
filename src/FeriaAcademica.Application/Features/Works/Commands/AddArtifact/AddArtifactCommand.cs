using FeriaAcademica.Domain.Works;
using Mediator;

namespace FeriaAcademica.Application.Features.Works.Commands.AddArtifact;

/// <summary>Either a link, or an uploaded file (its URL plus the original name and size, which the format and size rules check).</summary>
public sealed record AddArtifactCommand(
    Guid WorkId, Guid ActorId, ArtifactType Type, string? LinkUrl, string? FileUrl, string? FileName, long? FileSizeBytes) : ICommand<AddedArtifact>;

public sealed record AddedArtifact(Guid Id, int Version);
