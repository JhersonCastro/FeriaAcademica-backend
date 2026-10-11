using Mediator;

namespace FeriaAcademica.Application.Features.Works.Queries.GetWork;

internal sealed class GetWorkHandler(IWorkReadModel readModel) : IQueryHandler<GetWorkQuery, WorkDto?>
{
    public async ValueTask<WorkDto?> Handle(GetWorkQuery q, CancellationToken ct) => await readModel.GetAsync(q.WorkId, ct);
}
