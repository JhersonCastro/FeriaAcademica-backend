using FeriaAcademica.Domain.Events;
using Mediator;

namespace FeriaAcademica.Application.Features.Events.Commands.RescheduleEventStage;

/// <summary>Changes only the dates that are given; the other one keeps its value.</summary>
public sealed record RescheduleEventStageCommand(Guid EventId, StageType Type, DateTime? StartDate, DateTime? EndDate) : ICommand<Unit>;
