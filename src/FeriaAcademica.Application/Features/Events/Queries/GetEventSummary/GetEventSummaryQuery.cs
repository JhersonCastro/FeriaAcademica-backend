using Mediator;

namespace FeriaAcademica.Application.Features.Events.Queries.GetEventSummary;

/// <summary>The event's consolidated figures, calculated when asked for, so they are always up to date.</summary>
public sealed record GetEventSummaryQuery(Guid EventId) : IQuery<EventSummaryDto?>;
