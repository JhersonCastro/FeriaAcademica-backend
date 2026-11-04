using FeriaAcademica.Application.Features.Works.Commands.AddArtifact;
using Mediator;

namespace FeriaAcademica.Application.Features.Works.Commands.SubmitFinalVersion;

/// <summary>The uploaded final document: its URL plus the original name and size, which the format and size rules check.</summary>
public sealed record SubmitFinalVersionCommand(Guid WorkId, Guid ActorId, string? FileUrl, string? FileName, long? FileSizeBytes) : ICommand<AddedArtifact>;
