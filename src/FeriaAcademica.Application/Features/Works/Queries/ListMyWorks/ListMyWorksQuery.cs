using Mediator;

namespace FeriaAcademica.Application.Features.Works.Queries.ListMyWorks;

public sealed record ListMyWorksQuery(Guid AuthorId) : IQuery<IReadOnlyList<MyWorkDto>>;
