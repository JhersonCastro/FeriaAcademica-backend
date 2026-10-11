using Mediator;

namespace FeriaAcademica.Application.Features.Works.Queries.GetWork;

public sealed record GetWorkQuery(Guid WorkId) : IQuery<WorkDto?>;
