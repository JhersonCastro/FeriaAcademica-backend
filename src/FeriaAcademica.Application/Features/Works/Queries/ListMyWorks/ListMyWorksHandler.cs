using Mediator;

namespace FeriaAcademica.Application.Features.Works.Queries.ListMyWorks;

internal sealed class ListMyWorksHandler(IMyWorkReadModel readModel) : IQueryHandler<ListMyWorksQuery, IReadOnlyList<MyWorkDto>>
{
    public async ValueTask<IReadOnlyList<MyWorkDto>> Handle(ListMyWorksQuery q, CancellationToken ct) => await readModel.ListAsync(q.AuthorId, ct);
}
