using Mediator;

namespace FeriaAcademica.Application.Features.Works.Queries.ListAssignedWorks;

internal sealed class ListAssignedWorksHandler(IAssignedWorkReadModel readModel) : IQueryHandler<ListAssignedWorksQuery, IReadOnlyList<AssignedWorkDto>>
{
    public async ValueTask<IReadOnlyList<AssignedWorkDto>> Handle(ListAssignedWorksQuery q, CancellationToken ct) =>
        await readModel.ListAsync(q.ReviewerId, ct);
}
