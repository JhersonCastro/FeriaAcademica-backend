using Mediator;

namespace FeriaAcademica.Application.Features.Works.Commands.WithdrawWork;

/// <summary>A submitted work goes back to draft so it can be replaced; withdrawing a draft cancels it.</summary>
public sealed record WithdrawWorkCommand(Guid WorkId, Guid ActorId) : ICommand<Unit>;
