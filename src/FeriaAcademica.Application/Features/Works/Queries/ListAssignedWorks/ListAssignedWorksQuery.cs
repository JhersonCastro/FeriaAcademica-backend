using Mediator;

namespace FeriaAcademica.Application.Features.Works.Queries.ListAssignedWorks;

/// <summary>Works waiting for the given reviewer: submitted or already in review.</summary>
public sealed record ListAssignedWorksQuery(Guid ReviewerId) : IQuery<IReadOnlyList<AssignedWorkDto>>;
