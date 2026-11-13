using Mediator;

namespace FeriaAcademica.Application.Features.Evaluation.Queries.GetMyWorkAnswers;

public sealed record GetMyWorkAnswersQuery(Guid EventId, Guid WorkId, Guid UserId) : IQuery<MyWorkAnswersDto?>;
