using FeriaAcademica.Application.Features.Works.Queries.ListAssignedWorks;
using Mediator;

namespace FeriaAcademica.Application.Features.Works.Queries.ListApprovedWorks;

internal sealed class ListApprovedWorksHandler(IAssignedWorkReadModel readModel) : IQueryHandler<ListApprovedWorksQuery, IReadOnlyList<AssignedWorkDto>>
{
    public async ValueTask<IReadOnlyList<AssignedWorkDto>> Handle(ListApprovedWorksQuery q, CancellationToken ct) =>
        await readModel.ListApprovedAsync(q.ReviewerId, ct);
}
