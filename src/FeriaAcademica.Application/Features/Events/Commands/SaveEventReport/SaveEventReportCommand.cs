using Mediator;

namespace FeriaAcademica.Application.Features.Events.Commands.SaveEventReport;

/// <summary>Creates the event's report or replaces its content; <c>Html</c> is exactly what the administrator composed.</summary>
public sealed record SaveEventReportCommand(Guid EventId, string? Html, Guid ActorId) : ICommand<Unit>;
