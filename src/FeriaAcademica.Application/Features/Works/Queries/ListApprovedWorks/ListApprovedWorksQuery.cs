using FeriaAcademica.Application.Features.Works.Queries.ListAssignedWorks;
using Mediator;

namespace FeriaAcademica.Application.Features.Works.Queries.ListApprovedWorks;

/// <summary>Works of the given reviewer that were approved (admitted or already evaluated).</summary>
public sealed record ListApprovedWorksQuery(Guid ReviewerId) : IQuery<IReadOnlyList<AssignedWorkDto>>;
