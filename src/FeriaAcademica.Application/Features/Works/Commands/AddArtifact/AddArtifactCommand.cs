using FeriaAcademica.Domain.Works;
using Mediator;

namespace FeriaAcademica.Application.Features.Works.Commands.AddArtifact;

/// <summary>Adds a link; files are uploaded with <c>UploadArtifactFileCommand</c>.</summary>
public sealed record AddArtifactCommand(Guid WorkId, Guid ActorId, ArtifactType Type, string? LinkUrl) : ICommand<AddedArtifact>;

public sealed record AddedArtifact(Guid Id, int Version);
