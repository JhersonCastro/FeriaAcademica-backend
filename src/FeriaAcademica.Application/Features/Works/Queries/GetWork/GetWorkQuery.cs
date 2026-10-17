using Mediator;

namespace FeriaAcademica.Application.Features.Works.Queries.GetWork;

/// <summary>A work as its author sees it; null when it does not exist or belongs to someone else.</summary>
public sealed record GetWorkQuery(Guid WorkId, Guid AuthorId) : IQuery<WorkDto?>;
