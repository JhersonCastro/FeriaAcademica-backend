using FeriaAcademica.Domain.Events;
using Mediator;

namespace FeriaAcademica.Application.Features.Events.Commands.RemoveEventStage;

public sealed record RemoveEventStageCommand(Guid EventId, StageType Type) : ICommand<Unit>;
