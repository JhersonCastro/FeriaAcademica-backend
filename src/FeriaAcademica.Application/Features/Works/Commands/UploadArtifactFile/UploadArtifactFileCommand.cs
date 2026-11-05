using FeriaAcademica.Application.Features.Works.Commands.AddArtifact;
using FeriaAcademica.Domain.Works;
using Mediator;

namespace FeriaAcademica.Application.Features.Works.Commands.UploadArtifactFile;

/// <summary>Stores an uploaded file and adds it to the work as a new artifact version; the format and size rules check <paramref name="FileName"/> and <paramref name="SizeBytes"/>.</summary>
public sealed record UploadArtifactFileCommand(
    Guid WorkId, Guid ActorId, ArtifactType Type, string FileName, long SizeBytes, Stream Content) : ICommand<AddedArtifact>;
