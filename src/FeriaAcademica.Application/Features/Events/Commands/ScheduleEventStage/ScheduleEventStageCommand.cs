using FeriaAcademica.Domain.Events;
using Mediator;

namespace FeriaAcademica.Application.Features.Events.Commands.ScheduleEventStage;

public sealed record ScheduleEventStageCommand(Guid EventId, StageType Type, DateTime StartDate, DateTime EndDate) : ICommand<Unit>;
