using FeriaAcademica.Application.Features.Works.Commands.AddArtifact;
using Mediator;

namespace FeriaAcademica.Application.Features.Works.Commands.SubmitFinalVersion;

/// <summary>Stores the uploaded final document and marks it as the final version; the format and size rules check <paramref name="FileName"/> and <paramref name="SizeBytes"/>.</summary>
public sealed record SubmitFinalVersionCommand(Guid WorkId, Guid ActorId, string FileName, long SizeBytes, Stream Content) : ICommand<AddedArtifact>;
